using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Quantum;
using UnityEditor;
using UnityEngine;

public static class MainMazePrototypeImport
{
    public static void Apply()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        var handoff = JObject.Parse(File.ReadAllText(Path.Combine(root, "Docs/Modernization/verification/maze-native-integration-handoff.json")));
        var sourcePath = Path.Combine(root, (string)handoff["prototypeExport"]);
        using (var hash = SHA256.Create())
            if (BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(sourcePath))).Replace("-", "").ToLowerInvariant() != (string)handoff["prototypeExportSha256"])
                throw new InvalidDataException("Native prototype export differs from reviewed restart proof");
        var path = "Assets/AddressableResources/Maps/MazeMayhem/MazeMayhem.asset";
        var target = AssetDatabase.LoadAssetAtPath<Map>(path);
        if (!target || target.MapEntities.Length != 0) throw new InvalidOperationException("Expected original empty Maze prototype cache");
        var backup = Path.Combine(root, ".local/main-before-maze-native-prototypes.asset");
        if (File.Exists(backup)) throw new InvalidOperationException("Prototype recovery file already exists");
        AssetObject[] assets;
        using (var stream = File.OpenRead(sourcePath)) assets = new QuantumUnityJsonSerializer().DeserializeAssets(stream);
        try
        {
            var source = assets.OfType<Map>().Single();
            if (source.Guid != target.Guid || source.Path != target.Path || source.MapEntities.Length != 708)
                throw new InvalidDataException("Native map identity/count differs from reviewed export");
            var before = JObject.Parse(JsonUtility.ToJson(target));
            File.Copy(Path.GetFullPath(Path.Combine(Application.dataPath, "..", path)), backup);
            target.MapEntities = source.MapEntities;
            var after = JObject.Parse(JsonUtility.ToJson(target));
            before.Remove("MapEntities"); after.Remove("MapEntities");
            before.Remove("references"); after.Remove("references");
            if (!JToken.DeepEquals(before, after)) throw new InvalidDataException("A field outside native MapEntities changed");
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            Debug.Log("[Main Maze Prototype Import] Restored708 actual scene prototypes; other map fields preserved");
        }
        finally { foreach (var asset in assets) UnityEngine.Object.DestroyImmediate(asset); }
    }
}
