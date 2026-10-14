using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using FirstLight.Inspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs.AssetConfigs
{
	/// <summary>
	/// Scriptable object containing generic not identified custom game asset's configurations
	/// </summary>
	[CreateAssetMenu(fileName = "CustomAssetConfigs", menuName = "ScriptableObjects/AssetConfigs/CustomAssetConfigs")]
	[IgnoreServerSerialization]
	public class CustomAssetConfigs : QuantumAssetConfigs
	{
		[InspectorField(Required = true)] public AssetReferenceGameObject ErrorCube;
		[InspectorField(Required = true)] public AssetReferenceSprite ErrorSprite;
		[InspectorField(Required = true)] public AssetReferenceT<AudioClip> ErrorClip;
		[InspectorField(Required = true)] public AssetReferenceT<Material> ErrorMaterial;
		[InspectorField(Required = true)] public AssetRef<Quantum.EntityPrototype> ConsumablePrototype;

		public QuantumAssetConfigs AssetsConfig
		{
			get => this;
			set
			{
				PlayerCharacterPrototype = value.PlayerCharacterPrototype;
				DefaultBulletPrototype = value.DefaultBulletPrototype;
				DummyCharacterPrototype = value.DummyCharacterPrototype;
				WeaponPlatformPrototype = value.WeaponPlatformPrototype;
				ConsumablePlatformPrototype = value.ConsumablePlatformPrototype;
				EquipmentPickUpPrototype = value.EquipmentPickUpPrototype;
				ChestPrototype = value.ChestPrototype;
				AirDropPrototype = value.AirDropPrototype;
				LandMinePrototype = value.LandMinePrototype;
				DeathFlagPrototype = value.DeathFlagPrototype;
				BotNavMeshConfig = value.BotNavMeshConfig;
			}
		}
	}
}
