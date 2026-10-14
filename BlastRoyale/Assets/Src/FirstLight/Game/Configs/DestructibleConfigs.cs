using GameLovers.GameData;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumDestructibleConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "DestructibleConfigs", menuName = "ScriptableObjects/Configs/DestructibleConfigs")]
	public class DestructibleConfigs : QuantumDestructibleConfigs, IConfigsContainer<QuantumDestructibleConfig>
	{
		// ReSharper disable once ConvertToAutoProperty
		public List<QuantumDestructibleConfig> Configs
		{
			get => QuantumConfigs;
			set => QuantumConfigs = value;
		}
	}
}
