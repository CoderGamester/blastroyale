using System;
using FirstLight.Game.Utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.Build.Utils
{
	/// <summary>
	/// Static utility methods for use in build scripts.
	/// </summary>
	public static class BuildUtils
	{
		private const string ENVAR_BUILD_NUMBER = "FL_BUILD_NUMBER";
		private const string ENVAR_ENVIRONMENT = "FL_ENVIRONMENT";
		private const string ENVAR_DEVELOPMENT_BUILD = "FL_DEVELOPMENT_BUILD";
		private const string ENVAR_REMOTE_ADDRESSABLES = "FL_REMOTE_ADDRESSABLES";

		private const string ARG_ENVIRONMENT = "-FLEnvironment";
		private const string ARG_BUILD_NUMBER = "-FLBuildNumber";
		private const string ARG_DEV_BUILD = "-FLDevelopmentBuild";
		private const string ARG_REMOTE_ADDRESSABLES = "-FLRemoteAddressables";
		public static FLEnvironment.Definition OverwriteEnvironment;

		/// <summary>
		/// The version override for the addressable catalog.
		/// </summary>
		[UsedImplicitly]
		public static string AddressableVersionOverride =>
			GetEnvironment() == FLEnvironment.DEVELOPMENT.Name ? DateTime.UtcNow.ToString("yyyy.MM.dd.hh.mm.ss") : PlayerSettings.bundleVersion;

		/// <summary>
		/// Gets the configured client environment, defaults to development.
		/// </summary>
		public static string GetEnvironment()
		{
			if (!string.IsNullOrEmpty(OverwriteEnvironment.Name)) return OverwriteEnvironment.Name;
			return Environment.GetEnvironmentVariable(ENVAR_ENVIRONMENT) ?? GetCMDArgument(ARG_ENVIRONMENT) ?? FLEnvironment.DEVELOPMENT.Name;
		}

		/// <summary>
		/// Gets the current build number. Defaults to 1 for local development.
		/// </summary>
		public static int GetBuildNumber()
		{
			return int.Parse(Environment.GetEnvironmentVariable(ENVAR_BUILD_NUMBER) ?? GetCMDArgument(ARG_BUILD_NUMBER) ?? "1");
		}

		/// <summary>
		/// Gets whether the current build should be a development build.
		/// </summary>
		public static bool GetIsDevelopmentBuild()
		{
			return bool.Parse(Environment.GetEnvironmentVariable(ENVAR_DEVELOPMENT_BUILD) ?? GetCMDArgument(ARG_DEV_BUILD) ?? "true");
		}

		/// <summary>
		/// If we should use remote or bundled addressables.
		/// </summary>
		public static bool GetUseRemoteAddressables()
		{
			return bool.Parse(Environment.GetEnvironmentVariable(ENVAR_REMOTE_ADDRESSABLES) ?? GetCMDArgument(ARG_REMOTE_ADDRESSABLES) ?? "false");
		}

		public static BuildTarget GetBuildTarget() => EditorUserBuildSettings.activeBuildTarget;

		public static string GetOutputPath() => GetCMDArgument("-FLOutputPath");

		private static string GetCMDArgument(string argumentName)
		{
			var args = Environment.GetCommandLineArgs();
			for (var i = 0; i < args.Length; i++)
			{
				if (args[i] == argumentName && args.Length > i + 1)
				{
					return args[i + 1];
				}
			}

			return null;
		}

	}
}
