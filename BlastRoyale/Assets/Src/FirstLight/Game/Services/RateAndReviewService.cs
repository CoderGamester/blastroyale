using FirstLight.FLogger;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Messages;
using GameLovers.MobileServices.NativeUi;
using GameLovers.Services;
using FirstLightServerSDK.Services;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service handles logic for determining showing review prompt
	/// </summary>
	public class RateAndReviewService
	{
		private readonly IMessageBrokerService _messageBrokerService;
		private LocalPrefsService _localPrefsService;
		private readonly IRemoteConfigProvider _remoteConfigProvider;
		private bool _canShowPrompt;

		private int RequiredMatches => _remoteConfigProvider.GetConfig<GeneralConfig>().ReviewPromptGamesPlayedReq;

		public RateAndReviewService()
		{
		}

		public RateAndReviewService(IMessageBrokerService msgBroker, LocalPrefsService localPrefsService, IRemoteConfigProvider remoteConfigProvider)
		{
			_localPrefsService = localPrefsService;
			_remoteConfigProvider = remoteConfigProvider;
			_messageBrokerService = msgBroker;
			msgBroker.Subscribe<SuccessfullyAuthenticated>(OnAuthenticated);
		}

		private void OnAuthenticated(SuccessfullyAuthenticated msg)
		{
			if (msg.PreviouslyLoggedIn) return;
			Init();
		}

		private void Init()
		{
			if (!_remoteConfigProvider.GetConfig<GeneralConfig>().EnableReviewPrompt || _localPrefsService.RateAndReviewPromptShown)
			{
				return;
			}
			_messageBrokerService.Subscribe<MainMenuOpenedMessage>(OnPlayScreenOpenedMessage);
			_messageBrokerService.Subscribe<GameCompletedRewardsMessage>(OnGameCompletedRewardsMessage);
			_messageBrokerService.Subscribe<CollectionItemEquippedMessage>(OnCollectionItemEquippedMessage);
			_messageBrokerService.Subscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUpMessage);
		}

		private void OnBattlePassLevelUpMessage(BattlePassLevelUpMessage message)
		{
			if (_localPrefsService.GamesPlayed.Value < RequiredMatches)
			{
				return;
			}

			_canShowPrompt = true;
		}

		private void OnCollectionItemEquippedMessage(CollectionItemEquippedMessage message)
		{
			if (_localPrefsService.GamesPlayed.Value < RequiredMatches)
			{
				return;
			}

			_canShowPrompt = true;
		}

		private void OpenRateAndReviewPrompt()
		{
			if (!RequestStoreReview()) return;
			_localPrefsService.RateAndReviewPromptShown.Value = true;
			_messageBrokerService.Unsubscribe<GameCompletedRewardsMessage>(this);
			_messageBrokerService.Unsubscribe<CollectionItemEquippedMessage>(this);
			_messageBrokerService.Unsubscribe<BattlePassLevelUpMessage>(this);
			_messageBrokerService.Unsubscribe<MainMenuOpenedMessage>(this);

			FLog.Info($"RateAndReviewService->OpenRateAndReviewPrompt");
		}

		private static bool RequestStoreReview()
		{
#if UNITY_IOS && !UNITY_EDITOR
			return UnityEngine.iOS.Device.RequestStoreReview();
#elif UNITY_ANDROID && !UNITY_EDITOR
			// Uses the app's actual package identifier; a published store listing is
			// required for reviews, but is not a prerequisite for local play.
			Application.OpenURL("market://details?id=" + Application.identifier);
			return true;
#else
			return false;
#endif
		}

		private void OnPlayScreenOpenedMessage(MainMenuOpenedMessage message)
		{
			if (_localPrefsService.GamesPlayed.Value < RequiredMatches)
			{
				_canShowPrompt = false;

				return;
			}

			var shouldShowPrompt = !_localPrefsService.RateAndReviewPromptShown && _canShowPrompt;

			if (!shouldShowPrompt)
			{
				return;
			}

			OpenRateAndReviewPrompt();
		}

		private void OnGameCompletedRewardsMessage(GameCompletedRewardsMessage message)
		{
			_localPrefsService.GamesPlayed.Value += 1;
			FLog.Info(
				$"RateAndReviewService->OnGameCompletedRewardsMessage LocalPrefsService.GamesPlayed.Value {_localPrefsService.GamesPlayed.Value}");
		}
	}
}
