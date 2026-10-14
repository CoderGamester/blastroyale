using UnityEngine;

namespace FirstLight.Game.Utils
{
	public static class DeviceIdentity
	{
		public static string DeviceID() => SystemInfo.deviceUniqueIdentifier;
	}
}
