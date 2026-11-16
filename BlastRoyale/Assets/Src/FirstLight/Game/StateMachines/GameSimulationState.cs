using MainInstaller = FirstLight.Game.Utils.MainInstaller;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FirstLight.FLogger;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes.Helpers;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Authentication;
using FirstLight.Game.TestCases;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Server.SDK.Modules;
using GameLovers.StatechartMachine;
using FirstLight.Game.Localization;
using Photon.Deterministic;
using Quantum;
using Quantum.Commands;
using Quantum.Systems;
using Unity.Services.Authentication;
using Assert = UnityEngine.Assertions.Assert;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Adventure Game Simulation State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class GameSimulationState
	{
		public static readonly IStatechartEvent SimulationStartedEvent = new StatechartEvent("Simulation Ready Event");
		public static readonly IStatechartEvent SimulationDestroyedEvent = new StatechartEvent("Simulation Destroyed Event");
		public static readonly IStatechartEvent LocalPlayerExitEvent = new StatechartEvent("Local Player Exit");
		private readonly BattleRoyaleState _battleRoyaleState;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IGameServices _services;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly IGameNetworkService _network;
		private readonly IInternalGameNetworkService _networkService;

		private IMatchServices _matchServices;
		private CancellationTokenSource _simulationLifetime;

		public GameSimulationState(IGameDataProvider gameDataProvider, IGameServices services, IInternalGameNetworkService networkService,
								   Action<IStatechartEvent> statechartTrigger)
		{
			_gameDataProvider = gameDataProvider;
			_services = services;
			_networkService = networkService;
			_statechartTrigger = statechartTrigger;
			_battleRoyaleState = new BattleRoyaleState(services, statechartTrigger);
		}

		/// <summary>
		/// Setups the Game Simulation state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");

			var battleRoyale = stateFactory.Nest("Battle Royale Mode");
			var startSimulation = stateFactory.State("Start Simulation");
			var stopSimulationForDisconnection = stateFactory.State("Stop Simulation");
			var simulationInitializationError = stateFactory.Transition("Stop Simulation Initialization Error");
			var disconnected = stateFactory.State("Disconnected");
			var disconnectedCritical = stateFactory.State("Disconnected Critical");

			initial.Transition().Target(startSimulation);
			initial.OnExit(SubscribeEvents);

			startSimulation.OnEnter(StartSimulation);
			startSimulation.Event(SimulationStartedEvent).Target(battleRoyale);
			startSimulation.Event(SimulationDestroyedEvent).Target(simulationInitializationError);
			startSimulation.Event(NetworkState.LeftRoomEvent).Target(final);
			startSimulation.Event(NetworkState.PhotonDisconnectedEvent).Target(stopSimulationForDisconnection);
			startSimulation.OnExit(() => CloseSwipeTransition().Forget());

			battleRoyale.Nest(_battleRoyaleState.Setup).Target(final);
			battleRoyale.Event(NetworkState.PhotonDisconnectedEvent).Target(stopSimulationForDisconnection);

			simulationInitializationError.Transition().OnTransition(MatchError).Target(final);

			stopSimulationForDisconnection.OnEnter(StopSimulation);
			stopSimulationForDisconnection.Event(NetworkState.JoinedRoomEvent).OnTransition(UnloadSimulation).Target(startSimulation);
			stopSimulationForDisconnection.Event(NetworkState.JoinRoomFailedEvent).OnTransition(UnloadSimulation).Target(disconnectedCritical);

			disconnected.Event(NetworkState.JoinedRoomEvent).Target(startSimulation);
			disconnected.Event(NetworkState.JoinRoomFailedEvent).Target(disconnectedCritical);

			final.OnEnter(UnsubscribeEvents);
		}

		/// <summary>
		/// For tutorial, we close the swipe transition when we actually get into the game, instead of
		/// closing at matchmaking screen opening in matchState. This is to avoid visual glitches with MM screen
		/// still persisting on screen for a second before game simulation
		/// </summary>
		private async UniTaskVoid CloseSwipeTransition()
		{
			if (_simulationLifetime == null) return;
			if (await UniTask.NextFrame(cancellationToken: _simulationLifetime.Token).SuppressCancellationThrow()) return;
			await _services.UIService.CloseScreen<SwipeTransitionScreenPresenter>(false);
		}

		private void SubscribeEvents()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_services.MessageBrokerService.Subscribe<QuitGameClickedMessage>(OnQuitGameScreenClickedMessage);

			QuantumEvent.SubscribeManual<EventOnAllPlayersJoined>(this, OnAllPlayersJoined);
			QuantumCallback.SubscribeManual<CallbackGameStarted>(this, OnGameStart);
			QuantumCallback.SubscribeManual<CallbackGameResynced>(this, OnGameResync);
			QuantumCallback.SubscribeManual<CallbackGameDestroyed>(this, OnGameDestroyed);
			QuantumCallback.SubscribeManual<CallbackPluginDisconnect>(this, OnPluginDisconnected);
		}

		private void UnsubscribeEvents()
		{
			CancelSimulationWaits();
			_services?.MessageBrokerService.UnsubscribeAll(this);
			//_matchServices?.SpectateService?.SpectatedPlayer?.StopObserving(TO_DELETE_WITH_NEW_START_SEQUENCE);
			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener(this);

			_matchServices = null;
		}

		private void UnloadSimulation()
		{
			CancelSimulationWaits();
			FLog.Verbose("Unloading Simulation");
			if (QuantumRunner.Default != null && QuantumRunner.Default.UnityObject != null)
			{
				QuantumRunner.Default.Shutdown();
			}
		}

		private void OnGameDestroyed(CallbackGameDestroyed cb)
		{
			CancelSimulationWaits();
			FLog.Verbose("Game Destroyed");
			_statechartTrigger(SimulationDestroyedEvent);
		}

		private void OnPluginDisconnected(CallbackPluginDisconnect callback)
		{
			if (callback.Reason == GameConstants.QuantumPluginDisconnectReasons.NOT_ENOUGH_PLAYERS)
			{
				_services.InGameNotificationService.QueueNotification(
					ScriptLocalization.UITMatchmaking.failed_to_find_players,
					InGameNotificationStyle.Error,
					InGameNotificationDuration.Long);
			}

			_services.MessageBrokerService.PublishSafe(new QuantumServerSimulationDisconnectedMessage()
			{
				Reason = callback.Reason
			});
		}

		private bool IsSpectator()
		{
			return _services.RoomService.IsLocalPlayerSpectator;
		}

		private void OnGameStart(CallbackGameStarted callback)
		{
			if (_simulationLifetime == null) return;
			FLog.Verbose("Game Start");
			SetPlayerMatchData(callback.Game);
			// Q3 also raises GameStarted after GameResynced; that callback already owns reconnect bootstrap.
			if (callback.IsResync) return;
			// paused on Start means waiting for Snapshot
			if (callback.Game.Session.IsPaused)
			{
				FLog.Verbose("Waiting for snapshot");
				return;
			}

			GameStartAsync(callback.Game, _simulationLifetime.Token).Forget();
			FLog.Verbose("Waiting for all players to join");
		}

		private async UniTaskVoid GameStartAsync(QuantumGame game, CancellationToken token)
		{
			FLog.Info("Game Started Message");
			if (!await PublishMatchStartedMessage(game, false, token)) return;
			if (await UniTask.WaitUntil(_services.UIService.IsScreenOpen<HUDScreenPresenter>,
				cancellationToken: token).SuppressCancellationThrow()) return;
			if (!IsCurrentGame(game)) return;

			var f = game.Frames.Verified;
			var entityRef = game.GetLocalPlayerEntityRef();
			if (f != null && entityRef.IsValid && f.TryGet<PlayerCharacter>(entityRef, out var pc))
			{
				if (!pc.RealPlayer)
				{
					_services.MessageBrokerService.PublishSafe(new LeftBeforeMatchFinishedMessage());
					_statechartTrigger(LocalPlayerExitEvent);
				}
			}
		}

		private void OnAllPlayersJoined(EventOnAllPlayersJoined callback)
		{
			FLog.Info("All Players Joined Event");
			// paused on Start means waiting for Snapshot
			if (callback.Game.Session.IsPaused)
			{
				return;
			}

			if (callback.GameFailed)
			{
				FLog.Error("All players joined with errors, finishing simulation");
				StopSimulation();
				return;
			}

			_statechartTrigger(SimulationStartedEvent);
		}

		private void OnGameResync(CallbackGameResynced callback)
		{
			if (_simulationLifetime == null) return;
			FLog.Info(
				$"Game Resync {callback.Game.Frames.Verified.Number} vs {_gameDataProvider.AppDataProvider.LastFrameSnapshot.Value.FrameNumber}");

			ResyncCoroutine(callback.Game, _simulationLifetime.Token).Forget();
		}

		private async UniTaskVoid ResyncCoroutine(QuantumGame game, CancellationToken token)
		{
			if (await UniTask.WaitUntil(() => IsCurrentGame(game),
				cancellationToken: token).SuppressCancellationThrow()) return;
			_statechartTrigger(SimulationStartedEvent);
			await PublishMatchStartedMessage(game, true, token);
		}

		private void OnQuitGameScreenClickedMessage(QuitGameClickedMessage message)
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.Yes,
				ButtonOnClick = QuitGameConfirmedClicked
			};

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.confirmation,
				ScriptLocalization.AdventureMenu.AreYouSureQuit,
				true, confirmButton);
		}

		private void QuitGameConfirmedClicked()
		{
			if (!_services.RoomService.IsLocalPlayerSpectator)
			{
				QuantumRunner.Default.Game.SendCommand(new PlayerQuitCommand());
			}

			_statechartTrigger(MatchState.MatchQuitEvent);
		}

		private void MatchError()
		{
			MatchState.SharedData.Simulation = MatchState.Data.SimulationResult.Error;
			FLog.Verbose("Raising Match Error");
		}

		private void StartSimulation()
		{
			CancelSimulationWaits();
			_simulationLifetime = new CancellationTokenSource();
			MatchState.SharedData.Simulation = MatchState.Data.SimulationResult.Success;
			Assert.IsNull(QuantumRunner.Default, "Simulation already running");

			FLog.Info($"Starting simulation from source {_services.NetworkService.JoinSource.ToString()}");

			var room = _services.RoomService.CurrentRoom;
			var startParams = room.GetDefaultStartParameters();
			startParams.OnShutdown = (_, _) =>
			{
				if (ReferenceEquals(_services.RoomService.CurrentRoom, room))
					_services.RoomService.LeaveRoom(false);
			};
			startParams.ClientId = _services.NetworkService.UserId;
			if (IsSpectator())
			{
				startParams.GameMode = DeterministicGameMode.Multiplayer;
			}

			FLog.Verbose("Starting " + ModelSerializer.Serialize(((RuntimeConfig)startParams.RuntimeConfig).MatchConfigs).Value);

			var snapShot = _gameDataProvider.AppDataProvider.LastFrameSnapshot.Value;
			if (snapShot is {FrameNumber: > 0, Offline: true} && _services.RoomService.CurrentRoom.IsOffline)
			{
				FLog.Info("Restoring Local Snapshot");
				FLog.Verbose(snapShot);
				startParams.FrameData = snapShot.SnapshotBytes;
				startParams.InitialTick = snapShot.FrameNumber;
			}

			_networkService.SetLastRoom();

			try
			{
				QuantumRunner.StartGame(startParams);
			}
			catch
			{
				CancelSimulationWaits();
				if (ReferenceEquals(_services.RoomService.CurrentRoom, room))
					_services.RoomService.LeaveRoom(false);
				throw;
			}

			_services.MessageBrokerService.PublishSafe(new MatchSimulationStartedMessage());
		}

		private void CleanupFrame()
		{
			_matchServices.FrameSnapshotService.ClearFrameSnapshot();
		}

		/// <summary>
		/// This StopSimulation method is only used for disconnection flow.
		/// There is another StopSimulation method in MatchState which handles stopping simulation once the player
		/// has reached the complete end of flow, past any disconnection cases.
		/// </summary>
		private void StopSimulation()
		{
			CancelSimulationWaits();
			FLog.Verbose("Stopping Simulation");
			if (QuantumRunner.Default == null || QuantumRunner.Default.UnityObject == null)
			{
				FLog.Verbose("Simulation already destroyed");
				return;
			}

			_services.MessageBrokerService.PublishSafe(new SimulationEndedMessage
			{
				Game = QuantumRunner.Default.Game,
				Reason = SimulationEndReason.Disconnected
			});
			QuantumRunner.ShutdownAll();
			_services.RoomService.LeaveRoom(false);
		}

		private void CancelSimulationWaits()
		{
			_simulationLifetime?.Cancel();
			_simulationLifetime?.Dispose();
			_simulationLifetime = null;
		}

		private static bool IsCurrentGame(QuantumGame game)
		{
			return QuantumRunner.Default.IsDefinedAndRunning(false) &&
				ReferenceEquals(QuantumRunner.Default.Game, game);
		}

		private async UniTask<bool> PublishMatchStartedMessage(QuantumGame game, bool isResync, CancellationToken token)
		{
			if (await UniTask.WaitUntil(() => IsSimulationReady(game),
				cancellationToken: token).SuppressCancellationThrow()) return false;
			if (!IsCurrentGame(game)) return false;

			if (!isResync)
			{
				_services.AnalyticsService.MatchCalls.MatchStart();
			}

			_services.MessageBrokerService.PublishSafe(new MatchStartedMessage {Game = game, IsResync = isResync});
			return true;
		}

		private unsafe bool IsSimulationReady(QuantumGame game)
		{
			if (!IsCurrentGame(game)) return false;

			if ((!IsSpectator() && game.GetLocalPlayers().Count == 0) ||
				game.Frames.Verified == null || game.Frames.Verified.Number == 0)
			{
				return false;
			}

			if (!game.Frames.Verified.Unsafe.TryGetPointerSingleton<GameContainer>(out var container))
			{
				return false;
			}

			if (!IsSpectator())
			{
				var localPlayerData = container->PlayersData[game.GetLocalPlayerRef()];
				if (!localPlayerData.IsValid)
				{
					return false;
				}

				if (!game.Frames.Verified.Exists(localPlayerData.Entity))
				{
					return false;
				}
			}

			// Wait for the mode-specific systems to initialize before publishing match readiness.
			var hasCircle = game.Frames.Verified.SystemAnyEnabledSelf<ShrinkingCircleSystem>();
			if (hasCircle && !game.Frames.Verified.Unsafe.TryGetPointerSingleton<ShrinkingCircle>(out var circle))
			{
				return false;
			}

			return true;
		}

		private void SetPlayerMatchData(QuantumGame game)
		{
			if (IsSpectator())
			{
				return;
			}

			var spawnPosition = _services.RoomService.CurrentRoom.LocalPlayerProperties.DropPosition.Value;
			var equippedCosmetics = _gameDataProvider.CollectionDataProvider
				.GetCollectionsCategories()
				.Select(id => _gameDataProvider.CollectionDataProvider.GetEquipped(id))
				.Where(data => data != null)
				.Select(data => data.Id)
				.ToArray();

			var config = _services.ConfigsProvider.GetConfig<AvatarCollectableConfig>();
			var avatarUrl = AvatarHelpers.GetAvatarUrl(_gameDataProvider.CollectionDataProvider.GetEquipped(CollectionCategories.PROFILE_PICTURE),
				config);
			var useBotBehaviour = (FLGTestRunner.Instance.IsRunning() && FLGTestRunner.Instance.UseBotBehaviour) ||
				FeatureFlags.GetLocalConfiguration().UseBotBehaviour;

#if DEBUG

			if (DebugOptions.Current != null && DebugOptions.Current.DontSendPlayerData)
			{
				FLog.Warn("Not sending runtime player data. This is a hack for the local player, not an exception!");
				return;
			}
#endif
			FLog.Info("Sending player runtime data");
			game.AddPlayer(0, new RuntimePlayer
			{
				PlayerId = AuthenticationService.Instance.PlayerId,
				UnityId = AuthenticationService.Instance.PlayerId,
				PlayerName = _services.AuthService.GetPrettyLocalPlayerName(),
				Cosmetics = equippedCosmetics,
				PlayerLevel = _gameDataProvider.PlayerDataProvider.Level.Value,
				PlayerTrophies = _gameDataProvider.PlayerDataProvider.Trophies.Value,
				NormalizedSpawnPosition = spawnPosition.ToFPVector2(),
				LeaderboardRank = (uint) _services.LeaderboardService.CurrentRankedEntry.Position,
				PartyId = _services.TeamService.GetTeamForPlayer(_services.RoomService.CurrentRoom.LocalPlayer),
				AvatarUrl = avatarUrl,
				UseBotBehaviour = useBotBehaviour,
				TeamColor = _services.TeamService.GetTeamMemberColorIndex(_services.RoomService.CurrentRoom.LocalPlayer)
			});
		}

		[Conditional("DEBUG")]
		private void DebugSimulation() => FLog.Verbose(QuantumRunner.Default.GetSimulationDebugString());
	}
}
