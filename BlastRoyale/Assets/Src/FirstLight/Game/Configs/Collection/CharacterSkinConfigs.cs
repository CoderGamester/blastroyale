using FirstLight.Inspector;
using IgnoreServerSerialization = FirstLight.Server.SDK.Modules.GameConfiguration.IgnoreServerSerialization;
using GameLovers.GameData;
using System;
using System.Collections.Generic;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs
{
	[Serializable, IgnoreServerSerialization]
	public struct CharacterSkinsConfig
	{
		public List<CosmeticConfigEntry> Skins;
	}

	[Serializable]
	public struct CosmeticConfigEntry
	{
		 public GameId GameId;
		[InspectorField(Required = true)] public AssetReferenceGameObject Prefab;
		[InspectorField(Required = true)] public AssetReferenceSprite Sprite;
	}

	[CreateAssetMenu(fileName = "CharacterSkinConfigs", menuName = "ScriptableObjects/Configs/Collection/CharacterSkinContainer"),
	 IgnoreServerSerialization]
	public class CharacterSkinConfigs : ScriptableObject, ISingleConfigContainer<CharacterSkinsConfig>
	{
		[SerializeField] private CharacterSkinsConfig _config;

		public CharacterSkinsConfig Config
		{
			get => _config;
			set => _config = value;
		}
	}
}
