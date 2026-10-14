using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Modernization
{
	/// <summary>Native MonoScript cache audit; source and metadata remain unchanged.</summary>
	public static class MonoScriptLayoutAudit
	{
		private const string Merged = "Assets/Photon/Quantum/Runtime/QuantumUnityRuntime.cs";
		private static JObject Manifest => JObject.Parse(File.ReadAllText(Path.GetFullPath(
			Path.Combine(Application.dataPath, "../../Tools/Modernization/QuantumSdkLayout/manifest.json"))));

		public static void ForceSourceImports()
		{
			var manifest = Manifest;
			foreach (var target in (JArray)manifest["Targets"])
				AssetDatabase.ImportAsset((string)target["RelativePath"], ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			AssetDatabase.ImportAsset(Merged, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			Audit();
		}

		/// <summary>Run this in a separate restarted Editor process after ForceSourceImports.</summary>
		public static void Audit()
		{
			var manifest = Manifest;
			var scripts = new HashSet<MonoScript>(MonoImporter.GetAllRuntimeMonoScripts());
			var rows = new List<object>();
			foreach (var target in (JArray)manifest["Targets"])
			{
				var path = (string)target["RelativePath"];
				var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
				var actualClass = script == null ? null : script.GetClass();
				var expected = "Quantum." + (string)target["Name"];
				var expectedGuid = (string)target["Guid"];
				var guid = AssetDatabase.AssetPathToGUID(path);
				rows.Add(new { Path = path, ExpectedClass = expected, ActualClass = actualClass?.FullName,
					ExpectedGuid = expectedGuid, ActualGuid = guid, InRuntimeMonoScriptInventory = script != null && scripts.Contains(script) });
				if (actualClass?.FullName != expected || guid != expectedGuid)
					throw new InvalidOperationException("Native MonoScript class/GUID mismatch for " + path + ": " + actualClass?.FullName);
			}
			var merged = AssetDatabase.LoadAssetAtPath<MonoScript>(Merged);
			rows.Add(new { Path = Merged, ActualClass = merged?.GetClass()?.FullName });
			var root = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
			var pathOut = Path.Combine(root, "artifacts/quantum-native-monoscript-layout-audit.json");
			Directory.CreateDirectory(Path.GetDirectoryName(pathOut));
			File.WriteAllText(pathOut, JsonConvert.SerializeObject(rows, Formatting.Indented));
			Debug.Log("[MonoScript Layout Audit] All eleven actual canonical SDK types and GUIDs resolve; merged class=" + merged?.GetClass()?.FullName);
		}

		public static void AuditRemainingCandidates()
		{
			var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "Quantum.Unity");
			var source = File.ReadAllText(Merged);
			var scripts = MonoImporter.GetAllRuntimeMonoScripts();
			var rows = new List<object>();
			foreach (var name in new[] { "QuantumGameGizmosSettings", "QuantumMenuSceneInfo", "QuantumNavMesh",
				"QuantumAssetSourceAddressable`1", "QuantumAssetSourceResource`1", "QuantumAssetSourceStatic`1",
				"QuantumAssetSourceStaticLazy`1", "QuantumGlobalScriptableObject`1" })
			{
				var type = assembly.GetType("Quantum." + name, throwOnError: true);
				var simpleName = name.Split('`')[0];
				var matched = scripts.Where(s => s.GetClass() == type).Select(AssetDatabase.GetAssetPath).ToArray();
				rows.Add(new { Type = type.FullName, BaseType = type.BaseType?.FullName,
					IsUnityObject = typeof(UnityEngine.Object).IsAssignableFrom(type), type.ContainsGenericParameters,
					GenericConstraints = type.IsGenericTypeDefinition ? type.GetGenericArguments().SelectMany(p => p.GetGenericParameterConstraints()).Select(t => t.FullName).ToArray() : Array.Empty<string>(),
					MergedDeclarations = Regex.Matches(source, @"partial\s+class\s+" + Regex.Escape(simpleName) + @"\b").Count,
					NativeMonoScriptPaths = matched });
			}
			var root = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
			var output = Path.Combine(root, "artifacts/quantum-native-remaining-partial-candidates.json");
			Directory.CreateDirectory(Path.GetDirectoryName(output));
			File.WriteAllText(output, JsonConvert.SerializeObject(rows, Formatting.Indented));
			Debug.Log("[MonoScript Layout Audit] Recorded eight actual remaining candidate types, their base/constraint metadata and native MonoScript paths.");
		}
	}
}
