using System.IO;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

namespace FirstLight.Editor.Validators
{
	/// <summary>
	/// Validates actual player compilation, including unguarded Editor-only references.
	/// This project-owned menu does not require the separate Odin Validator product.
	/// </summary>
	public static class EditorNamespaceValidator
	{
		[MenuItem("FLG/Validation/Compile Player Scripts")]
		public static void ValidatePlayerScripts()
		{
			var target = EditorUserBuildSettings.activeBuildTarget;
			var settings = new ScriptCompilationSettings
			{
				target = target,
				group = BuildPipeline.GetBuildTargetGroup(target)
			};
			var output = Path.Combine("Library", "Modernization", "PlayerValidation", target.ToString());
			Directory.CreateDirectory(output);
			var errors = 0;
			void CountErrors(string condition, string stackTrace, LogType type)
			{
				if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
			}
			Application.logMessageReceived += CountErrors;
			ScriptCompilationResult result;
			try
			{
				result = PlayerBuildInterface.CompilePlayerScripts(settings, output);
			}
			finally
			{
				Application.logMessageReceived -= CountErrors;
			}
			if (errors != 0 || result.assemblies == null || result.assemblies.Count == 0)
				Debug.LogError($"Player script validation failed for {target}; inspect the compiler diagnostics in the Console.");
			else
				Debug.Log($"Player script validation passed for {target}: {result.assemblies.Count} assemblies compiled. Output: {output}");
		}
	}
}
