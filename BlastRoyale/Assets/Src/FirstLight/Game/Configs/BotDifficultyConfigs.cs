using IgnoreServerSerialization = FirstLight.Server.SDK.Modules.GameConfiguration.IgnoreServerSerialization;
using GameLovers.GameData;
using System.Collections.Generic;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumBotDifficultyConfig"/> sheet data
	/// </summary>
	///
	[IgnoreServerSerialization] // This is only used in quantum to setup bots and in the create custom game screen
	[CreateAssetMenu(fileName = "BotDifficultyConfigs", menuName = "ScriptableObjects/Configs/BotDifficultyConfigs")]
	public class BotDifficultyConfigs : QuantumBotDifficultyConfigs, IConfigsContainer<QuantumBotDifficultyConfig>
	{
		// ReSharper disable once ConvertToAutoProperty
		public List<QuantumBotDifficultyConfig> Configs
		{
			get => BotDifficulties;
			set => BotDifficulties = value;
		}
	}
}
