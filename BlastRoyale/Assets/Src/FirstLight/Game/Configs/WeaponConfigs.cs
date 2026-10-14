using GameLovers.GameData;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumWeaponConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "WeaponConfigs", menuName = "ScriptableObjects/Configs/WeaponConfigs")]
	public class WeaponConfigs : QuantumWeaponConfigs, IConfigsContainer<QuantumWeaponConfig>
	{
		// ReSharper disable once ConvertToAutoProperty
		public List<QuantumWeaponConfig> Configs
		{
			get => QuantumConfigs;
			set => QuantumConfigs = value;
		}
	}
}
