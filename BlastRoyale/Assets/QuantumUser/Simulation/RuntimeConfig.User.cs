using Photon.Deterministic;
using System;
using System.Collections.Generic;

namespace Quantum
{
	partial class RuntimeConfig
	{
		public SimulationMatchConfig MatchConfigs = new SimulationMatchConfig();

		public AssetRef<QuantumGameConfigs> GameConfigs;
		public AssetRef<QuantumMapConfigs> MapConfigs;
		public AssetRef<QuantumGameModeConfigs> GameModeConfigs;
		public AssetRef<QuantumBotConfigs> BotConfigs;
		public AssetRef<QuantumBotDifficultyConfigs> BotDifficultyConfigs;
		public AssetRef<QuantumWeaponConfigs> WeaponConfigs;
		public AssetRef<QuantumConsumableConfigs> ConsumableConfigs;
		public AssetRef<QuantumChestConfigs> ChestConfigs;
		public AssetRef<QuantumSpecialConfigs> SpecialConfigs;
		public AssetRef<QuantumAssetConfigs> AssetConfigs;
		public AssetRef<QuantumDestructibleConfigs> DestructibleConfigs;
		public AssetRef<QuantumShrinkingCircleConfigs> ShrinkingCircleConfigs;
		public AssetRef<QuantumEquipmentStatConfigs> EquipmentStatConfigs;
		public AssetRef<QuantumBaseEquipmentStatConfigs> BaseEquipmentStatConfigs;
		public AssetRef<QuantumStatConfigs> StatConfigs;
		public AssetRef<QuantumEquipmentMaterialStatConfigs> EquipmentMaterialStatConfigs;
		public AssetRef<QuantumReviveConfigs> ReviveConfigs;


	}
}
