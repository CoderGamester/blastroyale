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
	[Serializable]
	public struct FlagConfigEntry
	{
		 public GameId GameId;
		[InspectorField(Required = true)] public AssetReferenceT<Mesh> Mesh;
		[InspectorField(Required = true)] public AssetReferenceSprite Sprite;
	}

	[Serializable, IgnoreServerSerialization]
	public struct FlagSkinConfig
	{
		public GameObject FlagPrefab;
		public List<FlagConfigEntry> Skins;
	}

	[CreateAssetMenu(fileName = "FlagSkinConfigs", menuName = "ScriptableObjects/Configs/Collection/FlagSkinContainer"),
	 IgnoreServerSerialization]
	public class FlagSkinConfigs : ScriptableObject, ISingleConfigContainer<FlagSkinConfig>
	{
		[SerializeField] private FlagSkinConfig _config;

		public FlagSkinConfig Config
		{
			get => _config;
			set => _config = value;
		}
	}
}
