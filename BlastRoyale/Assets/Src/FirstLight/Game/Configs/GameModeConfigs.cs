using GameLovers.GameData;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumGameModeConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "GameModeConfigs", menuName = "ScriptableObjects/Configs/GameModeConfigs")]
	public class GameModeConfigs : QuantumGameModeConfigs, IConfigsContainer<QuantumGameModeConfig>
	{
		public List<QuantumGameModeConfig> Configs
		{
			get => QuantumConfigs;
			set => QuantumConfigs = value;
		}
	}
}
