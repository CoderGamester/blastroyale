using System;
using FirstLight.Game.Configs;
using FirstLight.Server.SDK.Modules;
using Quantum;

namespace FirstLight.Game.Data.DataTypes.Helpers
{
	public static class AvatarHelpers
	{

		public static string GetAvatarUrl(ItemData item, AvatarCollectableConfig config)
		{
			if (item.Id == GameId.AvatarRemote)
			{
				if (item.TryGetMetadata<CollectionMetadata>(out var metadata)
					&& metadata.TryGetTrait(CollectionTraits.URL, out var url))
				{
					return url;
				}
			}

			if(config.GameIdUrlDictionary.TryGetValue(item.Id, out var configUrl)) return configUrl;
			throw new Exception($"Could not find source of URL in neither configs metadata for avatar {ModelSerializer.Serialize(item).Value}");
		}
	}
}
