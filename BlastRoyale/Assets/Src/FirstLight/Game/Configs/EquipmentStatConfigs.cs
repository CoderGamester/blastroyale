using GameLovers.GameData;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumEquipmentStatConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "EquipmentStatConfigs", menuName = "ScriptableObjects/Configs/EquipmentStatConfigs")]
	public class EquipmentStatConfigs : QuantumEquipmentStatConfigs,
	                                     IConfigsContainer<QuantumEquipmentStatConfig>
	{
		public List<QuantumEquipmentStatConfig> Configs
		{
			get => QuantumConfigs;
			set => QuantumConfigs = value;
		}

		/// <summary>
		/// Requests the <see cref="QuantumEquipmentStatConfig"/> of the given <paramref name="equipment"/>
		/// </summary>
		public new virtual QuantumEquipmentStatConfig GetConfig(Quantum.Equipment equipment)
		{
			return base.GetConfig(equipment);
		}
	}
}
