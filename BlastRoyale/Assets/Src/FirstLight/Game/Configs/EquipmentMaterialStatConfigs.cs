using GameLovers.GameData;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumEquipmentMaterialStatConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "EquipmentMaterialStatConfigs", menuName = "ScriptableObjects/Configs/EquipmentMaterialStatConfigs")]
	public class EquipmentMaterialStatConfigs : QuantumEquipmentMaterialStatConfigs,
	                                     IConfigsContainer<QuantumEquipmentMaterialStatConfig>
	{
		public List<QuantumEquipmentMaterialStatConfig> Configs
		{
			get => QuantumConfigs;
			set => QuantumConfigs = value;
		}

		/// <summary>
		/// Requests the <see cref="QuantumEquipmentMaterialStatConfig"/> of the given <paramref name="equipment"/>
		/// </summary>
		public new virtual QuantumEquipmentMaterialStatConfig GetConfig(Quantum.Equipment equipment)
		{
			return base.GetConfig(equipment);
		}
	}
}
