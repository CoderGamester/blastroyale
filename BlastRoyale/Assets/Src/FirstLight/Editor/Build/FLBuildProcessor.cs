using FirstLight.Editor.Build.Utils;
using FirstLight.Game.Utils;
using FirstLight.Game.Configs;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace FirstLight.Editor.Build
{
	public class FLBuildProcessor : BuildPlayerProcessor
	{
		public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
		{
			var environment = BuildUtils.GetEnvironment();
			var developmentBuild = buildPlayerContext.BuildPlayerOptions.options.HasFlag(BuildOptions.Development);

			VersionEditorUtils.SetAndSaveInternalVersion(environment);
			GenerateEnvironment(environment);
			ConfigureQuantum(developmentBuild);

			// Probably not needed but why not
			AssetDatabase.Refresh();
		}

		private static void ConfigureQuantum(bool developmentBuild)
		{
			var guids = AssetDatabase.FindAssets($"t:{nameof(QuantumRunnerConfigs)}");
			if (guids.Length != 1) throw new BuildFailedException("Exactly one game QuantumRunnerConfigs asset is required.");
			var path = AssetDatabase.GUIDToAssetPath(guids[0]);
			var runnerConfig = AssetDatabase.LoadAssetAtPath<QuantumRunnerConfigs>(path);
			var deterministicConfig = runnerConfig.QuantumDeterministicSessionConfigAsset;
			if (deterministicConfig == null) throw new BuildFailedException("The game runner is missing its deterministic session config.");

			deterministicConfig.Config.ChecksumInterval = developmentBuild ? 60 : 0;

			EditorUtility.SetDirty(deterministicConfig);
			AssetDatabase.SaveAssets();
		}



		private static void GenerateEnvironment(string environment)
		{
			var envAsset = AssetDatabase.LoadAssetAtPath<FLEnvironmentAsset>("Assets/Resources/FLEnvironmentAsset.asset");
			envAsset.EnvironmentName = environment;
			EditorUtility.SetDirty(envAsset);
		}



	}
}
