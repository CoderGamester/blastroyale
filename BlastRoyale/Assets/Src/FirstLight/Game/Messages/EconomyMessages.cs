using GameLovers.Services;
using Quantum;

namespace FirstLight.Game.Messages
{
	/// <summary>
	/// Triggered (MANUALLY!) when a currency change event happens.
	/// </summary>
	public struct CurrencyChangedMessage : IMessage, FirstLight.SDK.Services.IMessage
	{
		public GameId Id;
		public int Change;
		public string Category;
		public ulong NewValue;
	}
}
