using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Quantum;
using UnityEditor;
using UnityEngine;

public static class MainFtuePrototypeImport
{
	public static void Apply()
	{
		var root = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
		var handoff = JObject.Parse(File.ReadAllText(Path.Combine(root, "Docs/Modernization/verification/ftue-native-integration-handoff.json")));
		var export = (string)handoff["NativeSerializerMapExport"];
		var mapPath = (string)handoff["MainMapPath"];
		CheckHash(export, (string)handoff["NativeSerializerMapExportSha256"]);
		CheckHash(mapPath, (string)handoff["MainMapBeforeSha256"]);
		var target = AssetDatabase.LoadAssetAtPath<Map>("Assets/AddressableResources/Maps/FtueDeck/FtueDeck.asset");
		if (!target || target.MapEntities.Length != 0) throw new InvalidOperationException("Expected the original empty FtueDeck prototype cache.");
		var backup = Path.Combine(root, ".local/main-before-ftue-native-prototypes.asset");
		if (File.Exists(backup)) throw new InvalidOperationException("Ftue prototype recovery file already exists.");
		AssetObject[] assets;
		using (var stream = File.OpenRead(export)) assets = new QuantumUnityJsonSerializer().DeserializeAssets(stream);
		try
		{
			var source = assets.OfType<Map>().Single();
			if (source.Guid != target.Guid || source.Path != target.Path || source.MapEntities.Length != (int)handoff["BakedEntities"])
				throw new InvalidDataException("Native Ftue map identity/count differs from the reviewed export.");
			var before = JObject.Parse(JsonUtility.ToJson(target));
			File.Copy(mapPath, backup);
			target.MapEntities = source.MapEntities;
			var after = JObject.Parse(JsonUtility.ToJson(target));
			before.Remove("MapEntities"); after.Remove("MapEntities");
			before.Remove("references"); after.Remove("references");
			if (!JToken.DeepEquals(before, after)) throw new InvalidDataException("A field outside native Ftue MapEntities changed.");
			EditorUtility.SetDirty(target);
			AssetDatabase.SaveAssets();
			Debug.Log("[Main Ftue Prototype Import] Restored17 actual scene prototypes; all other map fields preserved.");
		}
		finally { foreach (var asset in assets) UnityEngine.Object.DestroyImmediate(asset); }
	}

	private static void CheckHash(string path, string expected)
	{
		using var sha = SHA256.Create();
		if (BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant() != expected)
			throw new InvalidDataException("File differs from reviewed native Ftue handoff: " + path);
	}
}
