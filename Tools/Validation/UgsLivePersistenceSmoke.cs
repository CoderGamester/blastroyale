using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Newtonsoft.Json.Linq;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;

namespace FirstLight.Modernization
{
	/// <summary>Live development-only probe using an isolated native Authentication profile, never the game's guest profile.</summary>
	[InitializeOnLoad]
	public static class UgsLivePersistenceSmoke
	{
		private const string Profile = "codex_modernization_probe";
		private const string Active = "BlastModernization.UgsLiveProbe.Active";
		private const string Restart = "BlastModernization.UgsLiveProbe.Restart";
		private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
		static UgsLivePersistenceSmoke()
		{
			if (SessionState.GetBool(Active, false)) EditorApplication.playModeStateChanged += OnPlayModeChanged;
		}
		public static void Run() => EnterPlayMode(false);
		public static void VerifyRestart() => EnterPlayMode(true);
		private static void EnterPlayMode(bool restart)
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("This probe must own a fresh PlayMode session.");
			SessionState.SetBool(Active, true);
			SessionState.SetBool(Restart, restart);
			EditorApplication.playModeStateChanged += OnPlayModeChanged;
			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
			EditorApplication.isPlaying = true;
		}
		private static void OnPlayModeChanged(PlayModeStateChange state)
		{
			if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Active, false)) return;
			SessionState.SetBool(Active, false);
			EditorApplication.playModeStateChanged -= OnPlayModeChanged;
			Start(SessionState.GetBool(Restart, false));
		}

		private static async void Start(bool restart)
		{
			var result = new JObject { ["Restart"] = restart, ["ProfileIsolatedFromGame"] = true };
			try
			{
				var task = Probe(result, restart);
				if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(90))) != task)
					throw new TimeoutException("Live UGS persistence probe did not finish within ninety seconds.");
				await task;
				result["Passed"] = true;
				SaveResult(result, restart);
				Debug.Log("[UGS Live Probe] Native authentication/persistence acceptance passed; restart=" + restart);
				EditorApplication.Exit(0);
			}
			catch (Exception error)
			{
				result["Passed"] = false;
				result["ErrorType"] = error.GetType().FullName;
				SaveResult(result, restart);
				Debug.LogException(error); // Raw details remain in ignored local logs.
				EditorApplication.Exit(2);
			}
		}

		private static async Task Probe(JObject result, bool restart)
		{
			var config = FLEnvironment.Configuration;
			if (config.UnityEnvironmentName != "development")
				throw new InvalidOperationException("This probe requires the explicitly configured development environment.");
			if (!string.Equals(Application.cloudProjectId, config.UnityProjectId, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("Native Unity project configuration does not match local configuration.");
			await UnityServices.InitializeAsync(new InitializationOptions().SetEnvironmentName(config.UnityEnvironmentName).SetProfile(Profile));
			var cached = AuthenticationService.Instance.SessionTokenExists;
			await AuthenticationService.Instance.SignInAnonymouslyAsync();
			result["NativeAuthorized"] = AuthenticationService.Instance.IsAuthorized;
			var backend = new UnityCloudPlayerStateBackend();
			var state = await backend.Load();
			var proofPath = Path.Combine(Root, ".local/ugs-live-persistence-proof.json");
			if (restart)
			{
				var proof = JObject.Parse(File.ReadAllText(proofPath));
				result["CachedSessionPresent"] = cached;
				result["SameNativeAccount"] = (string)proof["AccountHash"] == Hash(AuthenticationService.Instance.PlayerId);
				result["TwelveModelRestartMatches"] = (string)proof["ModelHash"] == HashModels(state);
				if (!cached || !(bool)result["SameNativeAccount"] || !(bool)result["TwelveModelRestartMatches"])
					throw new InvalidOperationException("Native cached-account/restart model proof did not match.");
				return;
			}
			await backend.Save(state);
			var loaded = new UnityCloudPlayerStateBackend();
			var reloaded = await loaded.Load();
			result["SavedModelCount"] = state.Count;
			result["TwelveModelReloadMatches"] = HashModels(state) == HashModels(reloaded);
			if (state.Count != 12 || !(bool)result["TwelveModelReloadMatches"])
				throw new InvalidOperationException("Live twelve-model persistence roundtrip did not match.");

			const string profileName = "Blast modernization probe";
			await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object>
				{ ["player_name"] = profileName, ["avatar_url"] = "", ["trophies"] = 500 },
				new SaveOptions(new PublicWriteAccessClassOptions()));
			var profile = await loaded.GetPublicProfile(AuthenticationService.Instance.PlayerId);
			result["PublicProfileMatches"] = profile.Name == profileName && profile.Statistics.Single().Value == 500;
			if (!(bool)result["PublicProfileMatches"]) throw new InvalidOperationException("Native Public profile readback differs.");

			// Simulate an independent writer with the real SDK/current lock. Only this probe account is affected.
			var key = "model_" + nameof(RngData);
			var model = typeof(RngData).FullName;
			var before = reloaded[model];
			var items = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key },
				new LoadOptions(new DefaultReadAccessClassOptions()));
			await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, SaveItem>
				{ [key] = new SaveItem(before + " ", items[key].WriteLock) }, new SaveOptions(new DefaultWriteAccessClassOptions()));
			reloaded[model] = before + "  "; // Valid JSON whitespace triggers a changed snapshot without changing model meaning.
			try { await loaded.Save(reloaded); }
			catch (CloudSaveException error) when (error.Reason == CloudSaveExceptionReason.Conflict)
				{ result["NativeExistingKeyConflictRejected"] = true; }
			if (result["NativeExistingKeyConflictRejected"]?.Value<bool>() != true)
				throw new InvalidOperationException("A stale native write lock did not reject the save.");
			var fresh = new UnityCloudPlayerStateBackend();
			var restored = await fresh.Load();
			restored[model] = before;
			await fresh.Save(restored);
			File.WriteAllText(proofPath, new JObject
				{ ["AccountHash"] = Hash(AuthenticationService.Instance.PlayerId), ["ModelHash"] = HashModels(restored) }.ToString());
			result["ProbeDataRestoredAfterConflict"] = true;
		}

		private static string HashModels(IDictionary<string, string> models) => Hash(string.Join("\n",
			models.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + "=" + pair.Value)));
		private static string Hash(string value)
		{
			using var sha = SHA256.Create();
			return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
		}
		private static void SaveResult(JObject result, bool restart)
		{
			var path = Path.Combine(Root, "artifacts/ugs-live-persistence" + (restart ? "-restart" : "") + ".json");
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, result.ToString());
		}
	}
}
