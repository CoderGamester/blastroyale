using PlayFab;

namespace FirstLight.Game.Utils
{
	public static class DeviceIdentity
	{
		public static string DeviceID() => PlayFabSettings.DeviceUniqueIdentifier;
	}
}
