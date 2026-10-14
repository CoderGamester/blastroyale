using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Authentication;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.Commands;
using GameLovers.GameData;
using GameLovers.Services;
using NUnit.Framework;

namespace FirstLight.Tests.EditorMode.Authentication
{
    public sealed class AccountStateIsolationTests
    {
        [OneTimeSetUp] public void InitializeNativeLogger() => typeof(FirstLight.FLogger.FLog).GetMethod("Init").Invoke(null, null);

        [Test] public void StoreAccountSwitchRebindsPurchasePolicyToNewAccount()
        {
            var data = new ClientDataService();
            var a = new PlayerStoreData();
            a.TrackedStorePurchases.Add(new StorePurchaseData("limited-product", false));
            data.AddData(a);
            var logic = new PlayerStoreLogic(null, data);
            logic.Init();
            var observer = logic.TrackedPlayerPurchases;
            var rules = new StoreItemData { MaxAmount = 1 };
            Assert.That(logic.IsPurchasedAllowed("limited-product", rules), Is.False);
            var b = new PlayerStoreData();
            data.AddData(b);
            logic.ReInit();
            Assert.That(logic.TrackedPlayerPurchases, Is.SameAs(observer));
            Assert.That(logic.GetTrackedPlayerPurchases(), Is.Empty);
            Assert.That(logic.IsPurchasedAllowed("limited-product", rules), Is.True);
            logic.UpdateLastPlayerPurchase("limited-product", rules);
            Assert.That(logic.IsPurchasedAllowed("limited-product", rules), Is.False);
            Assert.That(a.TrackedStorePurchases.Single().AmountPurchased, Is.EqualTo(1));
            Assert.That(b.TrackedStorePurchases.Single().CatalogItemId, Is.EqualTo("limited-product"));
        }

        [Test] public void StoreAccountSwitchRetainsSubscribersAndPublishesNewBackingData()
        {
            var data = new ClientDataService();
            var a = new PlayerStoreData();
            a.TrackedStorePurchases.Add(new StorePurchaseData("a-product", false));
            data.AddData(a);
            var logic = new PlayerStoreLogic(null, data);
            logic.Init();
            var observed = new List<string>();
            var reader = logic.TrackedPlayerPurchases;
            reader.Observe((_, _, value, _) => observed.Add(value.CatalogItemId));
            var b = new PlayerStoreData();
            b.TrackedStorePurchases.Add(new StorePurchaseData("b-product", false));
            data.AddData(b);
            logic.ReInit();
            Assert.That(reader, Is.SameAs(logic.TrackedPlayerPurchases));
            Assert.That(observed, Is.EqualTo(new[] { "b-product" }));
            ((IObservableList<StorePurchaseData>)reader).Add(new StorePurchaseData("b-next", false));
            Assert.That(observed.Last(), Is.EqualTo("b-next"));
            Assert.That(b.TrackedStorePurchases.Count, Is.EqualTo(2));
            Assert.That(a.TrackedStorePurchases.Single().CatalogItemId, Is.EqualTo("a-product"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task DifferentLoadedAccountDoesNotInheritPreviousTutorialCompletion(bool newGuest)
        {
            var data = new ClientDataService();
            var players = new PlayerLogic(null, data);
            var store = new PlayerStoreLogic(null, data);
            var initializer = new NativeLogicInitializer(players, store);
            var commands = new NativeCommands(players, data);
            var auth = new AuthService(initializer, null, data, null, commands, new MessageBrokerService(), null);
            var a = State(TutorialSection.FIRST_GUIDE_MATCH);
            Load(auth, a);
            var hooks = (List<IAuthenticationHook>)typeof(AuthService).GetField("_authenticationHooks", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(auth);
            // Exact source ordering: hooks capture pre-login state, then cloud models replace it,
            // logic rebinds, and AfterFetchedState runs for the newly signed-in identity.
            foreach (var hook in hooks) await hook.BeforeAuthentication(true);
            var b = State(TutorialSection.NONE);
            Load(auth, b);
            foreach (var hook in hooks) await hook.AfterFetchedState(new PlayFab.ClientModels.LoginResult
                { PlayFabId = "synthetic-account-b", NewlyCreated = newGuest });
            Assert.That(data.GetData<TutorialData>().TutorialSections, Is.EqualTo(TutorialSection.NONE));
            Assert.That(data.GetData<PlayerData>().MigratedGuestData, Is.False);
            Assert.That(commands.Count, Is.Zero);
            Assert.That(initializer.Initializations, Is.EqualTo(1));
            Assert.That(initializer.Reinitializations, Is.EqualTo(1));
        }

        private static ServerState State(TutorialSection completed)
        {
            var state = new ServerState();
            foreach (var type in UnityCloudPlayerStateBackend.ModelTypes)
            {
                var model = Activator.CreateInstance(type);
                if (model is TutorialData tutorial) tutorial.TutorialSections = completed;
                if (model is FirstLight.Game.Data.RngData rng) { rng.Seed = 17; rng.State = RngUtils.GenerateRngState(rng.Seed); }
                state.UpdateModel(model);
            }
            return state;
        }
        private static void Load(AuthService auth, ServerState state) =>
            typeof(AuthService).GetMethod("UpdatePlayerDataAndLogic", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(auth, new object[] { state, false });

        // Game-only orchestration collaborators route through actual PlayerLogic/PlayerStoreLogic
        // and MigrateGuestDataCommand. No Unity, UGS, observable or SDK type is redefined.
        private sealed class NativeLogicInitializer : IGameLogicInitializer
        {
            private readonly PlayerLogic _player;
            private readonly PlayerStoreLogic _store;
            public int Initializations, Reinitializations;
            public NativeLogicInitializer(PlayerLogic player, PlayerStoreLogic store) { _player=player; _store=store; }
            public void Init() { Initializations++; _player.Init(); _store.Init(); }
            public void ReInit() { Reinitializations++; _player.ReInit(); _store.ReInit(); }
        }
        private sealed class NativeCommands : IGameCommandService
        {
            private readonly CommandExecutionContext _context;
            public int Count;
            public NativeCommands(PlayerLogic player, ClientDataService data)
            {
                var logic = new LogicContainer();
                logic.Add<IPlayerLogic>(player);
                _context = new CommandExecutionContext(logic, new ServiceContainer(), data);
            }
            public void ExecuteCommand<T>(T command) where T : IGameCommand
            {
                Count++;
                command.Execute(_context).GetAwaiter().GetResult();
            }
            public T ExecuteCommandWithResult<T>(IGameCommandWithResult<T> command) => throw new NotSupportedException();
        }
    }
}
