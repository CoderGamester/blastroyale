using FirstLight.Game.Commands;
using FirstLight.Game.Services.Authentication;
using FirstLight.Server.SDK.Modules.Commands;
using TestDoubles;

var cases = new[] { ("abc", "abc"), ("Miguel", "Miguel"), ("Miguel#1234", "Miguel"), ("Miguel#Clash", "Miguel#Clash"), ((string)null, (string)null) };
foreach (var (input, expected) in cases)
    Require(AuthServiceNameExtensions.TrimPlayfabDisplayName(input) == expected, "public name: " + (input ?? "null"));
var rejected = Fixture.Create(100, 0);
var command = new BuyFromStoreCommand { CatalogItemId = "fixture", StoreItemData = new() };
command.Execute(rejected.Context).GetAwaiter().GetResult();
Require(!command.GetResult() && rejected.Currency.Coin == 100 && rejected.Currency.Bucks == 0 && rejected.Reward.Grants == 0, "multi-currency rejection is atomic");
var accepted = Fixture.Create(100, 50);
command.Execute(accepted.Context).GetAwaiter().GetResult();
Require(command.GetResult() && accepted.Currency.Coin == 40 && accepted.Currency.Bucks == 30 && accepted.Reward.Grants == 1 && accepted.PlayerStore.Tracked == 1, "accepted purchase debits and grants once");
accepted.PlayerStore.Allowed = false;
command.Execute(accepted.Context).GetAwaiter().GetResult();
Require(!command.GetResult() && accepted.Currency.Coin == 40 && accepted.Currency.Bucks == 30 && accepted.Reward.Grants == 1, "purchase-limit rejection clears previous result and leaves balances/rewards unchanged");
Console.WriteLine("Backend isolation: 8 behavior checks passed (actual name/purchase source; injected model/service doubles; real UniTask assembly).");
static void Require(bool condition, string name) { if (!condition) throw new Exception("FAILED: " + name); }
