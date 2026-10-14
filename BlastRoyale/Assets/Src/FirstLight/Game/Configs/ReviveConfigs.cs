using IgnoreServerSerialization = FirstLight.Server.SDK.Modules.GameConfiguration.IgnoreServerSerialization;
using GameLovers.GameData;
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
	[CreateAssetMenu(fileName = "ReviveConfigs", menuName = "ScriptableObjects/Configs/ReviveConfigs")]
	public class ReviveConfigs : QuantumReviveConfigs, ISingleConfigContainer<QuantumReviveConfigs>
	{
		public QuantumReviveConfigs Config
		{
			get => this;
			set
			{
				FullyDisable = value.FullyDisable;
				ReviveAnimationDuration = value.ReviveAnimationDuration;
				PerGameMode = value.PerGameMode;
			}
		}
	}
}
