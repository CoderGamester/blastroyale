using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using GameLovers.Services;
using PlayFab.ClientModels;
using Unity.Services.RemoteConfig;

namespace FirstLight.Game.Services.Authentication.Hooks
{
	public sealed class ParseFeatureFlagsHook : IAuthenticationHook
	{
		private readonly IMessageBrokerService _msgBroker;
		public ParseFeatureFlagsHook(IMessageBrokerService msgBroker) => _msgBroker = msgBroker;
		public UniTask BeforeAuthentication(bool previouslyLoggedIn = false) => UniTask.CompletedTask;
		public UniTask AfterAuthentication(LoginResult result, bool previouslyLoggedIn = false) => UniTask.CompletedTask;
		public UniTask BeforeLogout() => UniTask.CompletedTask;

		public UniTask AfterFetchedState(LoginResult result)
		{
			// Remote config initialization has completed before this hook; no parallel title-data/config race.
			var flags = new Dictionary<string, string>();
			if (!FLEnvironment.Configuration.UseBundledRemoteConfig)
			{
				var config = RemoteConfigService.Instance.appConfig;
				var keys = new HashSet<string>(config.GetKeys());
				foreach (var key in new[] { "STORE_ENABLED", "DESYNC_DETECTION", "TUTORIAL", "TUTORIAL_BATTLE", "PAUSE_FREEZE" })
					if (keys.Contains(key)) flags[key] = config.GetBool(key).ToString();
			}
			// The authorized client-only path cannot enable a missing consensus/receipt server through a flag.
			flags["QUANTUM_CUSTOM_SERVER"] = "false";
			flags["WAIT_REWARD_SYNC"] = "false";
			FeatureFlags.ParseFlags(flags);
			FeatureFlags.ParseLocalFeatureFlags();
			_msgBroker.PublishSafe(new FeatureFlagsReceived { TitleData = flags });
			return UniTask.CompletedTask;
		}
	}
}
