using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Server.SDK.Models;
using FirstLight.Game.Data;
using FirstLight.Game.Services;

// Test-only collaborators. The linked production command and name helper are not copied or reimplemented.
namespace TestDoubles
{
    public sealed class Currency
    {
        public ulong Coin, Bucks;
        public ulong GetCurrencyAmount(Quantum.GameId id) => id == Quantum.GameId.Coin ? Coin : Bucks;
        public void DeductCurrency(Quantum.GameId id, ulong amount) { if (id == Quantum.GameId.Coin) Coin -= amount; else Bucks -= amount; }
    }
    public sealed class RewardSink { public int Grants; public void Reward(object[] items) => Grants++; }
    public sealed class PlayerStore
    {
        public bool Allowed = true; public int Tracked;
        public bool IsPurchasedAllowed(string id, StoreItemData item) => Allowed;
        public void UpdateLastPlayerPurchase(string id, StoreItemData item) => Tracked++;
    }
    public sealed class Creator { public StringField SupportingCreatorCode = new(); }
    public sealed class StringField { public string Value = ""; }
    public sealed class Catalog { public Task<object> GetCatalogItem(string id) => Task.FromResult(new object()); }
    public sealed class Store
    {
        public Task<Prices> GetItemPrice(string id, string store) => Task.FromResult(new Prices());
    }
    public sealed class Prices { public Dictionary<string, uint> Price = new() { ["CN"] = 60, ["BB"] = 20 }; }
    public sealed class Broker { public void PublishSafe(object message) {} }
    public sealed class Data : IDataProvider
    {
        public T GetData<T>() where T : class => new PlayerStoreData() as T;
        public bool TryGetData<T>(out T data) where T : class { data = GetData<T>(); return data != null; }
        public object GetData(Type type) => new PlayerStoreData();
        public bool TryGetData(Type type, out object data) { data = GetData(type); return true; }
        public IEnumerable<Type> GetKeys() => new[] { typeof(PlayerStoreData) };
    }
    public sealed class Fixture
    {
        public CommandExecutionContext Context;
        public Currency Currency; public RewardSink Reward; public PlayerStore PlayerStore;
        public static Fixture Create(ulong coins, ulong bucks)
        {
            var value = new Fixture { Currency = new Currency { Coin = coins, Bucks = bucks }, Reward = new(), PlayerStore = new() };
            var logic = new LogicContainer(); logic.Add(value.Currency); logic.Add(value.Reward); logic.Add(value.PlayerStore); logic.Add(new Creator());
            var services = new ServiceContainer(); services.Add(new Catalog()); services.Add(new Store()); services.Add(new Broker());
            value.Context = new CommandExecutionContext(logic, services, new Data()); return value;
        }
    }
}
namespace FirstLight.Game.Logic
{
    public interface IGameDataProvider { PlayerProvider PlayerDataProvider { get; } }
    public sealed class PlayerProvider { public PlayerFlags Flags; }
    public static class Containers
    {
        public static TestDoubles.Currency CurrencyLogic(this LogicContainer value) => value.Get<TestDoubles.Currency>();
        public static TestDoubles.RewardSink RewardLogic(this LogicContainer value) => value.Get<TestDoubles.RewardSink>();
        public static TestDoubles.PlayerStore PlayerStoreLogic(this LogicContainer value) => value.Get<TestDoubles.PlayerStore>();
        public static TestDoubles.Creator ContentCreatorLogic(this LogicContainer value) => value.Get<TestDoubles.Creator>();
        public static TestDoubles.Catalog CatalogService(this ServiceContainer value) => value.Get<TestDoubles.Catalog>();
        public static TestDoubles.Store StoreService(this ServiceContainer value) => value.Get<TestDoubles.Store>();
        public static TestDoubles.Broker MessageBrokerService(this ServiceContainer value) => value.Get<TestDoubles.Broker>();
    }
}
namespace FirstLight.Game.Data
{
    [Flags] public enum PlayerFlags { FLGOfficial = 1, DiscordMod = 2 }
    public sealed class PlayerStoreData { public Deals PlayerDailyDealsConfiguration; }
    public sealed class Deals { public List<Deal> SpecialStoreList; }
    public sealed class Deal { public bool IsActive; public List<string> SpecialStoreItemIDs; public string SpecialStoreName; }
}
namespace FirstLight.Game.Services { public sealed class StoreItemData {} public interface IAuthService { string RawLocalPlayerName { get; } } }
namespace FirstLight.Game.Utils { public static class MainInstaller { public static T Resolve<T>() => default; } }
namespace FirstLight.Game.Messages { public sealed class PurchaseClaimedMessage { public object ItemPurchased; public string SupportingContentCreator, PriceCurrencyId, PricePaid; } }
namespace FirstLight.Models { public static class PlayfabCurrencies { public static Quantum.GameId GetCurrency(string key) => key == "CN" ? Quantum.GameId.Coin : Quantum.GameId.Bucks; } }
namespace Quantum { public enum GameId { Coin, Bucks } }
namespace GameLovers.Services.Commands { }
