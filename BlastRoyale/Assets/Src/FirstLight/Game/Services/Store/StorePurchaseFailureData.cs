namespace FirstLight.Game.Services
{
	/// <summary>Failure of an earned-currency purchase, independent of any payment SDK.</summary>
	public sealed class StorePurchaseFailureData
	{
		public string ProductId;
		public string Reason;
		public string Message;
		public bool RequiredtoSeeShop;
	}

	public static class StorePurchaseFailureReason
	{
		// Failure reasons were serialized as strings; retain old local JSON without a payment SDK.
		public const string LegacyPending = "ExistingPurchasePending";
		public const string ProductUnavailable = "ProductUnavailable";
		public const string UserCancelled = "UserCancelled";
	}
}
