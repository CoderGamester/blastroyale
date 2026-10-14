using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using GameLovers.Services;

namespace FirstLight.Game.Messages
{

	public struct TicketsRefundedMessage : IMessage
	{
		public List<ItemData> Refunds;
	}
}
