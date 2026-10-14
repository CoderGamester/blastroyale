using System;
using System.Linq;
using NUnit.Framework;
using Photon.Deterministic;
using Quantum;
using Quantum.Core;
using Quantum.Systems;
using Quantum.Systems.Bots;
using Assert = NUnit.Framework.Assert;
using Input = Quantum.Input;

namespace FirstLight.Tests.EditorMode.Systems
{
	/// <summary>Runs the actual migrated game and native asset DB; no network or replacement simulation types.</summary>
	public unsafe class OfflineMazeSessionTests
	{
		private const string Marker = "codex-native-Maze-smoke";
		private SessionRunner _runner;
		private QuantumGame Game => (QuantumGame)_runner.DeterministicGame;
		private bool _startingLocal;
		private bool _pollLocal;
		private int _pollCount;
		private AssetObject[] _assets;

		[SetUp]
		public void SetUp()
		{
			Assert.That(QuantumRunner.Default, Is.Null, "A smoke test must own its session.");
			QuantumRunnerUnityFactory.Init();
			_pollCount = 0;
			var database = QuantumUnityDB.Global;
			_assets = database.Entries.Where(entry => entry != null).Select(entry =>
			{
				try { return database.GetAsset(entry.Guid); }
				catch (Exception error)
				{
					throw new InvalidOperationException($"Native asset load failed: {entry.Path} ({entry.Guid})", error);
				}
			}).ToArray();
			Assert.That(_assets.Length, Is.GreaterThanOrEqualTo(213), "The native migrated asset catalog must be loaded.");
			QuantumCallback.SubscribeManual<CallbackGameStarted>(this, OnGameStarted);
			QuantumCallback.SubscribeManual<CallbackPollInput>(this, OnPollInput);
		}

		[TearDown]
		public void TearDown()
		{
			_pollLocal = false;
			_runner?.Shutdown();
			_runner = null;
			QuantumCallback.UnsubscribeListener(this);
		}

		private void OnGameStarted(CallbackGameStarted callback)
		{
			if (!_pollLocal || (!_startingLocal && (_runner == null || callback.Game != Game))) return;
			// The headless native factory has no Unity runner recording handler; start actual SDK recording here.
			callback.Game.StartRecordingInput();
			callback.Game.StartRecordingChecksums();
			// SDK3 requires explicit registration at every game-start callback.
			callback.Game.AddPlayer(0, new RuntimePlayer
			{
				PlayerId = "offline-native-smoke",
				PlayerName = "Native smoke",
				UnityId = "offline",
				PlayerLevel = 1,
				PlayerTrophies = 500,
				Cosmetics = Array.Empty<GameId>(),
				NormalizedSpawnPosition = FPVector2.Zero,
				PartyId = string.Empty
			});
		}

		private void OnPollInput(CallbackPollInput callback)
		{
			if (!_pollLocal || (_runner != null && callback.Game != Game)) return;
			var direction = (callback.Frame / 45 % 4) switch
			{
				0 => FPVector2.Up,
				1 => FPVector2.Right,
				2 => -FPVector2.Up,
				_ => -FPVector2.Right
			};
			var input = new Input();
			input.SetInput(direction, direction, false, 100);
			callback.SetInput(input, DeterministicInputFlags.Repeatable);
			_pollCount++;
		}

		private T RequiredAsset<T>() where T : AssetObject
		{
			var matches = _assets.OfType<T>().Where(a => a.Path != null && a.Path.Contains("AddressableResources/")).ToArray();
			Assert.That(matches.Length, Is.EqualTo(1), "Expected one native " + typeof(T).Name);
			return matches[0];
		}

