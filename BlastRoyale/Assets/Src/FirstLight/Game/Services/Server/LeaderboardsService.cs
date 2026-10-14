using System;
using System.Collections.Generic;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using PlayFab.ClientModels;
using Unity.Services.Authentication;
using UnityEngine;
using MainInstaller = FirstLight.Game.Utils.MainInstaller;

namespace FirstLight.Game.Services
{
	public interface ILeaderboardService
	{
		bool IsGlobalRankingAvailable { get; }
		IReadOnlyList<GameLeaderboard> Leaderboards { get; }
		LeaderboardConfig GetConfigs();
		int MaxEntries { get; }
		void GetTopRankLeaderboard(string metricName, Action<GetLeaderboardResult> onSuccess);
		void GetNeighborRankLeaderboard(string metricName, Action<GetLeaderboardAroundPlayerResult> onSuccess);
		PlayerLeaderboardEntry CurrentRankedEntry { get; }
		Color GetRankColor(GameLeaderboard board, int rank);
		GameLeaderboard Ranked { get; }
		event Action<PlayerLeaderboardEntry> OnRankingUpdate;
	}

	/// <summary>Reports unavailable global ranking explicitly. Local trophies remain ordinary persisted progression.</summary>
	public sealed class LeaderboardsService : ILeaderboardService
	{
		public event Action<PlayerLeaderboardEntry> OnRankingUpdate;
		public const int MAX_ENTRIES = 100;
		public const string LeaderboardConfigsDataName = "LeaderboardConfigs";
		public bool IsGlobalRankingAvailable => false;
		public IReadOnlyList<GameLeaderboard> Leaderboards { get; } = Array.Empty<GameLeaderboard>();
		public GameLeaderboard Ranked => null;
		public int MaxEntries => MAX_ENTRIES;
		// Position zero is the existing unranked sentinel consumed by room/player presentation.
		public PlayerLeaderboardEntry CurrentRankedEntry { get; private set; } = new PlayerLeaderboardEntry { Position = 0 };

		public LeaderboardsService(IGameServices services)
		{
			services.MessageBrokerService.Subscribe<SuccessfullyAuthenticated>(_ => UpdateLocalPlayerClientRank());
			services.MessageBrokerService.Subscribe<MainMenuLoadedMessage>(_ => UpdateLocalPlayerClientRank());
		}

		public LeaderboardConfig GetConfigs() => MainInstaller.ResolveData().RemoteConfigProvider.GetConfig<LeaderboardConfig>();
		public Color GetRankColor(GameLeaderboard board, int rank) => GameConstants.PlayerName.DEFAULT_COLOR;

		public void GetTopRankLeaderboard(string metricName, Action<GetLeaderboardResult> onSuccess) =>
			throw new NotSupportedException("Global rankings are unavailable in this development build.");
		public void GetNeighborRankLeaderboard(string metricName, Action<GetLeaderboardAroundPlayerResult> onSuccess) =>
			throw new NotSupportedException("Global rankings are unavailable in this development build.");

		public void UpdateLocalPlayerClientRank()
		{
			var id = AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : null;
			if (CurrentRankedEntry.PlayFabId == id && CurrentRankedEntry.Position == 0) return;
			// The genuine legacy DTO field carries the UGS identity while existing SDK contracts are retained.
			CurrentRankedEntry = new PlayerLeaderboardEntry { PlayFabId = id, Position = 0 };
			OnRankingUpdate?.Invoke(CurrentRankedEntry);
		}
	}
}
