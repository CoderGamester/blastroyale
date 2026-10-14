using FirstLight.Editor.Build.Utils;
using FirstLight.Game.Utils;
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
			var guids = AssetDatabase.FindAssets($"t:{nameof(DeterministicSessionConfigAsset)}");
			var path = AssetDatabase.GUIDToAssetPath(guids[0]);
			var deterministicConfig = AssetDatabase.LoadAssetAtPath<DeterministicSessionConfigAsset>(path);

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