		private RuntimeConfig CreateConfig()
		{
			var map = _assets.OfType<Map>().SingleOrDefault(m => m.Path.EndsWith("MazeMayhem", StringComparison.Ordinal));
			Assert.That(map, Is.Not.Null, "The actual MazeMayhem map must be in the native database.");
			Assert.That(map.StaticColliders2D?.Length, Is.GreaterThan(0), "Maze has no native collision data.");
			Assert.That(map.MapEntities?.Length, Is.GreaterThan(0), "Maze has no baked entity prototypes.");
			Assert.That(map.NavMeshLinks?.Length, Is.GreaterThan(0), "Maze has no native navmesh link.");
			return new RuntimeConfig
			{
				Seed = 42,
				Map = map,
				SimulationConfig = RequiredAsset<SimulationConfig>(),
				GameConfigs = RequiredAsset<QuantumGameConfigs>(),
				MapConfigs = RequiredAsset<QuantumMapConfigs>(),
				GameModeConfigs = RequiredAsset<QuantumGameModeConfigs>(),
				BotConfigs = RequiredAsset<QuantumBotConfigs>(),
				BotDifficultyConfigs = RequiredAsset<QuantumBotDifficultyConfigs>(),
				WeaponConfigs = RequiredAsset<QuantumWeaponConfigs>(),
				ConsumableConfigs = RequiredAsset<QuantumConsumableConfigs>(),
				ChestConfigs = RequiredAsset<QuantumChestConfigs>(),
				SpecialConfigs = RequiredAsset<QuantumSpecialConfigs>(),
				AssetConfigs = RequiredAsset<QuantumAssetConfigs>(),
				DestructibleConfigs = RequiredAsset<QuantumDestructibleConfigs>(),
				ShrinkingCircleConfigs = RequiredAsset<QuantumShrinkingCircleConfigs>(),
				EquipmentStatConfigs = RequiredAsset<QuantumEquipmentStatConfigs>(),
				BaseEquipmentStatConfigs = RequiredAsset<QuantumBaseEquipmentStatConfigs>(),
				StatConfigs = RequiredAsset<QuantumStatConfigs>(),
				EquipmentMaterialStatConfigs = RequiredAsset<QuantumEquipmentMaterialStatConfigs>(),
				ReviveConfigs = RequiredAsset<QuantumReviveConfigs>(),
				MatchConfigs = new SimulationMatchConfig
				{
					UniqueConfigId = Marker,
					GameModeID = "BattleRoyale",
					MapId = GameId.MazeMayhem.ToString(),
					TeamSize = 1,
					MinPlayersToStartMatch = 1,
					MatchType = MatchType.Custom,
					DisableBots = false
				}
			};
		}

		private void StartLocal()
		{
			var session = QuantumDeterministicSessionConfigAsset.DefaultConfig;
			Assert.That(session, Is.Not.Null, "The native SDK session config is required.");
			var arguments = new SessionRunner.Arguments
			{
				RuntimeConfig = CreateConfig(),
				SessionConfig = session,
				GameMode = DeterministicGameMode.Local,
				ClientId = "offline-native-smoke",
				RunnerId = Marker,
				PlayerCount = 1,
				Communicator = null,
				GameParameters = QuantumRunnerUnityFactory.CreateGameParameters,
				RunnerFactory = new DotNetRunnerFactory(),
				RecordingFlags = RecordingFlags.Input | RecordingFlags.Checksums
			};
			_pollLocal = true;
			_startingLocal = true;
			try { _runner = SessionRunner.Start(arguments); }
			finally { _startingLocal = false; }
		}

		private void TickUntil(int tick)
		{
			for (var attempts = 0; attempts < tick * 3 + 120 && (Game.Frames.Verified?.Number ?? -1) < tick; attempts++)
				_runner.Service(1d / QuantumDeterministicSessionConfigAsset.DefaultConfig.UpdateFPS);
			Assert.That(Game.Frames.Verified, Is.Not.Null);
			Assert.That(Game.Frames.Verified.Number, Is.GreaterThanOrEqualTo(tick), "The actual local/replay session must reach the requested fixed tick (the SDK begins at its native initial tick).");
		}

		private EntityRef RealPlayer(Frame frame)
		{
			foreach (var pair in frame.GetComponentIterator<PlayerCharacter>())
				if (pair.Component.RealPlayer) return pair.Entity;
			return EntityRef.None;
		}

