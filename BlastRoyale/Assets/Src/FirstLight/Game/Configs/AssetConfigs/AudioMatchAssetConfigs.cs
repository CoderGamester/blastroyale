using FirstLight.Game.Configs;
using GameLovers.Services.AssetsImporter;
using FirstLight.Game.Ids;
using UnityEngine;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing all MainMenu audio assets configurations
	/// </summary>
	[CreateAssetMenu(fileName = "AudioMatchAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/AudioMatchAssetConfigs")]
	public class AudioMatchAssetConfigs : DirectAssetConfigs<AudioId, AudioClipConfig>
	{
	}
}
