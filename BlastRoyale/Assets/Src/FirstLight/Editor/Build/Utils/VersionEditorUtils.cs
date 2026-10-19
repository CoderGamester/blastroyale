using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Editor.Build.Utils;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.Build
{
	/// <summary>
	/// Set the internal version in any VersionService instances in the project before building
	/// and before entering Play Mode.
	/// </summary>
	public static class VersionEditorUtils
	{
		private const int ShortenedCommitLength = 8;
		
		/// <summary>
		/// Set the internal version before building the app.
		/// </summary>
		public static void SetAndSaveInternalVersion(string environment)
		{
			var newVersionData = GenerateInternalVersionSuffix(environment);
			var newVersionDataSerialized = JsonUtility.ToJson(newVersionData);
			var oldVersionDataSerialized = LoadVersionDataSerializedSync();
			if (newVersionDataSerialized.Equals(oldVersionDataSerialized, StringComparison.Ordinal))
			{
				return;
			}

			SaveVersionData(newVersionDataSerialized);
		}

		/// <summary>
		/// Loads the game version saved in disk into string format
		/// </summary>
		public static string LoadVersionDataSerializedSync ()
		{
			var path = Path.Combine(Application.dataPath, "Resources", VersionUtils.VersionDataFilename + ".txt");
			return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
		}

		/// <summary>
		/// Register version stamping before the app enters Play Mode.
		/// </summary>
		[InitializeOnLoadMethod]
		private static void OnEditorLoad()
		{
			if (AssetDatabase.IsAssetImportWorkerProcess()) return;
			EditorApplication.playModeStateChanged += UpdateEditorVersion;
		}

		private static void UpdateEditorVersion(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingEditMode) SetAndSaveInternalVersion("development");
		}

		private static VersionUtils.VersionData GenerateInternalVersionSuffix(string environment)
		{
			var data = new VersionUtils.VersionData();
			
			using (var repo = new GitProcess(Application.dataPath))
			{
				try
				{
					if (!repo.IsValidRepo())
					{
						Debug.LogWarning("Project is not a git repo. Internal version not set.");
					}
					else
					{
						var branch = repo.GetBranch();
						if (string.IsNullOrEmpty(branch))
						{
							Debug.LogWarning("Could not get git branch for internal version");
						}
						else
						{
							data.BranchName = branch;
						}
							
						var commitHash = repo.GetCommitHash();
						if (string.IsNullOrEmpty(commitHash))
						{
							Debug.LogWarning("Could not get git commit for internal version");
						}
						else
						{
							data.Commit = commitHash.Substring(0, ShortenedCommitLength);
						}
					}
				}
				catch (Exception e)
				{
					Debug.LogException(e);
					Debug.LogWarning("Could not execute git commands. Internal version not set.");
				}
			}

			data.BuildNumber = PlayerSettings.iOS.buildNumber;
			data.Environment = environment;

			return data;
		}
		
		/// <summary>
		/// Set the internal version of this application and save it in resources. This should be
		/// called at edit/build time.
		/// </summary>
		private static void SaveVersionData(string serializedData)
		{
			const string assets = "Assets";
			const string resources = "Resources";
			
			var absDirPath = Path.Combine(Application.dataPath, resources);
			if (!Directory.Exists(absDirPath))
			{
				Directory.CreateDirectory(absDirPath);
			}

			// delete old file with incorrect extension
			const string assetExtension = ".asset";
			var absFilePath = Path.Combine(absDirPath, VersionUtils.VersionDataFilename);
			if (File.Exists(Path.ChangeExtension(absFilePath, assetExtension)))
			{
				AssetDatabase.DeleteAsset(
					Path.Combine(assets, resources,
						Path.ChangeExtension(VersionUtils.VersionDataFilename, assetExtension)));
			}
			
			// create new text file
			const string textExtension = ".txt";
			File.WriteAllText(Path.ChangeExtension(absFilePath, textExtension), serializedData);
			
			AssetDatabase.ImportAsset(
				Path.Combine(assets, resources,
					Path.ChangeExtension(VersionUtils.VersionDataFilename, textExtension)), ImportAssetOptions.ForceSynchronousImport);
		}
	}
}
