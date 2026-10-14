using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	/// <summary>Client IDs are loaded from ignored Resources/LocalClientConfig.json. Never put server secrets here.</summary>
	public static class FLEnvironment
	{
		private const string ENV_KEY = "FLG_ENVIRONMENT";
		private static ClientConfiguration _configuration;
		public static ClientConfiguration Configuration => _configuration ??= LoadConfiguration();
		public static readonly Definition DEVELOPMENT = new ("development");
		public static readonly Definition STAGING = new ("staging");
		public static readonly Definition COMMUNITY = new ("community");
		public static readonly Definition PRODUCTION = new ("production");
		private static Definition? _current;

		public static Definition Current
		{
			get
			{
				if (_current.HasValue) return _current.Value;
				var asset = Resources.Load<FLEnvironmentAsset>("FLEnvironmentAsset");
				var fallback = asset != null ? asset.EnvironmentName : "development";
				_current = FromName(PlayerPrefs.GetString(ENV_KEY, fallback));
				return _current.Value;
			}
			set => _current = value;
		}

		[Serializable]
		public sealed class ClientConfiguration
		{
			public string PhotonQuantumAppId = "";
			public string UnityProjectId = "";
			public string UnityEnvironmentId = "";
			public string UnityEnvironmentName = "development";
			public string UnityCcdBucketId = "";
			public bool UseBundledRemoteConfig = true;
			public bool UseBundledShop = true;
		}

		private static ClientConfiguration LoadConfiguration()
		{
			var asset = Resources.Load<TextAsset>("LocalClientConfig");
			if (asset == null) return new ClientConfiguration();
			try
			{
				return JsonUtility.FromJson<ClientConfiguration>(asset.text) ?? new ClientConfiguration();
			}
			catch (Exception)
			{
				throw new InvalidOperationException("LocalClientConfig.json is not valid JSON. Copy the committed template and fill in the client IDs.");
			}
		}

		public static void ValidateClientConfiguration()
		{
			if (!Guid.TryParse(Configuration.UnityProjectId, out _))
				throw new InvalidOperationException("Set UnityProjectId in Assets/Resources/LocalClientConfig.json and link the same free Unity Cloud project in Project Settings > Services.");
			if (!Guid.TryParse(Configuration.PhotonQuantumAppId, out _))
				throw new InvalidOperationException("Set PhotonQuantumAppId to your Quantum App ID in Assets/Resources/LocalClientConfig.json.");
		}

		public readonly struct Definition : IEquatable<Definition>
		{
			private readonly string _name;
			public string Name => _name;
			public string PhotonAppIDRealtime => Configuration.PhotonQuantumAppId;
			public string UCSEnvironmentID => Configuration.UnityEnvironmentId;
			public string UCSEnvironmentName => Configuration.UnityEnvironmentName;
			public string UCSBucketID => Configuration.UnityCcdBucketId;
			public Definition(string name) { _name = name; }
			public bool Equals(Definition other) => Name == other.Name;
			public override bool Equals(object obj) => obj is Definition other && Equals(other);
			public override int GetHashCode() => Name?.GetHashCode() ?? 0;
			public static bool operator ==(Definition left, Definition right) => left.Equals(right);
			public static bool operator !=(Definition left, Definition right) => !left.Equals(right);
		}

		public static bool TryGetFromName(string environment, out Definition definition)
		{
			switch (environment)
			{
				case "development": definition = DEVELOPMENT; return true;
				case "staging": definition = STAGING; return true;
				case "community": definition = COMMUNITY; return true;
				case "production": definition = PRODUCTION; return true;
				default: definition = default; return false;
			}
		}

		public static Definition FromName(string environment) => TryGetFromName(environment, out var definition)
			? definition : throw new NotSupportedException("Invalid environment type: " + environment);

		// The retired deployment CDN must not redirect a developer's local backend configuration.
		public static UniTask CheckForEnvironmentRedirect(string gameBuild) => UniTask.CompletedTask;

		#region EditorHelpers

#if UNITY_EDITOR

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/development", false, 18)]
		private static void ToggleEnvironmentDevelopment() => SetEnvironment("development");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/development", true, 18)]
		private static bool ValidateEnvironmentDevelopment() => ValidateEnvironment("development");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/staging", false, 18)]
		private static void ToggleEnvironmentStaging() => SetEnvironment("staging");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/staging", true, 18)]
		private static bool ValidateEnvironmentStaging() => ValidateEnvironment("staging");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/community", false, 18)]
		private static void ToggleEnvironmentCommunity() => SetEnvironment("community");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/community", true, 18)]
		private static bool ValidateEnvironmentCommunity() => ValidateEnvironment("community");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/production", false, 18)]
		private static void ToggleEnvironmentProduction() => SetEnvironment("production");

		[UnityEditor.MenuItem("FLG/Local Flags/Environment/production", true, 18)]
		private static bool ValidateEnvironmentProduction() => ValidateEnvironment("production");

		private static bool ValidateEnvironment(string environment)
		{
			UnityEditor.Menu.SetChecked($"FLG/Local Flags/Environment/{environment}",
				UnityEngine.PlayerPrefs.GetString(ENV_KEY, "development") == environment);
			return true;
		}

		private static void SetEnvironment(string environment)
		{
			UnityEngine.PlayerPrefs.SetString(ENV_KEY, environment);
		}

#endif

		#endregion
	}
}
