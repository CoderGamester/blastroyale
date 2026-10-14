using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules;
using PlayFab.ClientModels;
using Quantum;

namespace FirstLight.Game.Services
{
	/// <summary>Read-only shop definitions; purchase balances are held in the persisted PlayerData model.</summary>
	public interface IShopCatalogBackend
	{
		UniTask<GetCatalogItemsResult> GetCatalog(string catalogName);
		UniTask<GetStoreItemsResult> GetStore(string catalogName, string storeName);
	}

	public sealed class PlayFabShopCatalogBackend : IShopCatalogBackend
	{
		public UniTask<GetCatalogItemsResult> GetCatalog(string catalogName)
		{
			if (!FLEnvironment.Configuration.UseBundledShop)
				return AsyncPlayfabAPI.GetCatalogItems(new GetCatalogItemsRequest { CatalogVersion = catalogName });
			return UniTask.FromResult(new GetCatalogItemsResult
			{
				Catalog = new List<CatalogItem>
				{
					new CatalogItem
					{
						ItemId = "development-blast-bucks", DisplayName = "10 Blast Bucks", ItemClass = "Currency",
						Description = "Development exchange: 100 earned coins for 10 Blast Bucks.",
						CatalogVersion = catalogName,
						CustomData = ModelSerializer.Serialize(new LegacyItemData { RewardId = GameId.BlastBuck, Value = 10 }).Value
					}
				}
			});
		}

		public UniTask<GetStoreItemsResult> GetStore(string catalogName, string storeName)
		{
			if (!FLEnvironment.Configuration.UseBundledShop)
				return AsyncPlayfabAPI.GetStoreItems(new GetStoreItemsRequest { CatalogVersion = catalogName, StoreId = storeName });
			var items = new List<StoreItem>();
			if (storeName == "MainShop") items.Add(new StoreItem
			{
				ItemId = "development-blast-bucks",
				VirtualCurrencyPrices = new Dictionary<string, uint> { ["CN"] = 100 },
				RealCurrencyPrices = new Dictionary<string, uint>(),
				CustomData = ModelSerializer.Serialize(new StoreItemData { Category = "Currency", MaxAmount = 0 }).Value
			});
			return UniTask.FromResult(new GetStoreItemsResult { CatalogVersion = catalogName, StoreId = storeName, Store = items });
		}
	}
}
