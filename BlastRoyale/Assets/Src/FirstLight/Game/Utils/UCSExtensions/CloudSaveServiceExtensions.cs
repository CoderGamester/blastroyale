using System.Collections.Generic;
using FirstLight.Game.Utils;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;
using SaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;

namespace FirstLight.Game.Utils.UCSExtensions
{
	public class UnityPlayerData
	{
		public string PlayerName;
		public string AvatarURL;
		public int Trophies;
	}

	/// <summary>
	/// Helpers for the UCS CloudSave service.
	/// </summary>
	public static class CloudSaveServiceExtensions
	{
		private const string KEY_PLAYER_NAME = "player_name";
		private const string KEY_AVATAR_URL = "avatar_url";
		private const string KEY_TROPHIES = "trophies";

		/// <summary>Publishes the avatar URL in the current UGS account public profile.</summary>
		public static async UniTask SaveAvatarURLAsync(this ICloudSaveService cloudSave, string avatarUrl)
		{
			var account = UgsAccountSession.Capture();
			await cloudSave.Data.Player.SaveAsync(new Dictionary<string, object>
			{
				{KEY_AVATAR_URL, avatarUrl}
			}, new SaveOptions(new PublicWriteAccessClassOptions()));
			account.ThrowIfChanged();
		}

		public static async UniTask SaveTrophiesAsync(this ICloudSaveService cloudSave, uint trophies)
		{
			var account = UgsAccountSession.Capture();
			await cloudSave.Data.Player.SaveAsync(new Dictionary<string, object> { { KEY_TROPHIES, trophies } },
				new SaveOptions(new PublicWriteAccessClassOptions()));
			account.ThrowIfChanged();
		}

		public static async UniTask SavePlayerNameAsync(this ICloudSaveService cloudSave, string playerName)
		{
			var account = UgsAccountSession.Capture();
			await cloudSave.Data.Player.SaveAsync(new Dictionary<string, object>
			{
				{ KEY_PLAYER_NAME, playerName }
			}, new SaveOptions(new PublicWriteAccessClassOptions()));
			account.ThrowIfChanged();
		}

		/// <summary>
		/// Loads public profile fields for the given Unity Authentication player ID.
		/// </summary>
		public static async UniTask<UnityPlayerData> LoadPlayerDataAsync(this ICloudSaveService cloudSave, string playerID)
		{
			var account = UgsAccountSession.Capture();
			var data = new UnityPlayerData();
			var loadOptions = new LoadOptions(new PublicReadAccessClassOptions(playerID));
			var keys = new HashSet<string>
			{
				KEY_PLAYER_NAME, KEY_AVATAR_URL, KEY_TROPHIES
			};

			var result = await cloudSave.Data.Player.LoadAsync(keys, loadOptions);
			account.ThrowIfChanged();
			if (result.TryGetValue(KEY_PLAYER_NAME, out var playerName))
			{
				data.PlayerName = playerName.Value.GetAs<string>();
			}

			if (result.TryGetValue(KEY_AVATAR_URL, out var avatarUrl))
			{
				data.AvatarURL = avatarUrl.Value.GetAs<string>();
			}

			if (result.TryGetValue(KEY_TROPHIES, out var trophiesString))
			{
				if (int.TryParse(trophiesString.Value.GetAsString(), out var trophies))
				{
					data.Trophies = trophies;
				}

			}

			return data;
		}
	}
}
