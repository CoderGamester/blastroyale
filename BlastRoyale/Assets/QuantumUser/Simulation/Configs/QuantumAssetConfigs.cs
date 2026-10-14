namespace Quantum
{
	/// <summary>
	/// Quantum asset config container for misc asset refs.
	/// </summary>
	public partial class QuantumAssetConfigs : Quantum.AssetObject
	{
		public AssetRef<EntityPrototype> PlayerCharacterPrototype;
		public AssetRef<EntityPrototype> DefaultBulletPrototype;
		public AssetRef<EntityPrototype> DummyCharacterPrototype;
		public AssetRef<EntityPrototype> WeaponPlatformPrototype;
		public AssetRef<EntityPrototype> ConsumablePlatformPrototype;
		public AssetRef<EntityPrototype> EquipmentPickUpPrototype;
		public AssetRef<EntityPrototype> ChestPrototype;
		public AssetRef<EntityPrototype> AirDropPrototype;
		public AssetRef<EntityPrototype> LandMinePrototype;
		public AssetRef<EntityPrototype> DeathFlagPrototype;
		public AssetRef<NavMeshAgentConfig> BotNavMeshConfig;
	}
}
