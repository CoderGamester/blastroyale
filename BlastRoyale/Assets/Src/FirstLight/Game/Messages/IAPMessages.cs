using FirstLight.Game.Data.DataTypes;
using GameLovers.Services;

namespace FirstLight.Game.Messages
{
	public struct PurchaseClaimedMessage : IMessage, FirstLight.SDK.Services.IMessage
	{
		public ItemData ItemPurchased;
		public string SupportingContentCreator;
		public string PricePaid;
		public string PriceCurrencyId;
	}

	public struct BattlePassPurchasedMessage : IMessage
	{
	}


	public struct BattlePassLevelPurchasedMessage : IMessage
	{

	}
}
