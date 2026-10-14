using GameLovers.GameData;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[CreateAssetMenu(fileName = "BuffConfigs", menuName = "ScriptableObjects/Configs/BuffConfigs")]
	public class BuffConfigs : QuantumBuffConfigs, ISingleConfigContainer<QuantumBuffConfigs>
	{
		public QuantumBuffConfigs Config
		{
			get => this;
			set
			{
				Buffs = value.Buffs;
				Sources = value.Sources;
			}
		}
	}
}
