using GameLovers.GameData;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumGameConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "GameConfigs", menuName = "ScriptableObjects/Configs/GameConfigs")]
	public class GameConfigs : QuantumGameConfigs, ISingleConfigContainer<QuantumGameConfig>
	{
		public QuantumGameConfig Config
		{
			get => QuantumConfig;
			set => QuantumConfig = value;
		}
	}
}
