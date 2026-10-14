using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using GameLovers.Services;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;

namespace FirstLight.Game.Services.Social
{
	public interface IPlayfabUnityBridgeService
	{
		UniTask<PlayfabUnityBridgeService.CacheHackData> LoadDataForPlayer(string unityId, string playerName);
	}

	public class PlayfabUnityBridgeService : IPlayfabUnityBridgeService
	{
		public class CacheHackData
		{
			public string AvatarUrl;
			public int Trophies;
			public string PlayerName;
		}

		private IPlayerProfileService _profileService;
		private IMessageBrokerService _messageBroker;

		public PlayfabUnityBridgeService(IPlayerProfileService profileService, IMessageBrokerService messageBroker)
		{
			_profileService = profileService;
			_messageBroker = messageBroker;
			_messageBroker.Subscribe<CollectionItemEquippedMessage>(message =>
			{
				if (message.Category == CollectionCategories.PROFILE_PICTURE)
				{
					_cacheHack.Remove(AuthenticationService.Instance.PlayerId);
				}
			});
		}

		private static readonly BatchQueue<CacheHackData> _batchQueue = new (3);
		private Dictionary<string, CacheHackData> _cacheHack = new ();
		private long _cacheGeneration = -1;

		public async UniTask<CacheHackData> LoadDataForPlayer(string unityId, string playerName)
		{
			var account = UgsAccountSession.Capture();
			if (_cacheGeneration != account.Generation)
			{
				_cacheHack.Clear();
				_cacheGeneration = account.Generation;
			}
			if (_cacheHack.TryGetValue(unityId, out var hackData))
				return hackData;

			try
			{
				var loadedData = await _batchQueue.AddAsync(async () =>
				{
					account.ThrowIfChanged();
					var profile = await _profileService.GetPlayerPublicProfile(unityId);
					account.ThrowIfChanged();
					return new CacheHackData
					{
						AvatarUrl = profile.AvatarUrl,
						Trophies = profile.Statistics.Where(st => st.Name == GameConstants.Stats.RANKED_LEADERBOARD_LADDER_NAME)
							.Select(st => st.Value).FirstOrDefault(),
						PlayerName = string.IsNullOrEmpty(profile.Name) ? playerName : profile.Name
					};
				});
				account.ThrowIfChanged();
				if (loadedData != null)
				{
					_cacheHack[unityId] = loadedData;
					return loadedData;
				}
			}
			catch (Exception e)
			{
				FLog.Warn($"Error setting friend unityid {unityId} avatar", e);
			}

			return null;
		}
	}
}