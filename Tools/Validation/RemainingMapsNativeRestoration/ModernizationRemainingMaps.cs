using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Quantum;
using Quantum.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ModernizationRemainingMaps
{
    private static void RequireIsolated()
    {
        if (!Application.dataPath.EndsWith("/.local/remaining-map-probe/Assets", StringComparison.Ordinal))
            throw new InvalidOperationException("The remaining-map rehearsal requires its unique isolated project.");
    }
    public static void Inventory()
    {
        RequireIsolated();
        var records = new List<object>();
        var payloads = AssetDatabase.FindAssets("t:Map t:NavMesh t:BinaryData").Select(AssetDatabase.GUIDToAssetPath)
            .Where(File.Exists).Distinct().ToDictionary(p => p, Hash);
        var settings = QuantumEditorSettings.Global;
        var old = settings.AutoBuildOnSceneSave;
        settings.AutoBuildOnSceneSave = QuantumMapDataBakeFlags.None;
        try
        {
            foreach (var path in Directory.GetFiles("Assets/AddressableResources/Maps", "*.unity", SearchOption.AllDirectories).OrderBy(p => p))
            {
                try
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var objects = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
                    var missingRoots = objects.Where(go => PrefabUtility.IsOutermostPrefabInstanceRoot(go)
                        && PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.MissingAsset).ToArray();
                    var mapData = objects.SelectMany(go => go.GetComponents<QuantumMapData>()).ToArray();
                    var maps = mapData.Select(data => new { asset = data.GetAsset(true), data }).ToArray();
                    var scripts = new List<object>();
                    foreach (var block in Regex.Split(File.ReadAllText(path), "(?=^--- !u!)", RegexOptions.Multiline))
                    {
                        var header = Regex.Match(block, @"^--- !u!114 &(\d+)", RegexOptions.Multiline);
                        if (!header.Success) continue;
                        var script = Regex.Match(block, @"m_Script: \{fileID: [^,]+, guid: (\w+), type: 3\}").Groups[1].Value;
                        if (script.Length == 0 || AssetDatabase.GUIDToAssetPath(script).Length > 0) continue;
                        var owner = Regex.Match(block, @"m_GameObject: \{fileID: (\d+)\}").Groups[1].Value;
                        scripts.Add(new { componentId = header.Groups[1].Value, ownerId = owner, sourceScriptGuid = script,
                            serializedRecordSha256 = HashText(block), recognizableFieldNames = Regex.Matches(block, @"^  (\w+):", RegexOptions.Multiline).Cast<Match>().Select(m => m.Groups[1].Value).ToArray() });
                    }
                    records.Add(new { status = "native_inventory", scene = path, sourceSceneSha256 = Hash(path), nativeObjects = objects.Length,
                        missingPrefabRoots = missingRoots.Length, nativeMissingScripts = objects.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount),
                        activePrototypeSources = QuantumMapDataBaker.FindLocalObjects<QuantumEntityPrototype>(scene).Count,
                        mapBindings = maps.Select(pair => new { path = pair.asset ? AssetDatabase.GetAssetPath(pair.asset) : null,
                            nativeMapEntities = pair.asset?.MapEntities?.Length ?? 0, sceneMapReferences = pair.data.MapEntityReferences?.Count ?? 0 }).ToArray(),
                        serializedMissingScriptRecords = scripts, missingRootNameSamples = missingRoots.Take(8).Select(go => go.name).ToArray() });
                }
                catch (Exception error)
                {
                    records.Add(new { status = "native_inventory_failed", scene = path, error = error.ToString() });
                }
                File.WriteAllText("remaining-map-native-inventory.json", JsonConvert.SerializeObject(records, Formatting.Indented));
            }
            foreach (var entry in payloads)
                if (Hash(entry.Key) != entry.Value) throw new InvalidOperationException("Read-only inventory changed native map/nav/triangle payload: " + entry.Key);
            File.WriteAllText("remaining-map-native-payload-inventory.json", JsonConvert.SerializeObject(payloads, Formatting.Indented));
            Debug.Log("[RemainingMapInventory] Native inventory completed for " + records.Count + " scenes, with all map/navigation/collision hashes unchanged.");
        }
        finally { settings.AutoBuildOnSceneSave = old; }
    }
    private static string Hash(string path) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
    private static string HashText(string text) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant(); }
}
