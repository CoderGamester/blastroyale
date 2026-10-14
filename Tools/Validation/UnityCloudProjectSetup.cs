using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Modernization
{
	/// <summary>Explicit setup of an existing, user-owned project through Unity's native binding API.</summary>
	public static class UnityCloudProjectSetup
	{
		private static string _project;
		private static double _deadline;
		public static void BindExistingProject()
		{
			var config = JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath,
				"Resources/LocalClientConfig.json")));
			var project = (string)config["UnityProjectId"];
			var organization = (string)config["UnityOrganizationId"];
			var name = (string)config["UnityCloudProjectName"];
			if (!Guid.TryParse(project, out _) || string.IsNullOrWhiteSpace(organization) || string.IsNullOrWhiteSpace(name))
				throw new InvalidOperationException("Supply the actual existing project, organization and project name in ignored local configuration.");
			var connect = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Connect.UnityConnect", true);
			var instance = connect.GetProperty("instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
			var bind = connect.GetMethod("BindProject", BindingFlags.Public | BindingFlags.Instance,
				null, new[] { typeof(string), typeof(string), typeof(string) }, null);
			if (instance == null || bind == null) throw new MissingMethodException("Native Unity project binding API is unavailable.");
			bind.Invoke(instance, new object[] { project, name, organization });
			// UnityConnect binding may remain unavailable in a headless signed-out Editor.
			// Configure the actual native player setting consumed by UGS; this does not authenticate the Editor.
			var setProject = typeof(PlayerSettings).GetMethod("SetCloudProjectId", BindingFlags.NonPublic | BindingFlags.Static);
			if (setProject == null) throw new MissingMethodException("Native PlayerSettings project configuration API is unavailable.");
			setProject.Invoke(null, new object[] { project });
			typeof(PlayerSettings).GetMethod("SetDirty", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
			_project = project;
			_deadline = EditorApplication.timeSinceStartup + 30;
			EditorApplication.update += VerifyBinding;
		}

		private static void VerifyBinding()
		{
			var matches = string.Equals(Application.cloudProjectId, _project, StringComparison.OrdinalIgnoreCase);
			if (!matches && EditorApplication.timeSinceStartup < _deadline) return;
			EditorApplication.update -= VerifyBinding;
			AssetDatabase.SaveAssets();
			var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../artifacts/unity-cloud-project-link.json"));
			Directory.CreateDirectory(Path.GetDirectoryName(output));
			File.WriteAllText(output, new JObject { ["NativePlayerProjectMatchesLocalConfiguration"] = matches,
				["EditorDashboardBindingMatchesLocalConfiguration"] = string.Equals(CloudProjectSettings.projectId, _project, StringComparison.OrdinalIgnoreCase) }.ToString());
			if (matches) Debug.Log("[Unity Cloud Setup] Native player project matches ignored local configuration.");
			else Debug.LogError("Native binding did not update this checkout. Link it through Project Settings > Services in the signed-in Editor.");
			EditorApplication.Exit(matches ? 0 : 1);
		}
	}
}
