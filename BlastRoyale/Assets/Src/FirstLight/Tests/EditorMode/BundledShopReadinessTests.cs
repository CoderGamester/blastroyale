#define LOG_LEVEL_VERBOSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.FLogger;
using FirstLight.Game.Logic;
using FirstLight.Game.Domains.HomeScreen;
using FirstLight.Game.Services;
using GameLovers.Services;
using NSubstitute;
using NUnit.Framework;
using PlayFab.ClientModels;
using Assert = NUnit.Framework.Assert;
using UnityEngine;

namespace FirstLight.Tests.EditorMode.Services
{
	public class BundledShopReadinessTests
	{
		private const string StoreStateKey = "ss_LastStoreState";
		private const string FailureKey = "ss_FailedTransactionMessages";
		private readonly Dictionary<string, string> _savedPrefs = new Dictionary<string, string>();

		[SetUp]
		public void SetUp()
		{
			FLog.Init();
			foreach (var key in new[] { StoreStateKey, FailureKey })
			{
				_savedPrefs[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
				PlayerPrefs.DeleteKey(key);
			}
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var entry in _savedPrefs)
			{
				if (entry.Value == null) PlayerPrefs.DeleteKey(entry.Key);
				else PlayerPrefs.SetString(entry.Key, entry.Value);
			}
			PlayerPrefs.Save();
		}

		private static IAPService CreateService(IGameCommandService commands = null, IGenericDialogService dialogs = null)
		{
			var provider = Substitute.For<IGameDataProvider>();
			var storeData = Substitute.For<IPlayerStoreDataProvider>();
			storeData.GetPlayerDailyStoreConfiguration().Returns(new PlayerDailyDealsConfiguration { ResetDealsAt = DateTime.UtcNow.AddDays(1) });
			provider.PlayerStoreDataProvider.Returns(storeData);
			return new IAPService(commands ?? Substitute.For<IGameCommandService>(), new MessageBrokerService(),
				Substitute.For<IGameBackendService>(), provider, Substitute.For<IHomeScreenService>(),
				new LocalPrefsService(), dialogs ?? Substitute.For<IGenericDialogService>());
		}

		[Test]
		public void OldPaymentFailureJsonRemainsReadableWithoutPaymentTypes()
		{
			var oldJson = "{\"legacy-product\":{\"ProductId\":\"legacy-product\",\"Reason\":\"PaymentDeclined\",\"Message\":\"Earlier transaction was rejected\"}}";
			PlayerPrefs.SetString(FailureKey, oldJson);
			var saved = new LocalPrefsService().FailedTransactionMessages.Value;
			Assert.That(saved["legacy-product"].Reason, Is.EqualTo("PaymentDeclined"));
		}

		[Test]
		public async Task CatalogReadinessCompletesWithoutPaymentSdk()
		{
			var service = CreateService();
			Assert.That(service.Initialized, Is.False);
			service.Init();
			await UniTask.WaitUntil(() => service.Initialized).Timeout(TimeSpan.FromSeconds(3)).AsTask();
			var product = service.AvailableProducts.Single();
			Assert.That(product.ID, Is.EqualTo("development-blast-bucks"));
			Assert.That(product.GetPrice().amt, Is.EqualTo(100));
			Assert.That(service.IsRealMoney(product), Is.False);
			Assert.That(service.IsPending(null), Is.False);
		}

		[Test]
		public void CashRequestRejectsWithoutExecutingEarnedCurrencyCommand()
		{
			var commands = Substitute.For<IGameCommandService>();
			var service = CreateService(commands);
			StorePurchaseFailureData failure = null;
			service.PurchaseFinished += (_, item, succeeded, reason) =>
			{
				Assert.That(succeeded, Is.False);
				Assert.That(item, Is.Null);
				failure = reason;
			};
			var product = new GameProduct { PlayfabProductConfig = new PlayfabProductConfig
			{
				CatalogItem = new CatalogItem { ItemId = "unavailable-payment-product" },
				StoreItem = new StoreItem { RealCurrencyPrices = new Dictionary<string, uint> { ["USD"] = 100 } }
			} };
			service.BuyProduct(product);
			Assert.That(failure, Is.Not.Null);
			Assert.That(failure.Reason, Is.EqualTo(StorePurchaseFailureReason.ProductUnavailable));
			Assert.That(commands.ReceivedCalls(), Is.Empty);
		}

		[Test]
		public async Task FailurePopupCompletesWhenRealDialogCloses()
		{
			var dialogs = Substitute.For<IGenericDialogService>();
			dialogs.OpenSimpleMessageAndWait(Arg.Any<string>(), Arg.Any<string>()).Returns(UniTask.CompletedTask);
			var service = CreateService(dialogs: dialogs);
			service.Init();
			await UniTask.WaitUntil(() => service.Initialized).Timeout(TimeSpan.FromSeconds(3)).AsTask();
			await service.ShowAndWaitFailurePopup(new[] { new StorePurchaseFailureData
			{
				ProductId = "development-blast-bucks", Reason = StorePurchaseFailureReason.ProductUnavailable
			} }).Timeout(TimeSpan.FromSeconds(3)).AsTask();
			Assert.That(dialogs.ReceivedCalls().Count(), Is.EqualTo(1));
		}
	}
}
