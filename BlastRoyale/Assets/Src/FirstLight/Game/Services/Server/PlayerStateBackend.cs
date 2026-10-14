using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using PlayFab.ClientModels;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;
using SaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;

namespace FirstLight.Game.Services
{
	/// <summary>Client-computed progression and virtual purchases; Cloud Save persists them without server validation.</summary>
	public interface IPlayerStateBackend
	{
		UniTask<ServerState> Load();
		UniTask Save(ServerState state);
		UniTask<PublicPlayerProfile> GetPublicProfile(string playerId);
	}

	public sealed class UnityCloudPlayerStateBackend : IPlayerStateBackend
	{
		// AccountData, device IDs and local settings never belong in cloud progression.
		public static readonly Type[] ModelTypes =
		{
			typeof(PlayerData), typeof(LiveopsData), typeof(CollectionData), typeof(IdData), typeof(RngData),
			typeof(EquipmentData), typeof(BattlePassData), typeof(TutorialData), typeof(ContentCreatorData),
			typeof(PlayerStoreData), typeof(EventsData), typeof(BundlePurchaseData)
		};
		private readonly Dictionary<string, string> _lastSaved = new();
		private readonly Dictionary<string, string> _writeLocks = new();
		private UgsAccountSession.Snapshot _account;
		private int _loadGeneration;
		private bool _loaded;

		public async UniTask<ServerState> Load()
		{
			_loaded = false;
			var generation = ++_loadGeneration;
			var account = UgsAccountSession.Capture();
			var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
				ModelTypes.Select(Key).ToHashSet(), new LoadOptions(new DefaultReadAccessClassOptions()));
			account.ThrowIfChanged();
			if (generation != _loadGeneration) throw new InvalidOperationException("A newer player-state load superseded this request.");
			var state = new ServerState();
			var saved = new Dictionary<string, string>();
			var locks = new Dictionary<string, string>();
			foreach (var type in ModelTypes)
			{
				var key = Key(type);
				if (result.TryGetValue(key, out var item))
				{
					var json = item.Value.GetAs<string>();
					if (string.IsNullOrEmpty(json)) throw new InvalidOperationException("Saved player model is empty: " + type.Name);
					state[type.FullName] = json;
					saved[key] = json;
					locks[key] = item.WriteLock;
				}
				else
				{
					var model = Activator.CreateInstance(type);
					if (model is RngData rng)
					{
						rng.Seed = StableSeed(account.PlayerId);
						rng.State = RngUtils.GenerateRngState(rng.Seed);
					}
					state.UpdateModel(model);
				}
			}
			_lastSaved.Clear();
			_writeLocks.Clear();
			foreach (var entry in saved) _lastSaved[entry.Key] = entry.Value;
			foreach (var entry in locks) _writeLocks[entry.Key] = entry.Value;
			_account = account;
			_loaded = true;
			return state;
		}

		public async UniTask Save(ServerState state)
		{
			if (!_loaded) throw new InvalidOperationException("Load this account's progression before saving.");
			var account = _account;
			var generation = _loadGeneration;
			account.ThrowIfChanged();
			var updates = new Dictionary<string, SaveItem>();
			var values = new Dictionary<string, string>();
			foreach (var type in ModelTypes)
			{
				if (!state.TryGetValue(type.FullName, out var json)) continue;
				var key = Key(type);
				if (_lastSaved.TryGetValue(key, out var old) && old == json) continue;
				_writeLocks.TryGetValue(key, out var writeLock);
				updates[key] = new SaveItem(json, writeLock);
				values[key] = json;
			}
			if (updates.Count == 0) return;
			account.ThrowIfChanged();
			// One native batch covers all twelve keys. Existing write locks reject concurrent-device overwrites.
			var locks = await CloudSaveService.Instance.Data.Player.SaveAsync(updates,
				new SaveOptions(new DefaultWriteAccessClassOptions()));
			account.ThrowIfChanged();
			if (generation != _loadGeneration) throw new InvalidOperationException("Player state was reloaded while saving.");
			foreach (var entry in values)
			{
				_lastSaved[entry.Key] = entry.Value;
				_writeLocks[entry.Key] = locks[entry.Key];
			}
		}

		public async UniTask<PublicPlayerProfile> GetPublicProfile(string playerId)
		{
			if (string.IsNullOrWhiteSpace(playerId)) throw new ArgumentException("A UGS player ID is required.", nameof(playerId));
			var account = UgsAccountSession.Capture();
			var data = await CloudSaveService.Instance.Data.Player.LoadAsync(
				new HashSet<string> { "player_name", "avatar_url", "trophies" },
				new LoadOptions(new PublicReadAccessClassOptions(playerId)));
			account.ThrowIfChanged();
			return new PublicPlayerProfile
			{
				Name = data.TryGetValue("player_name", out var name) ? name.Value.GetAs<string>() : "Player",
				AvatarUrl = data.TryGetValue("avatar_url", out var avatar) ? avatar.Value.GetAs<string>() : null,
				Statistics = new List<Statistic>
				{
					new() { Name = GameConstants.Stats.RANKED_LEADERBOARD_LADDER_NAME,
						Value = data.TryGetValue("trophies", out var trophies) ? trophies.Value.GetAs<int>() : 0 }
				}
			};
		}

		private static string Key(Type type) => "model_" + type.Name;
		private static int StableSeed(string value)
		{
			unchecked
			{
				uint hash = 2166136261;
				foreach (var character in value ?? "") hash = (hash ^ character) * 16777619;
				return (int)hash;
			}
		}
	}

	public interface IClientAuthorityBackend
	{
		UniTask PersistPlayerState(ServerState snapshot);
	}
}
