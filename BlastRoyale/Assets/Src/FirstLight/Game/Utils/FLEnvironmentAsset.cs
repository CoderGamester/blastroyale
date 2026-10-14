using FirstLight.Inspector;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// This stores the current environment used by the game. This asset will have its value set at build time.
	/// </summary>
	public class FLEnvironmentAsset : ScriptableObject
	{
		[InspectorField(ReadOnly = true), UnityEngine.Tooltip("Do not edit this manually. This value is set at build time.")]
		// ReSharper disable once InconsistentNaming
		public string EnvironmentName = "development";
	}
}
