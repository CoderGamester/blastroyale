using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FirstLight.Editor.Build.Utils;
using FirstLight.Game.Utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Entry point for batch mode build calls.
	/// </summary>
	public static class Builder
	{
		/// <summary>
		/// Builds an addressable update - for testing only.
		/// </summary>
		public static void BuildAddressablesUpdateIOS()
		{
			SetupAddressables(true);
			ContentUpdateScript.BuildContentUpdate(AddressableAssetSettingsDefaultObject.Settings, "ServerData/iOS/addressables_content_state.bin");
		}

		/// <summary>
		/// Builds an addressable update - for testing only.
		/// </summary>
		public static void BuildAddressablesUpdateAndroid()
		{
			SetupAddressables(true);
			ContentUpdateScript.BuildContentUpdate(AddressableAssetSettingsDefaultObject.Settings,
				"ServerData/Android/addressables_content_state.bin");
		}

		/// <summary>
		/// Combines the configure and build steps
		/// </summary>
		[UsedImplicitly]
		[MenuItem("FLG/Build/Batch Build")]
		public static void BatchBuild()
		{
			var buildReport = Build(new BuildParameters()
			{
				BuildNumber = BuildUtils.GetBuildNumber(),
				BuildTarget = BuildUtils.GetBuildTarget(),
				DevelopmentBuild = BuildUtils.GetIsDevelopmentBuild(),
				RemoteAddressables = BuildUtils.GetUseRemoteAddressables(),
			});

			if (buildReport.summary.result != BuildResult.Succeeded)
			{
				EditorApplication.Exit(1);
			}
		}

		public class BuildParameters
		{
			public BuildTarget BuildTarget;
			public int BuildNumber;
			public bool DevelopmentBuild;
			public bool RemoteAddressables;
			public BuildOptions AdditionalOptions;
		}

		internal static BuildReport Build(BuildParameters @params)
		{
			var buildConfig = new BuildPlayerOptions();

			SetupBuildNumber(@params.BuildNumber);
			SetupDevelopmentBuild(@params.DevelopmentBuild, ref buildConfig);
			SetupAddressables(@params.RemoteAddressables);
			// Development APKs use Unity's debug keystore; store signing is separate.
			if (@params.BuildTarget == BuildTarget.Android)
			{
				PlayerSettings.Android.useCustomKeystore = false;
				EditorUserBuildSettings.buildAppBundle = false;
				PlayerSettings.Android.splitApplicationBinary = false;
			}
			SetupScenes(ref buildConfig);
			SetupPath(ref buildConfig, @params.BuildTarget);
			BuildAddressables(@params.DevelopmentBuild);

			AssetDatabase.Refresh();

			// Additional build options
			buildConfig.target = @params.BuildTarget;
			buildConfig.options |= BuildOptions.CompressWithLz4HC;
			buildConfig.options |= @params.AdditionalOptions;

			return BuildPipeline.BuildPlayer(buildConfig);
		}

		private static void SetupPath(ref BuildPlayerOptions buildConfig, BuildTarget buildTarget)
		{
			var directory = BuildUtils.GetOutputPath() ?? "Builds/" + buildTarget;
			Directory.CreateDirectory(directory);
			buildConfig.locationPathName = buildTarget switch
			{
				BuildTarget.Android => Path.Combine(directory, "BlastRoyale.apk"),
				BuildTarget.iOS => Path.Combine(directory, "BlastRoyale-Xcode"),
				_ => throw new NotSupportedException("Select Android or iOS before building the mobile client.")
			};
		}

		private static void SetupScenes(ref BuildPlayerOptions buildConfig)
		{
			buildConfig.scenes = (from editorScene in EditorBuildSettings.scenes where editorScene.enabled select editorScene.path).ToArray();
		}

		private static void SetupDevelopmentBuild(bool isDevelopmentBuild, ref BuildPlayerOptions buildOptions)
		{
			if (isDevelopmentBuild)
			{
				buildOptions.options |= BuildOptions.Development;
			}
		}

		private static void SetupAddressables(bool remoteAddressables)
		{
			var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;

			var profileId = addressableSettings.profileSettings.GetProfileId(remoteAddressables ? "CCD" : "Default");

			if (string.IsNullOrEmpty(profileId))
				throw new InvalidOperationException("The selected Addressables profile does not exist.");
			addressableSettings.activeProfileId = profileId;
		}

		internal static void BuildAddressables(bool enableDebugPackages)
		{
			var settings = AddressableAssetSettingsDefaultObject.Settings;
			if (settings == null) throw new InvalidOperationException("Addressables settings are missing.");
			var changed = new Dictionary<BundledAssetGroupSchema, bool>();
			try
			{
				if (!enableDebugPackages)
				{
					foreach (var group in settings.groups)
					{
						if (group == null || group.ReadOnly || !(group.name.EndsWith("_Dev") || group.name.StartsWith("Dev_"))) continue;
						var schema = group.GetSchema<BundledAssetGroupSchema>();
						if (schema == null) continue;
						changed.Add(schema, schema.IncludeInBuild);
						schema.IncludeInBuild = false;
					}
				}
				AddressableAssetSettings.BuildPlayerContent(out var result);
				if (!string.IsNullOrEmpty(result.Error)) throw new InvalidOperationException(result.Error);
			}
			finally
			{
				foreach (var pair in changed) pair.Key.IncludeInBuild = pair.Value;
			}
		}

		private static void SetupBuildNumber(int buildNumber)
		{
			PlayerSettings.Android.bundleVersionCode = buildNumber;
			PlayerSettings.iOS.buildNumber = buildNumber.ToString();
		}

	}
}
