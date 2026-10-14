using FirstLight.Inspector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public class QuantumGameModeConfig
	{
		[UnityEngine.Tooltip(DESC_ID)] public string Id;

#region Matchmaking & Room

		[UnityEngine.Header("Matchmaking & Room/Teams"), UnityEngine.Tooltip(DESC_MAX_PLAYERS_IN_TEAM)]
		public uint MaxAllowedPlayersPerTeam;

		[UnityEngine.Tooltip(DESC_ALLOWED_MAPS)]
		public List<GameId> AllowedMaps;

#endregion

#region UI / Visual

		// UI / Visual
		[UnityEngine.Header("UI"), UnityEngine.Tooltip(DESC_SHOW_MINIMAP)]
		public bool ShowUIMinimap;

		[UnityEngine.Tooltip(DESC_SHOW_TIMER)]
		public bool ShowUITimer;

		[UnityEngine.Tooltip(DESC_SHOW_UI_STANDINGS_EXTRA_INFO)]
		public bool ShowUIStandingsExtraInfo;

		[UnityEngine.Tooltip(DESC_SHOW_WEAPON_SLOTS)]
		public bool ShowWeaponSlots;

		[UnityEngine.Tooltip(DESC_SINGLE_SLOT_MODE)]
		public bool SingleSlotMode;

		[UnityEngine.Tooltip(DESC_DESCRIPTION_LOCALISATION_KEY)]
		public string DescriptionLocalisationKey;

#endregion

#region Player

		[UnityEngine.Header("Player"), UnityEngine.Tooltip(DESC_DROP_WEAPON_ON_PICKUP)]
		public bool DropWeaponOnPickup;

		[UnityEngine.Tooltip(DESC_AIRDROP_NEAR_PLAYER)]
		public bool AirdropNearPlayer;

		[UnityEngine.Tooltip(DESC_SHRINKING_CIRCLE_CENTERED_ON_PLAYER)]
		public bool ShrinkingCircleCenteredOnPlayer;

		[UnityEngine.Header("Player/H1/Spawning"), UnityEngine.Tooltip(DESC_SKYDIVE_SPAWN)]
		public bool SkydiveSpawn;

		[UnityEngine.Tooltip(DESC_SPAWN_SELECTION)]
		public bool SpawnSelection;

		[UnityEngine.Header("Player/H1/Death drops"), UnityEngine.Tooltip(DESC_DEATH_DROP_STRATEGY)]
		public DeathDropsStrategy DeathDropStrategy;

		[UnityEngine.Tooltip(DESC_DEATH_MARKER)]
		public bool DeathMarker;

		[UnityEngine.Tooltip(DESC_MINIMUM_HEALTH + "\n" + "If more than zero the player will never die. Only works with real players, not bots.")]
		public uint MinimumHealth;

		/// <summary>
		/// If this gamemode skips the pre game loading screen with the plane and maps election
		/// </summary>
		public bool InstantLoad;
#endregion

#region Bots

		[UnityEngine.Header("Bots"), UnityEngine.Tooltip(DESC_ALLOW_BOTS)]
		public bool AllowBots;

		[InspectorField(VisibleWhen = "AllowBots"), UnityEngine.Tooltip(DESC_BOT_USE_ANOTHER_MODE_BOTS)]
		public string UseBotsFromGamemode;

		[InspectorField(VisibleWhen = "AllowBots"), UnityEngine.Tooltip(DESC_BOT_TEAM_OVERRIDE)]
		public int BotsTeamOverride;

#endregion

#region Endgame

		[UnityEngine.Header("Endgame"), UnityEngine.Tooltip(DESC_COMPLETION_STRATEGY)]
		public GameCompletionStrategy CompletionStrategy;

		[InspectorField(VisibleWhen = nameof(UsesKillCount)), UnityEngine.Tooltip(DESC_COMPLETION_KILL_COUNT)]
		public uint CompletionKillCount;

		[UnityEngine.Tooltip(DESC_ALLOW_EARLY_REWARDS)]
		public bool AllowEarlyRewards;

#endregion

#region Debug

		[UnityEngine.Header("Debug"), UnityEngine.Tooltip(DESC_IS_DEBUG_ONLY)]
		public bool IsDebugOnly;

#endregion

		[InspectorField(Options = "GetOptionalSystems"), UnityEngine.Tooltip(DESC_SYSTEMS)]
		public List<string> Systems;

		#region Inspector Helpers

		private bool UsesKillCount() => CompletionStrategy == GameCompletionStrategy.KillCount;

		private IEnumerable<string> GetOptionalSystems()
		{
			return from assembly in AppDomain.CurrentDomain.GetAssemblies()
			       from type in assembly.GetTypes()
			       where type.IsDefined(typeof(OptionalSystemAttribute))
			       select type.AssemblyQualifiedName;
		}

#endregion

#region Tooltips

		// @formatter:off
		private const string DESC_ID = "A UNIQUE ID that identifies this game mode.";
		private const string DESC_MAX_PLAYERS = "The maximum number of players that can join a room.";
		private const string DESC_MIN_PLAYERS = "The minimum number of players that can join a room.";
		private const string DESC_TEAMS = "Whether or not this game mode supports teams.";
		private const string DESC_MAX_PLAYERS_IN_TEAM = "What is the maxumum number of players in a team.";
		private const string DESC_MIN_PLAYERS_IN_TEAM = "What is the minimum number of players in a team.";
		private const string DESC_SHOW_STAT_CHANGES = "Displays floating texts above the player when their stats change (e.g. on weapon pickup)";
		private const string DESC_SHOW_MINIMAP = "Displays the Minimap";
		private const string DESC_SHOW_TIMER = "Displays the countdown timer at the top center of the screen, along with other BR specific UIs. Requires ShrinkingCircle system to be enabled.";
		private const string DESC_SHOW_UI_STANDINGS_EXTRA_INFO = "Displays additional information on the standings / leaderboards, like XP and Trophy count.";
		private const string DESC_SHOW_WEAPON_SLOTS = "Displays weapon slots and enables the player to switch between weapons.";
		private const string DESC_SINGLE_SLOT_MODE = "Only shows the melee slot and the first weapon slot";
		private const string DESC_DESCRIPTION_LOCALISATION_KEY = "Localisation key for the description of this game mode";
		private const string DESC_MINIMUM_HEALTH = "Health will not fall below this value. Not for bots.";
		private const string DESC_DROP_WEAPON_ON_PICKUP = "Drops the player's equipped weapon if they pick up a better one.";
		private const string DESC_AIRDROP_NEAR_PLAYER = "Airdrop will spawn near player.";
		private const string DESC_SHRINKING_CIRCLE_CENTERED_ON_PLAYER = "Shrinking circle center is set on player.";
		private const string DESC_SPAWN_WITH_GEAR = "Spawns the player with their loadout gear equipped.";
		private const string DESC_SPAWN_WITH_WEAPON = "Spawns the player with their loadout weapon equipped.";
		private const string DESC_SKYDIVE_SPAWN = "Drops the player from a height when spawning.";
		private const string DESC_SPAWN_SELECTION = "Enables the player to select a spawn position on the map.";
		private const string DESC_SPAWN_PATTERN = "Limits spawn selection to a path on the map.";
		private const string DESC_DEATH_DROP_STRATEGY = "Which strategy of drop we use when the player dies.";
		private const string DESC_DEATH_MARKER = "If we should spawn a death marker on the position where a player died.";
		private const string DESC_ENABLE_BOX_RARITY_MODIFIERS = "If we change boxes rarities based on ChestTypeModifiers inside Chest config";
		private const string DESC_BOT_SEARCH_FOR_CRATES = "Should the bots search / look for crates.";
		private const string DESC_BOT_USE_ANOTHER_MODE_BOTS = "Use bot configs from another gamemode";
		private const string DESC_ALLOW_BOTS = "If bots can be enabled for this game mode.";
		private const string DESC_BOT_RESPAWN = "Allows bots to respawn when they get killed.";
		private const string DESC_BOT_WEAPON_SEARCH_STRATEGY = "How should bots search for weapons on the map.";
		private const string DESC_BOT_TEAM_OVERRIDE = "If >0 it will override all the bot's team number";
		private const string DESC_RANK_SORTER = "How should we sort the players on the leaderboards.";
		private const string DESC_RANK_PROCESSOR = "How should we modify the player's rank on the leaderboards.";
		private const string DESC_ALLOWED_MAPS = "Which maps are allowed to be played with this game mode.";
		private const string DESC_COMPLETION_STRATEGY = "What should mark the end of a match.";
		private const string DESC_ALLOW_EARLY_REWARDS = "Can player earn rewards mid-game ?";
		private const string DESC_COMPLETION_KILL_COUNT = "How many kills must a player have to win the match.";
		private const string DESC_SYSTEMS = "Which Quantum systems should be enabled for this game mode.";
		private const string DESC_IS_DEBUG_ONLY = "Marks this game mode to be available only in Debug builds.";
		// @formatter:on

#endregion
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumGameModeConfig"/>
	/// </summary>
	public partial class QuantumGameModeConfigs : Quantum.AssetObject
	{
		public List<QuantumGameModeConfig> QuantumConfigs = new List<QuantumGameModeConfig>();

		private IDictionary<string, QuantumGameModeConfig> _dictionary;

		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			_dictionary = new Dictionary<string, QuantumGameModeConfig>();
			for (var i = 0; i < QuantumConfigs.Count; i++)
			{
				_dictionary.Add(QuantumConfigs[i].Id, QuantumConfigs[i]);
			}
		}

		/// <summary>
		/// Requests the <see cref="QuantumGameModeConfig"/> defined by the given <paramref name="name"/>
		/// </summary>
		public QuantumGameModeConfig GetConfig(string name)
		{
			return _dictionary[name];
		}
	}
}
