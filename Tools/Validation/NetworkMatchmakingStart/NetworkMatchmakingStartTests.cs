using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.StateMachines;
using FirstLight.Game.Utils;
using FirstLightServerSDK.Services;
using GameLovers.GameData;
using GameLovers.Services;
using GameLovers.StatechartMachine;
using NSubstitute;
using NUnit.Framework;
using Photon.Realtime;
using Quantum;
using Unity.Services.Authentication;
using UnityEngine.TestTools;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Network
{
    public sealed class NetworkMatchmakingStartTests
    {
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private object _previousAuth;
        private NativeFixture _auth;
        private NetworkState _networkState;
        private MessageBrokerService _broker;
        private RealtimeClient _client;
        private Statechart _chart;
        private MatchRoomSetup _setup;
        private int _requests;
        private readonly List<string> _publicationStates = new();
        private readonly IStatechartEvent _play = new StatechartEvent("Actual play trigger");
        private IMatchmakingService _matchmaking;

        [SetUp] public void SetUp()
        {
            typeof(FirstLight.FLogger.FLog).GetMethod("Init").Invoke(null, null);
            _previousAuth = typeof(AuthenticationService).GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            _auth = new NativeFixture();
            AuthenticationService.Instance = _auth.Service;
            _auth.Service.SignInAnonymouslyAsync().GetAwaiter().GetResult();
            _requests = 0;
            _publicationStates.Clear();
            _broker = new MessageBrokerService();
            _client = new RealtimeClient(); // Genuine SDK client; no socket or cloud operation is executed.
            var network = Substitute.For<IInternalGameNetworkService>();
            network.QuantumClient.Returns(_client);
            network.JoinSource.Returns(new ObservableField<JoinRoomSource>());
            ((IGameNetworkService)network).LastUsedSetup.Returns(new ObservableField<MatchRoomSetup>());
            var app = Substitute.For<IAppDataProvider>();
            app.LastFrameSnapshot.Returns(new ObservableField<FrameSnapshot>());
            var remote = Substitute.For<IRemoteConfigProvider>();
            remote.GetConfigVersion().Returns(1);
            var data = Substitute.For<IGameDataProvider>();
            data.AppDataProvider.Returns(app);
            data.RemoteConfigProvider.Returns(remote);
            var services = Substitute.For<IGameServices>();
            services.MessageBrokerService.Returns(_broker);
            services.RoomService.Returns(Substitute.For<IRoomService>());
            var notifications = new InGameNotificationService(null);
            typeof(InGameNotificationService).GetField("_isProcessingQueue", Private).SetValue(notifications, true);
            typeof(InGameNotificationService).GetField("_currentInGameNotificationMessage", Private).SetValue(notifications,
                new InGameNotificationEntry { Message = "Controlled local notification queue" });
            services.InGameNotificationService.Returns(notifications);
            var backend = Substitute.For<IGameBackendService>();
            backend.UpdateConfigs(Arg.Any<Type[]>()).Returns(UniTask.FromResult(false));
            services.GameBackendService.Returns(backend);
            _matchmaking = Substitute.For<IMatchmakingService>();
            services.MatchmakingService.Returns(_matchmaking);
            _setup = new MatchRoomSetup { SimulationConfig = new SimulationMatchConfig
                { MapId = GameId.MazeMayhem.ToString(), UniqueConfigId = "native-local-regression", TeamSize = 1 } };
            _matchmaking.JoinMatchmaking(Arg.Any<MatchRoomSetup>()).Returns(call =>
            {
                _requests++;
                _publicationStates.Add(_chart.CurrentState);
                // Only the first callback emits, bounding the unpatched native graph's reentrant duplicate.
                if (_requests == 1) _broker.PublishSafe(new MatchmakingJoinedMessage
                { Config = new JoinedMatchmaking { RoomSetupBase64 = Convert.ToBase64String(_setup.ToByteArray()) } });
                return UniTask.CompletedTask;
            });
            _matchmaking.LeaveMatchmaking().Returns(_ =>
            {
                _broker.PublishSafe(new MatchmakingLeftMessage());
                return UniTask.CompletedTask;
            });
            _networkState = new NetworkState(data, services, network, e => _chart.Trigger(e));
            _chart = new Statechart(factory =>
            {
                var initial = factory.Initial("Initial");
                factory.Final("Final");
                var home = factory.State("Home");
                var checks = factory.Choice("Play choice");
                var waiting = factory.State("Waiting");
                var matched = factory.State("Matched");
                initial.Transition().Target(home);
                home.Event(_play).Target(checks);
                // Exact production shape: a choice transition invokes StartRandomMatchmaking,
                // whose immediately-completed service publishes a second native graph event.
                checks.Transition().Condition(() => false).Target(home);
                checks.Transition().OnTransition(Start).Target(waiting);
                waiting.Event(NetworkState.JoinedPlayfabMatchmaking).Target(matched);
                waiting.Event(NetworkState.CanceledMatchmakingEvent).Target(home);
            });
            typeof(NetworkState).GetMethod("SubscribeEvents", Private).Invoke(_networkState, null);
            _chart.Run();
        }

        [TearDown] public void TearDown()
        {
            typeof(NetworkState).GetMethod("UnsubscribeEvents", Private).Invoke(_networkState, null);
            _client.RemoveCallbackTarget(_networkState);
            typeof(AuthenticationService).GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, _previousAuth);
            UgsAccountSession.Invalidate();
        }

        [UnityTest] public IEnumerator SynchronousBundledMatchmakingPublishesAfterNativeChoiceCommits() => UniTask.ToCoroutine(async () =>
        {
            _chart.Trigger(_play);
            Assert.That(_requests, Is.Zero);
            Assert.That(_chart.CurrentState, Is.EqualTo("Waiting"));
            await NextFrames();
            Assert.That(_requests, Is.EqualTo(1));
            Assert.That(_publicationStates, Is.EqualTo(new[] { "Waiting" }));
            Assert.That(_chart.CurrentState, Is.EqualTo("Matched"));
        });

        [UnityTest] public IEnumerator RepeatedStartsBeforeNextFrameDispatchOnlyOneReservation() => UniTask.ToCoroutine(async () =>
        {
            _chart.Trigger(_play);
            Start();
            Start();
            await NextFrames();
            Assert.That(_requests, Is.EqualTo(1));
        });

        [UnityTest] public IEnumerator CanceledDeferredStartCannotDispatchOrReleaseANewerStart() => UniTask.ToCoroutine(async () =>
        {
            _chart.Trigger(_play);
            _broker.PublishSafe(new MatchmakingCancelMessage());
            Assert.That(_chart.CurrentState, Is.EqualTo("Home"));
            _chart.Trigger(_play);
            Start();
            await NextFrames();
            Assert.That(_requests, Is.EqualTo(1));
            Assert.That(_publicationStates, Is.EqualTo(new[] { "Waiting" }));
        });

        [UnityTest] public IEnumerator NativeSignedOutAccountCannotDispatchDeferredReservation() => UniTask.ToCoroutine(async () =>
        {
            _chart.Trigger(_play);
            _auth.Service.SignOut();
            await NextFrames();
            Assert.That(_requests, Is.Zero);
            Assert.That((bool)typeof(NetworkState).GetField("_matchmakingStartPending", Private).GetValue(_networkState), Is.False);
        });

        [UnityTest] public IEnumerator NativePhotonJoiningStateRejectsAnotherStart() => UniTask.ToCoroutine(async () =>
        {
            typeof(RealtimeClient).GetProperty("State", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SetValue(_client, ClientState.Joining);
            Start();
            await NextFrames();
            Assert.That(_requests, Is.Zero);
        });

        private void Start() => typeof(NetworkState).GetMethod("StartRandomMatchmaking", Private).Invoke(_networkState, new object[] { _setup });
        private static async UniTask NextFrames() { await UniTask.NextFrame(); await UniTask.NextFrame(); }
    }
}
