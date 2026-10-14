using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Quantum;
using Quantum.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ModernizationFtuePrototypeBake
{
    private const string ScenePath = "Assets/ModernizationProbe/FtueDeckScene_FREE_PLACEHOLDER_PROBE.unity";
    private const string MapPath = "Assets/AddressableResources/Maps/FtueDeck/FtueDeck.asset";
    public static void RunAll()
    {
        ModernizationFtueProbe.RunFull();
        Run();
        PersistAndReopen();
    }
    public static void Run()
    {
        if (!Application.dataPath.Contains("/.local/maze-native-probe/")) throw new InvalidOperationException("Isolated full-client project is required.");
        var visualProof = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("ftue-native-full-results.json"));
        if ((string)visualProof["status"] != "native_full_roundtrip_pass") throw new InvalidOperationException("Native visual preservation proof has not passed.");
        var sourceSceneHash = Hash(ScenePath);
        var payloadHashes = AssetDatabase.FindAssets("t:Map t:NavMesh t:BinaryData").Select(AssetDatabase.GUIDToAssetPath).Where(File.Exists).Distinct().ToDictionary(p => p, Hash);
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var mapData = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<QuantumMapData>(true)).Single();
        var map = mapData.GetAsset(true);
        if (AssetDatabase.GetAssetPath(map) != MapPath) throw new InvalidOperationException("Unexpected native map asset binding.");
        var activePrototypes = QuantumMapDataBaker.FindLocalObjects<QuantumEntityPrototype>(scene);
        var allPrototypes = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<QuantumEntityPrototype>(true)).ToArray();
        var inventory = allPrototypes.Select(p => new { name = p.name, active = p.gameObject.activeInHierarchy,
            identity = GlobalObjectId.GetGlobalObjectIdSlow(p).ToString(), componentTypes = p.GetComponents<QuantumUnityComponentPrototype>().Select(c => c.GetType().AssemblyQualifiedName).ToArray() }).ToArray();
        File.WriteAllText("ftue-native-prototype-inventory.json", JsonConvert.SerializeObject(inventory, Formatting.Indented));
        var before = map.MapEntities?.Length ?? 0;
        if (before != 0) throw new InvalidOperationException("Expected original empty MapEntities prerequisite; actual " + before);
        if (activePrototypes.Count == 0) throw new InvalidOperationException("Scene has no real active QuantumEntityPrototype sources.");
        var mapJsonBefore = EditorJsonUtility.ToJson(map);
        var settings = QuantumEditorSettings.Global;
        var oldBake = settings.AutoBuildOnSceneSave;
        settings.AutoBuildOnSceneSave = QuantumMapDataBakeFlags.None;
        try
        {
            // Only actual native component/prototype serialization; collider/navmesh baking is not invoked.
            QuantumMapDataBaker.BakePrototypes(mapData, true);
            if (map.MapEntities == null || map.MapEntities.Length != activePrototypes.Count) throw new InvalidOperationException("Native prototype count mismatch.");
            var beforeFields = JObject.Parse(mapJsonBefore);
            var afterFields = JObject.Parse(EditorJsonUtility.ToJson(map));
            var beforeEntityField = beforeFields.Descendants().OfType<JProperty>().Single(p => p.Name == "MapEntities");
            var afterEntityField = afterFields.Descendants().OfType<JProperty>().Single(p => p.Name == "MapEntities");
            // Unity stores SerializeReference prototype bodies in a separate native managed-reference registry.
            var ids = ((JContainer)afterEntityField.Value).DescendantsAndSelf().OfType<JProperty>().Where(p => p.Name == "rid").Select(p => (long)p.Value).Where(id => id > 0).ToHashSet();
            var beforeRegistry = (JArray)beforeFields["MonoBehaviour"]["references"]["RefIds"];
            var afterRegistry = (JArray)afterFields["MonoBehaviour"]["references"]["RefIds"];
            if (beforeRegistry.Count != 0 || !ids.SetEquals(afterRegistry.Select(entry => (long)entry["rid"])))
                throw new InvalidOperationException("Managed-reference registry is not exclusively the newly baked native MapEntities prototype bodies.");
            ((JObject)beforeFields["MonoBehaviour"]).Remove("references");
            ((JObject)afterFields["MonoBehaviour"]).Remove("references");
            beforeEntityField.Remove(); afterEntityField.Remove();
            if (!JToken.DeepEquals(beforeFields, afterFields))
            {
                File.WriteAllText("ftue-map-other-fields-before.json", beforeFields.ToString(Formatting.Indented));
                File.WriteAllText("ftue-map-other-fields-after.json", afterFields.ToString(Formatting.Indented));
                throw new InvalidOperationException("Prototype-only bake changed other Map fields.");
            }
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssetIfDirty(map);
            using (var stream = File.Create("ftue-native-baked-map.json")) new QuantumUnityJsonSerializer().SerializeAssets(stream, new AssetObject[] { map });
            var typeCounts = map.MapEntities.SelectMany(set => set.Components).GroupBy(component => component.GetType().AssemblyQualifiedName).ToDictionary(g => g.Key, g => g.Count());
            foreach (var pair in payloadHashes) if (pair.Key != MapPath && Hash(pair.Key) != pair.Value) throw new InvalidOperationException("Non-Maze map/navmesh payload changed: " + pair.Key);
            if (Hash(ScenePath) != sourceSceneHash) throw new InvalidOperationException("Visual scene file was saved or changed during prototype-only baking.");
            File.WriteAllText("ftue-native-prototype-bake-results.json", JsonConvert.SerializeObject(new {
                status = "native_prototype_bake_pass", editor = Application.unityVersion, scene = ScenePath, sceneHash = sourceSceneHash, map = MapPath,
                mapGuid = map.Guid.ToString(), mapEntitiesBefore = before, mapEntitiesAfter = map.MapEntities.Length, activeSourcePrototypes = activePrototypes.Count,
                inactiveSourcePrototypes = allPrototypes.Length - activePrototypes.Count, componentTypeCounts = typeCounts, mapEntityViewReferences = mapData.MapEntityReferences.Count,
                colliderBake = "not_invoked", navMeshBake = "not_invoked", lightingRebake = "not_invoked", mapHashBefore = payloadHashes[MapPath], mapHashAfter = Hash(MapPath),
                unchangedOtherNativePayloads = payloadHashes.Count - 1, export = "ftue-native-baked-map.json"
            }, Formatting.Indented));
            File.WriteAllText("ftue-native-map-before-prototype-bake.json", mapJsonBefore);
        }
        finally { settings.AutoBuildOnSceneSave = oldBake; }
    }
    public static void PersistAndReopen()
    {
        if (!Application.dataPath.Contains("/.local/maze-native-probe/")) throw new InvalidOperationException("Isolated full-client project required.");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var data = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<QuantumMapData>(true)).Single();
        var asset = data.GetAsset(true);
        var sourceHash = Hash(ScenePath);
        var refsBefore = data.MapEntityReferences.Select(v => v == null ? "null" : GlobalObjectId.GetGlobalObjectIdSlow(v).ToString()).ToArray();
        var nativePayloadHashes = AssetDatabase.FindAssets("t:Map t:NavMesh t:BinaryData").Select(AssetDatabase.GUIDToAssetPath).Where(File.Exists).Distinct().ToDictionary(p => p, Hash);
        var settings = QuantumEditorSettings.Global;
        var old = settings.AutoBuildOnSceneSave;
        settings.AutoBuildOnSceneSave = QuantumMapDataBakeFlags.None;
        try
        {
            QuantumMapDataBaker.BakePrototypes(data, true);
            if (asset.MapEntities.Length != QuantumMapDataBaker.FindLocalObjects<QuantumEntityPrototype>(scene).Count || data.MapEntityReferences.Count != asset.MapEntities.Length) throw new InvalidOperationException("Native prototype/reference source count mismatch.");
            var refs = data.MapEntityReferences.Select(v => v == null ? "null" : GlobalObjectId.GetGlobalObjectIdSlow(v).ToString()).ToArray();
            EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not persist native generated scene references.");
            var sceneHashAfter = Hash(ScenePath);
            var serializedMap = EditorJsonUtility.ToJson(asset);
            EditorSceneManager.CloseScene(scene, true);
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            data = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<QuantumMapData>(true)).Single();
            var actualRefs = data.MapEntityReferences.Select(v => v == null ? "null" : GlobalObjectId.GetGlobalObjectIdSlow(v).ToString()).ToArray();
            if (!refs.SequenceEqual(actualRefs)) throw new InvalidOperationException("MapEntityReferences changed/lost during save/reopen.");
            if (serializedMap != EditorJsonUtility.ToJson(data.GetAsset(true))) throw new InvalidOperationException("Native Map serialization changed during scene reload.");
            foreach (var pair in nativePayloadHashes) if (pair.Key != MapPath && Hash(pair.Key) != pair.Value) throw new InvalidOperationException("Non-Maze payload changed.");
            File.WriteAllText("ftue-native-map-entity-reference-proof.json", JsonConvert.SerializeObject(new { status = "native_reference_roundtrip_pass", editor = Application.unityVersion, scene = ScenePath,
                sceneHashBefore = sourceHash, sceneHashAfter, mapHash = Hash(MapPath), entityCount = asset.MapEntities.Length, referenceCount = refs.Length, nonNullViewReferences = refs.Count(v => v != "null"),
                nativeReferencesChanged = !refsBefore.SequenceEqual(refs), nativeReferences = refs, colliderBake = "not_invoked", navMeshBake = "not_invoked", unchangedOtherPayloads = nativePayloadHashes.Count - 1 }, Formatting.Indented));
        }
        finally { settings.AutoBuildOnSceneSave = old; }

    }

    public static void VerifyPersistent()
    {
        if (!Application.dataPath.Contains("/.local/maze-native-probe/")) throw new InvalidOperationException("Isolated project required.");
        var referenceProof = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("ftue-native-map-entity-reference-proof.json"));
        if (Hash(ScenePath) != (string)referenceProof["sceneHashAfter"]) throw new InvalidOperationException("Scene artifact hash changed.");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var objects = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
        var data = objects.SelectMany(go => go.GetComponents<QuantumMapData>()).Single();
        var map = data.GetAsset(true);
        if (map.MapEntities.Length != (int)referenceProof["entityCount"] || data.MapEntityReferences.Count != map.MapEntities.Length) throw new InvalidOperationException("Persistent entity/reference count mismatch.");
        if (Hash(MapPath) != (string)referenceProof["mapHash"]) throw new InvalidOperationException("Native map artifact hash changed.");
        var references = data.MapEntityReferences.Select(v => v == null ? "null" : GlobalObjectId.GetGlobalObjectIdSlow(v).ToString()).ToArray();
        if (!references.SequenceEqual(referenceProof["nativeReferences"].Values<string>())) throw new InvalidOperationException("Persistent native view references differ.");
        var missingPrefabs = objects.Count(go => PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.MissingAsset);
        var missingScripts = objects.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
        var boxes = objects.Where(go => go.name == "__Modernization_Visual_Placeholder_Unit_Box__").ToArray();
        if (missingPrefabs != 0 || missingScripts != 0 || boxes.Length != 100 || boxes.Any(go => go.GetComponent<Collider>() != null || go.GetComponent<MeshFilter>().sharedMesh == null || go.GetComponent<MeshRenderer>().sharedMaterial == null)) throw new InvalidOperationException("Persistent native visual/type checks failed.");
        var banners = new List<object>();
        File.WriteAllText("ftue-native-postrestart-proof.json", JsonConvert.SerializeObject(new { status = "native_postrestart_pass", editor = Application.unityVersion, scene = ScenePath,
            sceneHash = Hash(ScenePath), mapHash = Hash(MapPath), mapEntities = map.MapEntities.Length, mapEntityReferences = references.Length, nonNullViewReferences = references.Count(v => v != "null"),
            placeholders = boxes.Length, nativeMissingPrefabs = missingPrefabs, nativeMissingScripts = missingScripts, banners, colliderBake = "not_invoked", navMeshBake = "not_invoked", lightingRebake = "not_invoked" }, Formatting.Indented));
    }

    private static string Hash(string path) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
}
