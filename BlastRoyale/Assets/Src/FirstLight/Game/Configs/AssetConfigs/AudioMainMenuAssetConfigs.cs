using FirstLight.Game.Configs;
using System.Collections.Generic;
using GameLovers.Services.AssetsImporter;
using FirstLight.Game.Ids;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all MainMenu audio assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "AudioMainMenuAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/AudioMainMenuAssetConfigs")]
	public class AudioMainMenuAssetConfigs : DirectAssetConfigs<AudioId, AudioClipConfig>
	{
	}
}
