using GameLovers.Services.Commands;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Models;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Command used to process Logical purchases from the store, only used for InGame currencies
	/// Cash purchases are unavailable in the restored development shop.
	/// </summary>
	public class BuyFromStoreCommand : IGameCommandWithResult<bool>
	{
		public string CatalogItemId;

		public StoreItemData StoreItemData;
		private bool _wasPurchased;
		public bool GetResult() => _wasPurchased;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public async UniTask Execute(CommandExecutionContext ctx)
		{
			_wasPurchased = false;
			var catalogItem = await ctx.Services.CatalogService().GetCatalogItem(CatalogItemId);

			var playerDailyDealsConfiguration = ctx.Data.GetData<PlayerStoreData>().PlayerDailyDealsConfiguration;
			var dealStore = playerDailyDealsConfiguration?.SpecialStoreList.FirstOrDefault(s => s.IsActive && s.SpecialStoreItemIDs.Contains(CatalogItemId));

			var storeSetup = await ctx.Services.StoreService().GetItemPrice(CatalogItemId, dealStore?.SpecialStoreName);

			if (!ctx.Logic.PlayerStoreLogic().IsPurchasedAllowed(CatalogItemId, StoreItemData))
			{
				return;
			}

			// Validate every cost before any debit so a rejected multi-currency purchase leaves all balances unchanged.
			foreach (var price in storeSetup.Price)
				if (ctx.Logic.CurrencyLogic().GetCurrencyAmount(PlayfabCurrencies.GetCurrency(price.Key)) < price.Value) return;
			foreach (var price in storeSetup.Price)
				ctx.Logic.CurrencyLogic().DeductCurrency(PlayfabCurrencies.GetCurrency(price.Key), price.Value);

			ctx.Logic.RewardLogic().Reward(new[] {catalogItem});
			ctx.Logic.PlayerStoreLogic().UpdateLastPlayerPurchase(CatalogItemId, StoreItemData);

			var mainPrice = storeSetup.Price.FirstOrDefault();
			var msg = new PurchaseClaimedMessage
			{
				ItemPurchased = catalogItem,
				SupportingContentCreator = ctx.Logic.ContentCreatorLogic().SupportingCreatorCode.Value,
				PriceCurrencyId = mainPrice.Key,
				PricePaid = mainPrice.Value.ToString(),
			};
			_wasPurchased = true;
			ctx.Services.MessageBrokerService().PublishSafe(msg);
		}
	}
}
