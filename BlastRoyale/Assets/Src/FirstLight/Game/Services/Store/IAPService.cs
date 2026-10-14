using MainInstaller = FirstLight.Game.Utils.MainInstaller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Commands.OfflineCommands;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Domains.HomeScreen;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services.Analytics;
using FirstLight.Game.StateMachines;
using FirstLight.Game.Utils;
using FirstLight.Models;
using GameLovers.Services;
using FirstLightServerSDK.Services;
using FirstLight.Game.Localization;
using Newtonsoft.Json;
using PlayFab.ClientModels;
using Quantum;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Wrapper product class that holds game specific data
	/// </summary>
	public class GameProduct
	{
		public PlayfabProductConfig PlayfabProductConfig;
		public ItemData GameItem;

		public (GameId item, uint amt) GetPrice()
		{
			var cost = PlayfabProductConfig.StoreItem.VirtualCurrencyPrices.FirstOrDefault();

			return (PlayfabCurrencies.GetCurrency(cost.Key), cost.Value);
		}

		public string ID => PlayfabProductConfig.CatalogItem.ItemId;

		public string GetDisplayPrice()
		{
			var price = GetPrice();
			if (price.item == GameId.RealMoney) return string.Empty;
			return $"{price.amt} {CurrencyItemViewModel.GetRichTextIcon(price.item)}";
		}
	}

	/// <summary>
	/// Shop Category which is a division of products.
	/// This is derived from the "ItemClass" field in playfab
	/// </summary>
	public class GameProductCategory
	{
		public string Name;
		public List<GameProduct> Products = new ();

		public bool IsHidden => Name == "Hidden";
	}

	public class GameProductsBundle
	{
		public string Name;
		public GameProduct Bundle;
		public List<GameProduct> BundleProducts = new ();
	}

	/// <summary>
	/// Main entrypoint for our IAP service
	/// Provides bundled earned-currency purchases; cash purchases require a trusted receipt service
	/// </summary>
	public interface IIAPService
	{
		/// <summary>
		/// True after the real bundled catalog has loaded and its products are ready.
		/// </summary>
		bool Initialized { get; }

		/// <summary>
		/// Refences our remote catalog store
		/// </summary>
		IStoreService RemoteCatalogStore { get; }

		/// <summary>
		/// Buys the product defined by the given <paramref name="unityStoreId"/>
		/// </summary>
		void BuyProduct(GameProduct product);

		/// <summary>
		///  Check if there is a pending purchase in background, this is NOT reliable between restarts, you should handle when using this method.
		/// </summary>
		bool IsPending(GameProduct product);

		/// <summary>
		/// Gets all available products on the shop
		/// </summary>
		IReadOnlyCollection<GameProductCategory> AvailableProductCategories { get; }

		/// <summary>
		/// Gets all available products on the shop
		/// </summary>
		IReadOnlyCollection<GameProductsBundle> AvailableGameProductBundles { get; }

		/// <summary>
		/// Find GameProduct with ItemId
		/// </summary>
		IReadOnlyCollection<GameProduct> AvailableProducts { get; }

		/// <summary>
		/// Mark all the new store items as seen by the player
		/// </summary>
		void MarkAllNewStoreItemsAsSeen();

		/// <summary>
		/// Returns true if is there any new update on the store that player didn't see it
		/// </summary>
		bool HasStoreItemsUpdate(string filterCategory = null, string filterProduct = null);

		/// <summary>
		/// Event fired everytime a purchase finishes processing
		/// </summary>
		event PurchaseFinishedDelegate PurchaseFinished;

		delegate void PurchaseFinishedDelegate(string productId, ItemData data, bool succeeded, StorePurchaseFailureData failureData);

		/// <summary>
		/// Display all the failed transactions popup and wait for it to close
		/// </summary>
		/// <returns></returns>
		public UniTask ShowQueuedTransactionFailedMessagesAndWait();
	}

	public class IAPService : IIAPService
	{
		private PlayfabStoreService _playfab;
		public event IIAPService.PurchaseFinishedDelegate PurchaseFinished;

		private Dictionary<string, GameProductCategory> _lastStoreLoadedDifference = new ();
		private Dictionary<string, GameProductCategory> _availableProductCategories = new ();
		private Dictionary<string, GameProductsBundle> _availableGameProductBundles = new ();
		private List<GameProduct> _availableProducts = new ();
		private readonly IGameCommandService _commandService;
		private readonly IMessageBrokerService _msgBroker;
		private readonly IHomeScreenService _homeScreen;
		private readonly LocalPrefsService _localPrefs;
		private readonly IGenericDialogService _genericDialogService;

		public List<Func<GameProduct, UniTask>> PrePurchaseHooks = new ();

		public IReadOnlyCollection<GameProductCategory> AvailableProductCategories => _availableProductCategories.Values;
		public IReadOnlyCollection<GameProductsBundle> AvailableGameProductBundles => _availableGameProductBundles.Values;
		public IReadOnlyCollection<GameProduct> AvailableProducts => _availableProducts;
		public bool Initialized { get; private set; }
		public IStoreService RemoteCatalogStore => _playfab;

		private HashSet<string> _pending = new HashSet<string>();

		public IAPService(IGameCommandService commandService, IMessageBrokerService messageBroker,
						  IGameBackendService gameBackendService,
						  IGameDataProvider gameDataProvider, IHomeScreenService homeScreen, LocalPrefsService localPrefs,
						  IGenericDialogService genericDialogService)
		{
			_playfab = new PlayfabStoreService(gameBackendService, commandService, gameDataProvider);
			_commandService = commandService;
			_msgBroker = messageBroker;
			_homeScreen = homeScreen;
			_localPrefs = localPrefs;
			_genericDialogService = genericDialogService;
			_msgBroker.Subscribe<ShopScreenOpenedMessage>(OnShopOpened);
			_msgBroker.Subscribe<LogicInitializedMessage>(OnLogicInitialized);
			_playfab.OnStoreLoaded += OnStoreLoaded;
			_homeScreen.RegisterNotificationQueueProcessor(OnHomeScreenNotification);
		}

		private void OnLogicInitialized(LogicInitializedMessage obj)
		{
			Init();
		}

		private async UniTask<IHomeScreenService.ProcessorResult> OnHomeScreenNotification(Type arg)
		{
			if (arg != typeof(HomeScreenService))
			{
				return IHomeScreenService.ProcessorResult.None;
			}

			var failed = _localPrefs.FailedTransactionMessages.Value;
			if (failed.Count == 0) return IHomeScreenService.ProcessorResult.None;
			// We need the store to get the product names
			await UniTask.WaitUntil(() => Initialized);
			await ShowQueuedTransactionFailedMessagesAndWait();
			return IHomeScreenService.ProcessorResult.None;
		}

		public string BuildMessage(StorePurchaseFailureData data)
		{
			var product = AvailableProducts.FirstOrDefault(product => product.ID == data.ProductId);
			if (product == null) return null;
			var name = product.PlayfabProductConfig.CatalogItem.DisplayName ?? product.ID;
			if (GameLocalization.TryGetTranslation($"UITStore/transaction_failed_${(data.Reason ?? "Unknown")}", out var translation))
			{
				return name + ": " + translation;
			}

			return name + ": " + data.Reason;
		}

		public bool ShouldDisplay(StorePurchaseFailureData data)
		{
			return data.Reason != StorePurchaseFailureReason.LegacyPending && data.Reason != StorePurchaseFailureReason.UserCancelled;
		}

		public async UniTask ShowQueuedTransactionFailedMessagesAndWait()
		{
			var failed = _localPrefs.FailedTransactionMessages.Value;
			if (failed.Count == 0) return;
			await ShowAndWaitFailurePopup(_localPrefs.FailedTransactionMessages.Value.Values);
			_localPrefs.FailedTransactionMessages.Value = new Dictionary<string, StorePurchaseFailureData>();
		}

		public async UniTask ShowAndWaitFailurePopup(IEnumerable<StorePurchaseFailureData> data)
		{
			string allMessages = "";
			foreach (var failed in data)
			{
				if (!ShouldDisplay(failed)) continue;
				var msg = BuildMessage(failed);
				if (msg == null) continue;
				if (!string.IsNullOrEmpty(allMessages))
				{
					allMessages += "\n";
				}

				allMessages += msg;
			}

			_localPrefs.FailedTransactionMessages.Value = new Dictionary<string, StorePurchaseFailureData>();

			if (string.IsNullOrEmpty(allMessages))
			{
				return;
			}

			await _genericDialogService.OpenSimpleMessageAndWait(ScriptLocalization.UITStore.transaction_failed_popup_title, allMessages);
		}

		public bool IsPending(GameProduct product)
		{
			return product != null && _pending.Contains(product.ID);
		}

		private void OnPurchaseFailure(StorePurchaseFailureData data)
		{
			FLog.Warn("OnPurchaseFailure "+data.Reason+" "+data.Message);
			var reason = data.Reason;
			if (reason != StorePurchaseFailureReason.UserCancelled && reason != StorePurchaseFailureReason.LegacyPending)
			{
				SetQueuedFailedMessage(data);
			}

			if (reason == StorePurchaseFailureReason.LegacyPending) // If there is already a pending one lets block user from trying again
			{
				_pending.Add(data.ProductId);
			}
			else
			{
				_pending.Remove(data.ProductId);
			}

			PurchaseFinished?.Invoke(data.ProductId, null, false, data);
		}

		public void SetQueuedFailedMessage(StorePurchaseFailureData data)
		{
			var localPrefsFailed = _localPrefs.FailedTransactionMessages.Value;

			localPrefsFailed[data.ProductId] = data;

			_localPrefs.FailedTransactionMessages.Value = localPrefsFailed;
		}

		private void OnShopOpened(ShopScreenOpenedMessage msg)
		{
			if (_homeScreen.ForceBehaviour == HomeScreenForceBehaviourType.Store)
			{
				_homeScreen.SetForceBehaviour(HomeScreenForceBehaviourType.None);
			}

			TryUpdateStoreCatalog();
		}

		public void Init()
		{
			_playfab.Init();
		}

		private void OnStoreLoaded(List<PlayfabProductConfig> products,
			Dictionary<PlayfabProductConfig, List<PlayfabProductConfig>> bundles)
		{
			ResolveCategoryProducts(products);
			ResolveBundles(bundles);
			UpdateLocalStoreState();
			Initialized = true;
		}

		private void UpdateLocalStoreState()
		{
			var lastStoreState = _localPrefs.LastStoreState;
			var newStoreState = new LocalStoreStateData();

			var isFirstTimeLoadingStore = lastStoreState.Value.Categories.Count == 0;

			foreach (var productCategory in _availableProductCategories.Values.Where(apc => !apc.IsHidden))
			{
				//No previous state persisted inside PlayerPrefs, All items and Categories will be tagged as new.
				if (isFirstTimeLoadingStore)
				{
					newStoreState.Categories.Add(new LocalStoreCategory()
					{
						CategoryName = productCategory.Name,
						Products = productCategory.Products.Select(p => new LocalStoreProduct
							{ProductName = p.PlayfabProductConfig.CatalogItem.ItemId, IsNewStoreItem = false}).ToList()
					});

					continue;
				}

				//Previous State has been found, try to get LastStateCategory
				var lastStoreStateCategory = lastStoreState.Value.Categories.FirstOrDefault(c => c.CategoryName == productCategory.Name);

				if (lastStoreStateCategory == null)
				{
					newStoreState.Categories.Add(new LocalStoreCategory()
					{
						CategoryName = productCategory.Name,
						Products = productCategory.Products.Select(p => new LocalStoreProduct
							{ProductName = p.PlayfabProductConfig.CatalogItem.ItemId, IsNewStoreItem = true}).ToList()
					});
				}
				else
				{
					var addedProducts = productCategory.Products
						.Where(productFromStore => lastStoreStateCategory.Products.All(lastStateProduct =>
							lastStateProduct.ProductName != productFromStore.PlayfabProductConfig.CatalogItem.ItemId))
						.Select(currentProduct => new LocalStoreProduct
						{
							ProductName = currentProduct.PlayfabProductConfig.CatalogItem.ItemId,
							IsNewStoreItem = true
						}).ToList();

					var removedProducts = lastStoreStateCategory.Products
						.Where(oldProduct => productCategory.Products.All(currentProduct =>
							currentProduct.PlayfabProductConfig.CatalogItem.ItemId != oldProduct.ProductName))
						.ToList();

					//Remove what need to be removed, Add what need to be added and keep everything else untouched
					lastStoreStateCategory.Products.RemoveAll(p => removedProducts.Contains(p));
					lastStoreStateCategory.Products.AddRange(addedProducts);

					newStoreState.Categories.Add(lastStoreStateCategory);
				}
			}

			_localPrefs.LastStoreState.Value = newStoreState;
		}

		public void MarkAllNewStoreItemsAsSeen()
		{
			var lastStoreState = _localPrefs.LastStoreState.Value;
			lastStoreState.Categories.ForEach(c => c.MarkProductsAsSeen());

			_localPrefs.LastStoreState.Value = lastStoreState;
		}

		public bool HasStoreItemsUpdate(string filterCategory = null, string filterProduct = null)
		{
			if (filterProduct != null)
			{
				return _localPrefs.LastStoreState.Value.Categories
					.Where(category => category?.Products != null)
					.SelectMany(category => category.Products)
					.Any(product => product?.ProductName == filterProduct && product.IsNewStoreItem);
			}

			if (filterCategory != null)
			{
				return _localPrefs.LastStoreState.Value.Categories
					.FirstOrDefault(c => c.CategoryName == filterCategory)
					?.HasUpdate() ?? false;
			}

			return _localPrefs.LastStoreState.Value.Categories.Any(c => c.HasUpdate());
		}

		private void ResolveBundles(Dictionary<PlayfabProductConfig, List<PlayfabProductConfig>> playfabGameProductBundles)
		{
			_availableGameProductBundles.Clear();

			foreach (var playfabBundle in playfabGameProductBundles)
			{
				if (this.IsRealMoney(new GameProduct { PlayfabProductConfig = playfabBundle.Key })) continue;
				var bundleId = playfabBundle.Key.CatalogItem.ItemId;

				//Resolve Bundle
				if (!_availableGameProductBundles.TryGetValue(bundleId, out var gameProductBundle))
				{
					gameProductBundle = new GameProductsBundle()
					{
						Name = bundleId,
						Bundle = new GameProduct()
						{
							GameItem = ItemFactory.PlayfabCatalog(playfabBundle.Key.CatalogItem),
							PlayfabProductConfig = playfabBundle.Key
						}
					};
					_availableGameProductBundles[bundleId] = gameProductBundle;
				}

				//Resolve Bundle Products
				foreach (var playfabProduct in playfabBundle.Value)
				{
					gameProductBundle.BundleProducts.Add(new GameProduct()
					{
						GameItem = ItemFactory.PlayfabCatalog(playfabProduct.CatalogItem),
						PlayfabProductConfig = playfabProduct
					});
				}
			}
		}

		private void ResolveCategoryProducts(List<PlayfabProductConfig> playfabProducts)
		{
			_availableProducts.Clear();
			foreach (var categoryList in _availableProductCategories.Values)
			{
				categoryList.Products.Clear();
			}

			foreach (var playfabProduct in playfabProducts)
			{
				if (this.IsRealMoney(new GameProduct { PlayfabProductConfig = playfabProduct })) continue;
				var category = string.IsNullOrEmpty(playfabProduct.StoreItemData.Category) ? "General" : playfabProduct.StoreItemData.Category;
				if (!_availableProductCategories.TryGetValue(category, out var categoryList))
				{
					categoryList = new GameProductCategory() {Name = category};
					_availableProductCategories[category] = categoryList;
				}

				var gameProduct = new GameProduct()
				{
					GameItem = ItemFactory.PlayfabCatalog(playfabProduct.CatalogItem),
					PlayfabProductConfig = playfabProduct
				};

				categoryList.Products.Add(gameProduct);
				_availableProducts.Add(gameProduct);
			}
		}

		private void TryUpdateStoreCatalog()
		{
			_playfab.TryLoadStore();
		}

		public void BuyProduct(GameProduct product)
		{
			FLog.Verbose("Buy prodyct called");
			if (!this.IsRealMoney(product))
			{
				LogicPurchaseItem(product);
			}
			else
			{
				OnPurchaseFailure(new StorePurchaseFailureData
				{
					ProductId = product.ID, Reason = StorePurchaseFailureReason.ProductUnavailable,
					Message = "Cash purchases are unavailable in this build."
				});
			}
		}

		public async UniTask ConfirmLogicalPurchase(GameProduct product, ItemData item, (GameId item, uint price) price)
		{
			FLog.Info("IAP", "Purchase of logical item");
			foreach (var hook in PrePurchaseHooks)
			{
				await hook(product);
			}
			var purchased = _commandService.ExecuteCommandWithResult(new BuyFromStoreCommand()
			{
				CatalogItemId = product.PlayfabProductConfig.CatalogItem.ItemId,
				StoreItemData = product.PlayfabProductConfig.StoreItemData
			});
			_pending.Remove(product.ID);
			if (!purchased)
			{
				PurchaseFinished?.Invoke(product.ID, null, false, new StorePurchaseFailureData
				{
					ProductId = product.ID, Reason = StorePurchaseFailureReason.ProductUnavailable,
					Message = "The balance or purchase limit changed before this purchase completed."
				});
				return;
			}
			FLog.Info("IAP", "Purchase finished, calling events");
			PurchaseFinished?.Invoke(product.ID, item, true, null);
		}

		private bool ShouldUseTextConfirmation(ItemData item)
		{
			if (!item.TryGetViewModel(out _))
			{
				return true;
			}

			if (item.HasMetadata<UnlockMetadata>())
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// A logical purchase happens when no real money (FIAT) is involved
		/// </summary>
		private void LogicPurchaseItem(GameProduct product)
		{
			var generatedItem = ItemFactory.PlayfabCatalog(product.PlayfabProductConfig.CatalogItem);
			var price = product.GetPrice();

			var shouldUseText = ShouldUseTextConfirmation(generatedItem);

			FLog.Verbose("Logic Purchase Item - should use text ? "+shouldUseText);

			GenericPurchaseDialogPresenter.IPurchaseData screenData = !shouldUseText
				? new GenericPurchaseDialogPresenter.IconPurchaseData()
				{
					Item = generatedItem,
					Value = price.amt,
					Currency = price.item,
					OnConfirm = () =>
					{
						FLog.Warn("Confirmed purchase");
						ConfirmLogicalPurchase(product, generatedItem, price).Forget();
					},
					OnExit = (shouldSeeShop) =>
					{
						_pending.Remove(product.ID);
						FLog.Warn("Closed purchase popup");
						PurchaseFinished?.Invoke(product.ID, generatedItem, false, new StorePurchaseFailureData()
						{
							Reason = StorePurchaseFailureReason.UserCancelled,
							RequiredtoSeeShop = shouldSeeShop,
							ProductId = product.ID
						});
					}
				}
				: new GenericPurchaseDialogPresenter.TextPurchaseData()
				{
					TextFormat = ScriptLocalization.UITStore.logical_purchase_popup_text,
					Price = ItemFactory.Currency(price.item, (int) price.amt),
					OnConfirm = () => ConfirmLogicalPurchase(product, generatedItem, price).Forget(),
					OnExit = (shouldSeeShop) =>
					{
						FLog.Warn("Closed purchase popup 2");
						PurchaseFinished?.Invoke(product.ID, generatedItem, false, new StorePurchaseFailureData()
						{
							RequiredtoSeeShop = shouldSeeShop,
							Reason = StorePurchaseFailureReason.UserCancelled,
							ProductId = product.ID
						});
					}
				};
			MainInstaller.ResolveServices().GenericDialogService.OpenPurchaseOrNotEnough(screenData);
		}


	}

	public static class IAPHelpers
	{
		private static HashSet<string> _handled = new (); // TODO: remove this

		public static bool IsUIBeingHandled(string itemID)
		{
			return _handled.Contains(itemID);
		}

		public static bool IsRealMoney(this IIAPService iapService, GameProduct product)
		{
			if (product == null) return false;
			var config = product.PlayfabProductConfig;
			var prices = config.StoreItem?.VirtualCurrencyPrices ?? config.CatalogItem.VirtualCurrencyPrices;
			return prices?.ContainsKey("RM") == true || config.StoreItem?.RealCurrencyPrices?.Count > 0
				|| config.CatalogItem.RealCurrencyPrices?.Count > 0;
		}

		public enum BuyProductResult
		{
			ForcePlayerToShop,
			Rewarded,
			Rejected,
			Deferred,
		}

		/// <summary>
		/// Opens and wait for the whole purchase flow to end and returns the result
		/// This handles: Opening the Purchase Popup, Showing the Reward screen if successfull, Showing Transaction Error popup if not
		/// </summary>
		public static async UniTask<BuyProductResult> BuyProductHandleUI(this IIAPService service,
																		 GameProduct product,
																		 FirstLight.Game.UI.UIService uiService,
																		 IRewardService rewardService,
																		 IGenericDialogService genericDialogService
		)
		{
			if (product == null) return BuyProductResult.Rejected;
			if (service.IsRealMoney(product))
			{
				service.BuyProduct(product);
				await service.ShowQueuedTransactionFailedMessagesAndWait();
				return BuyProductResult.Rejected;
			}

			var productId = product.ID;
			if (!_handled.Add(productId)) return BuyProductResult.Deferred;
			var completion = new UniTaskCompletionSource<(ItemData data, bool succeeded, StorePurchaseFailureData failure)>();
			service.PurchaseFinished += OnPurchaseFinished;
			try
			{
				service.BuyProduct(product);
				var result = await completion.Task;
				if (!result.succeeded)
					return result.failure?.RequiredtoSeeShop == true ? BuyProductResult.ForcePlayerToShop : BuyProductResult.Rejected;
				await rewardService.OpenRewardScreen(result.data);
				return BuyProductResult.Rewarded;
			}
			finally
			{
				service.PurchaseFinished -= OnPurchaseFinished;
				_handled.Remove(productId);
			}

			void OnPurchaseFinished(string itemId, ItemData data, bool succeeded, StorePurchaseFailureData failure)
			{
				if (itemId == productId || failure?.ProductId == productId)
					completion.TrySetResult((data, succeeded, failure));
			}
		}
	}
}