		[Test]
		public void MazeSessionInitializesTicksMovesQueriesPhysicsAndExecutesBots()
		{
			StartLocal();
			TickUntil(30);
			var first = Game.Frames.Verified;
			Assert.That(first.SystemAnyEnabledSelf(typeof(PhysicsSystem2D)), Is.True);
			Assert.That(first.SystemAnyEnabledSelf(typeof(PlayerCharacterSystem)), Is.True);
			Assert.That(first.SystemAnyEnabledSelf(typeof(BotCharacterSystem)), Is.True);
			Assert.That(first.SystemAnyEnabledSelf(typeof(AiSystem)), Is.True);
			Assert.That(first.ComponentCount<BotCharacter>(), Is.GreaterThan(0));
			Assert.That(first.TryGetSingleton<GameContainer>(out _), Is.True);
			var player = RealPlayer(first);
			Assert.That(player.IsValid, Is.True, "AddPlayer must materialize a real player from the actual prototype.");
			var origin = first.Get<Transform2D>(player).Position;
			var moved = false;
			for (var target = 60; target <= 1200; target += 30)
			{
				TickUntil(target);
				var frame = Game.Frames.Verified;
				if (frame.Exists(player) && frame.TryGet<Transform2D>(player, out var transform) && transform.Position != origin)
					moved = true;
			}
			var final = Game.Frames.Verified;
			Assert.That(_pollCount, Is.GreaterThan(0), "Movement must enter through the real input callback.");
			Assert.That(moved, Is.True, "The real player never moved under supplied input.");
			var botUpdated = false;
			foreach (var pair in final.GetComponentIterator<BotCharacter>())
				botUpdated |= pair.Component.NextDecisionTime > FP._0 || pair.Component.NextLookForTargetsToShootAtTime > FP._0;
			Assert.That(botUpdated, Is.True, "Bots were created but no native bot update was observed.");
			var hits = final.Physics2D.OverlapShape(new Transform2D {Position = final.Map.StaticColliders2D[0].Position},
				Shape2D.CreateCircle(FP._1), -1, QueryOptions.HitStatics);
			Assert.That(hits.Count, Is.GreaterThan(0), "A native physics query must hit the baked map geometry.");
			Assert.That(final.CalculateChecksum(), Is.Not.Zero);
		}

		[Test]
		public void RecordedMazeSessionReplaysToIdenticalVerifiedChecksum()
		{
			StartLocal();
			TickUntil(600);
			var checksum = Game.Frames.Verified.CalculateChecksum();
			var replay = Game.GetRecordedReplay(includeChecksums: true);
			Assert.That(replay, Is.Not.Null);
			Assert.That(replay.Checksums, Is.Not.Null);
			Assert.That(replay.CreateInputProvider(), Is.Not.Null, "The actual SDK recording must produce replayable input/RPC data.");
			_pollLocal = false;
			_runner.Shutdown();
			_runner = null;
			var parameters = QuantumRunnerUnityFactory.CreateGameParameters;
			var arguments = new SessionRunner.Arguments
			{
				RuntimeConfig = parameters.AssetSerializer.ConfigFromByteArray<RuntimeConfig>(replay.RuntimeConfigData.Decode(), compressed: true),
				SessionConfig = replay.DeterministicConfig,
				GameMode = DeterministicGameMode.Replay,
				PlayerCount = 1,
				RunnerId = Marker + "-replay",
				Communicator = null,
				RunnerFactory = new DotNetRunnerFactory(),
				GameParameters = parameters,
				ReplayProvider = replay.CreateInputProvider(),
				InitialTick = replay.InitialTick,
				FrameData = replay.InitialFrameData
			};
			_runner = SessionRunner.Start(arguments);
			TickUntil(replay.LastTick);
			Assert.That(Game.Frames.Verified.CalculateChecksum(), Is.EqualTo(checksum),
				"Recorded native inputs/RPCs must reproduce the same full verified frame.");
		}
	}
}
