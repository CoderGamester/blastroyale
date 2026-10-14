using System;
using System.Collections.Generic;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Configs.Utils;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>Explicit development defaults; use UGS Remote Config for live operations.</summary>
	public static class BundledRemoteConfig
	{
		public static object Create(Type type)
		{
			if (type == typeof(GameMaintenanceConfig)) return new GameMaintenanceConfig
			{
				AllowedVersion = Application.version,
				VersionBlockMessage = "Please update the game to {1}.",
				MaintenanceMessage = "The game is being maintained.",
				MaintenanceButtonText = "OK"
			};
			if (type == typeof(GeneralConfig)) return new GeneralConfig { ConfigCacheInSeconds = 300 };
			if (type == typeof(EventGameModesConfig)) return new EventGameModesConfig();
			if (type == typeof(EventNotificationConfig)) return new EventNotificationConfig { Notifications = Array.Empty<EventNotification>() };
			if (type == typeof(PerformanceConfig)) return new PerformanceConfig();
			if (type == typeof(LeaderboardConfig)) return new LeaderboardConfig();
			if (type == typeof(LeaderboardSeasons)) return new LeaderboardSeasons { [0] = new SeasonConfig { Name = "Development", Visible = false } };
			if (type == typeof(MatchmakingQueuesConfig)) return new MatchmakingQueuesConfig
			{
				["1"] = new TeamSizeConfig { QueueName = "Solo", QueueTimeoutTimeInSeconds = 15, FailsOnTimeout = false, IconSpriteClass = "solo-icon" },
				["2"] = new TeamSizeConfig { QueueName = "Duo", QueueTimeoutTimeInSeconds = 15, FailsOnTimeout = false, IconSpriteClass = "duo-icon" },
				["4"] = new TeamSizeConfig { QueueName = "Squad", QueueTimeoutTimeInSeconds = 15, FailsOnTimeout = false, IconSpriteClass = "squad-icon" }
			};
			if (type == typeof(FixedGameModesConfig)) return new FixedGameModesConfig
			{
				new FixedGameModeEntry
				{
					MatchConfig = new SimulationMatchConfig { UniqueConfigId = "development-solo", MapId = GameId.MazeMayhem.ToString(), TeamSize = 1, MinPlayersToStartMatch = 1 },
					Title = LocalizableString.FromText("Battle Royale"),
					Description = LocalizableString.FromText("Solo with bots"),
					LongDescription = LocalizableString.FromText("A development match with bots filling the room.")
				}
			};
			throw new NotSupportedException("No bundled default for " + type.Name + ". Configure it in Unity Remote Config or add an explicit development default.");
		}
	}
}
