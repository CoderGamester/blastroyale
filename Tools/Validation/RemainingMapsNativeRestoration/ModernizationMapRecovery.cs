using FirstLight.Game.MonoComponent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quantum;
using Quantum.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ModernizationMapRecovery
{
    private static string Original;
    // The native binding reads Unity's serialized file-ID hint; it never generates an ID.
    // GlobalObjectId's scene path regenerates the entire dirty scene for every object,
    // including inside its bulk API. This read-only binding is independently cross-checked
    // against actual GlobalObjectId output and original serialized scene identifiers.
    private static readonly Func<Object, ulong> ReadFileIdHint = (Func<Object, ulong>)typeof(Unsupported)
        .GetMethod("GetFileIDHint", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
        .CreateDelegate(typeof(Func<Object, ulong>));
    private static readonly Dictionary<Object, ulong> NativeIds = new();
    private static int NativeGlobalFallbacks;
    private static void RefreshNativeIds(Scene scene)
    {
        NativeIds.Clear();
        foreach (var obj in scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Transform>(true))
            .SelectMany(t => new Object[] { t.gameObject }.Concat(t.GetComponents<Component>())).Where(o => o != null).Distinct())
            NativeIds[obj] = ReadFileIdHint(obj);
    }
    private static ulong NativeFileId(Object obj) => obj == null ? 0 : NativeIds.TryGetValue(obj, out var id) ? id : ReadFileIdHint(obj);
    private static bool SceneInstance(Object obj) => obj != null && !EditorUtility.IsPersistent(obj) && (obj is GameObject || obj is Component || NativeIds.ContainsKey(obj));
    private static string NativeId(Object obj)
    {
        if (obj == null) return "null";
        if (SceneInstance(obj))
        {
            var id = NativeFileId(obj);
            if (id != 0) return "native-scene-file-id:" + id;
            if (!PrefabUtility.IsPartOfPrefabInstance(obj) || PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.MissingAsset)
                return "native-scene-file-id:0";
        }
        NativeGlobalFallbacks++;
        return GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();
    }
    private static void AssertSerializedSourceIds(SnapshotContext context, string source)
    {
        var text = File.ReadAllText(source);
        var ids = Regex.Matches(text, @"(?:^--- !u!\d+ &|fileID: )(-?\d+)", RegexOptions.Multiline)
            .Cast<Match>().Select(match => match.Groups[1].Value).ToHashSet();
        foreach (var obj in context.Keys.Keys)
        {
            var id = NativeFileId(obj);
            if (id != 0 && !ids.Contains(id.ToString()))
                throw new InvalidOperationException("Native file hint lacks an exact original serialized scene identity: " + id + " type=" + obj.GetType().FullName);
        }
    }
    private const string Folder = "Assets/ModernizationProbe/RecoveredMaps";
    public static void RunAll()
    {
        RequireIsolated();
        var inventory = JArray.Parse(File.ReadAllText("remaining-map-native-inventory.json"));
        var results = new List<object>();
        foreach (var entry in inventory.OfType<JObject>())
        {
            if ((string)entry["status"] != "native_inventory") continue;
            var scenePath = (string)entry["scene"];
            var map = entry["mapBindings"].OfType<JObject>().Single();
            if ((int)map["nativeMapEntities"] > 0 && (int)entry["nativeMissingScripts"] == 0 && (int)entry["missingPrefabRoots"] == 0) continue;
            try
            {
                if (Hash(scenePath) != (string)entry["sourceSceneSha256"]) throw new InvalidOperationException("Original snapshot scene changed before recovery.");
                var result = RepairOne(scenePath, (string)map["path"], (int)entry["missingPrefabRoots"], (int)entry["activePrototypeSources"]);
                results.Add(result);
            }
            catch (Exception error)
            {
                results.Add(new { status = "excluded_native_recovery_failed", scene = scenePath, error = error.ToString() });
            }
            File.WriteAllText("remaining-map-native-recovery-results.json", JsonConvert.SerializeObject(results, Formatting.Indented));
        }
    }
    private static object RepairOne(string source, string mapPath, int expectedMissingRoots, int expectedPrototypes)
    {
        Original = source;
        Directory.CreateDirectory(Folder);
        var label = Path.GetFileNameWithoutExtension(source);
        var outputScene = Folder + "/" + label + "_FREE_PLACEHOLDER_PROBE.unity";
        var assetHashes = AssetHashes();
        var sourceHash = Hash(source);
        var settings = QuantumEditorSettings.Global;
        var oldBake = settings.AutoBuildOnSceneSave;
        settings.AutoBuildOnSceneSave = QuantumMapDataBakeFlags.None;
        try
        {
            NativeGlobalFallbacks = 0;
            Debug.Log("[Map Recovery] starting " + label);
            var scene = EditorSceneManager.OpenScene(source, OpenSceneMode.Single);
            RefreshNativeIds(scene);
            var roots = MissingRoots(scene).ToArray();
            if (roots.Length != expectedMissingRoots) throw new InvalidOperationException("Native missing-root count changed.");
            var lightingBefore = LightingSnapshot();
            var identityless = roots.Where(go => DurableIdentity(go.transform) == null).Select(go => new {
                root = go, oldName = go.name, transformBefore = EditorJsonUtility.ToJson(go.transform),
                originalNativeObjectId = NativeId(go).ToString(), originalNativeTransformId = NativeId(go.transform).ToString(),
                sourceHandleId = NativeId(PrefabUtility.GetPrefabInstanceHandle(go)).ToString(),
                originalSourceSceneSha256 = sourceHash,
                reason = "Stripped source prefab has no durable loaded transform identity; actual Unity save assigns new placeholder IDs. Original geometry/default transform is not claimed recoverable."
            }).ToArray();
            var retired = RetireKnownBakery(scene, label);
            var context = new SnapshotContext(scene.GetRootGameObjects());
            AssertSerializedSourceIds(context, source);
            Debug.Log("[Map Recovery] " + label + " source IDs verified, missing roots=" + roots.Length);
            var newIdentitySources = context.Keys.Where(pair => DurableIdentity(pair.Key) == null).Select(pair => new {
                obj = pair.Key, runtimeSnapshotKey = pair.Value, type = pair.Key.GetType().FullName, name = pair.Key.name,
                sourceObjectIdentity = NativeId(pair.Key).ToString(),
                originalNativeSerializedFields = pair.Key is Component ? EditorJsonUtility.ToJson(pair.Key) : null
            }).ToArray();
            var before = SnapshotScene(scene, context);
            Debug.Log("[Map Recovery] " + label + " original native snapshot complete");
            if (scene.GetRootGameObjects().Sum(MissingScripts) != 0) throw new InvalidOperationException("An unresolved non-Bakery component remains.");
            foreach (var root in roots)
            {
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                if (PrefabUtility.IsPartOfPrefabInstance(root)) throw new InvalidOperationException("Native unpack retained a missing connection.");
                AddVisual(root);
            }
            Debug.Log("[Map Recovery] " + label + " native unpack and unit boxes complete");
            context.AssertObjectsRetained();
            Compare(before, SnapshotScene(scene, context));
            if (!EditorSceneManager.SaveScene(scene, outputScene)) throw new IOException("Native scene save failed.");
            AssetDatabase.ImportAsset(outputScene, ImportAssetOptions.ForceSynchronousImport);
            RefreshNativeIds(scene);
            context.AssertAuthoredIdsRetained();
            Compare(before, SnapshotScene(scene, context));
            if (MissingScriptRecords(outputScene).Count != 0) throw new InvalidOperationException("Native saved scene retains unresolved script records.");
            if (lightingBefore != LightingSnapshot()) throw new InvalidOperationException("Native lighting settings changed.");
            var expected = SnapshotScene(scene, new SnapshotContext(scene.GetRootGameObjects(), requireDurable: true));
            var allGeneratedNativeIdentities = newIdentitySources.Select(entry => new {
                entry.runtimeSnapshotKey, entry.type, entry.name, entry.sourceObjectIdentity, entry.originalNativeSerializedFields,
                actualUnitySavedIdentity = NativeId(entry.obj).ToString(), sourceSceneSha256 = sourceHash,
                reason = "Unity-loaded stripped-prefab placeholder had no durable source object ID; actual native save allocated this ID. Original source record is retained in the recovery copy."
            }).ToArray();
            if (allGeneratedNativeIdentities.Any(entry => entry.actualUnitySavedIdentity == "native-scene-file-id:0" || entry.actualUnitySavedIdentity.EndsWith("-0-0", StringComparison.Ordinal)))
                throw new InvalidOperationException("Native saving failed to assign a durable ID to a retained source placeholder.");
            var generatedIdentityLedger = identityless.Select(entry => new {
                entry.oldName, entry.transformBefore, entry.originalNativeObjectId, entry.originalNativeTransformId,
                entry.sourceHandleId, entry.originalSourceSceneSha256, entry.reason,
                actualUnitySavedObjectId = NativeId(entry.root).ToString(), actualUnitySavedTransformId = NativeId(entry.root.transform).ToString()
            }).ToArray();
            EditorSceneManager.CloseScene(scene, true);
            scene = EditorSceneManager.OpenScene(outputScene, OpenSceneMode.Single);
            RefreshNativeIds(scene);
            Compare(expected, SnapshotScene(scene, new SnapshotContext(scene.GetRootGameObjects(), requireDurable: true)));
            if (scene.GetRootGameObjects().Sum(MissingScripts) != 0 || MissingRoots(scene).Any()) throw new InvalidOperationException("Native reopened scene has missing scripts/prefabs.");
            if (lightingBefore != LightingSnapshot()) throw new InvalidOperationException("Native lighting changed after scene reload.");
            Compare(assetHashes, AssetHashes());
            if (Hash(source) != sourceHash) throw new InvalidOperationException("Source clone scene changed.");
            var visualSceneHash = Hash(outputScene);
            var data = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<QuantumMapData>(true)).Single();
            var map = data.GetAsset(true);
            if (!map || AssetDatabase.GetAssetPath(map) != mapPath) throw new InvalidOperationException("Native map asset binding changed.");
            var active = QuantumMapDataBaker.FindLocalObjects<QuantumEntityPrototype>(scene);
            if (active.Count != expectedPrototypes || active.Count == 0) throw new InvalidOperationException("Native prototype source inventory changed.");
            var allPrototypes = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<QuantumEntityPrototype>(true)).ToArray();
            var inactivePrototypeCount = allPrototypes.Count(p => !p.gameObject.activeInHierarchy);
            var priorRefs = data.MapEntityReferences.Select(v => new { exists = v != null, active = v != null && v.gameObject.activeInHierarchy,
                name = v != null ? v.name : null }).ToArray();
            var entityCountBefore = map.MapEntities?.Length ?? 0;
            if (entityCountBefore != 0) throw new InvalidOperationException("Only genuinely empty native entity caches may be populated.");
            var mapJsonBefore = EditorJsonUtility.ToJson(map);
            QuantumMapDataBaker.BakePrototypes(data, true);
            if (map.MapEntities?.Length != active.Count || data.MapEntityReferences.Count != active.Count) throw new InvalidOperationException("Real SDK prototype/reference count mismatch.");
            var mapJsonAfter = EditorJsonUtility.ToJson(map);
            Directory.CreateDirectory("remaining-map-native-field-evidence");
            File.WriteAllText("remaining-map-native-field-evidence/" + label + "-before.json", mapJsonBefore);
            File.WriteAllText("remaining-map-native-field-evidence/" + label + "-after.json", mapJsonAfter);
            File.WriteAllText("remaining-map-native-field-evidence/" + label + "-native.json", JsonUtility.ToJson(map));
            AssertOnlyNativeEntitiesChanged(mapJsonBefore, mapJsonAfter);
            EditorUtility.SetDirty(map); AssetDatabase.SaveAssetIfDirty(map);
            foreach (var pair in assetHashes)
                if (pair.Key != mapPath && Hash(pair.Key) != pair.Value) throw new InvalidOperationException("Unrelated map/nav/triangle payload changed.");
            if (Hash(outputScene) != visualSceneHash) throw new InvalidOperationException("Prototype-only bake saved scene prematurely.");
            var refs = data.MapEntityReferences.Select(v => v == null ? "null" : NativeId(v).ToString()).ToArray();
            if (!EditorSceneManager.SaveScene(scene, outputScene)) throw new IOException("Native scene references save failed.");
            EditorSceneManager.CloseScene(scene, true);
            scene = EditorSceneManager.OpenScene(outputScene, OpenSceneMode.Single);
            RefreshNativeIds(scene);
            data = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<QuantumMapData>(true)).Single();
            if (!refs.SequenceEqual(data.MapEntityReferences.Select(v => v == null ? "null" : NativeId(v).ToString()))) throw new InvalidOperationException("Native references changed on reload.");
            if (mapJsonAfter != EditorJsonUtility.ToJson(data.GetAsset(true))) throw new InvalidOperationException("Native map fields changed on reload.");
            return new { status = "native_recovery_roundtrip_pass", scene = source, target = outputScene, map = mapPath,
                sourceSceneSha256 = sourceHash, sceneAfterSha256 = Hash(outputScene), mapBeforeSha256 = assetHashes[mapPath], mapAfterSha256 = Hash(mapPath),
                missingRootsBefore = roots.Length, generatedVisualBoxes = roots.Length, authoredObjectsRetained = context.DurableCount, retainedSourceObjectsWithoutOriginalIds = context.TemporaryCount,
                generatedNativeIdentityLedger = generatedIdentityLedger, allGeneratedNativeIdentities, retiredBakeryRecords = retired, nativeMapEntities = map.MapEntities.Length, nativeMapReferences = refs.Length,
                originalReferenceCount = priorRefs.Length, originalNullReferences = priorRefs.Count(r => !r.exists), originalInactiveReferences = priorRefs.Count(r => r.exists && !r.active),
                allAuthoredPrototypeSources = allPrototypes.Length, inactiveAuthoredPrototypeSources = inactivePrototypeCount,
                priorReferenceInventory = priorRefs,
                nativeViewReferences = refs, nonNullViewReferences = refs.Count(r => r != "null"),
                componentTypeCounts = map.MapEntities.SelectMany(set => set.Components).GroupBy(c => c.GetType().FullName).ToDictionary(g => g.Key, g => g.Count()),
                nativeIdentityMethod = "Actual read-only Unity Unsupported.GetFileIDHint; exact original serialized ID membership; actual GlobalObjectId fallback for assets and valid prefab objects without scene hints", nativeGlobalFallbacks = NativeGlobalFallbacks,
                nativeLightingSnapshotHash = HashText(lightingBefore), colliderBake = "not_invoked", navMeshBake = "not_invoked", lightingBake = "not_invoked" };
        }
        finally { settings.AutoBuildOnSceneSave = oldBake; }
    }
    public static void VerifySourceAndOriginalGuid()
    {
        RequireIsolated();
        var results = JArray.Parse(File.ReadAllText("remaining-map-native-recovery-results.json"));
        if (results.Count != 8 || results.Any(row => (string)row["status"] != "native_recovery_roundtrip_pass"))
            throw new InvalidOperationException("All eight strict native recoveries must pass first.");
        var oldBake = QuantumEditorSettings.Global.AutoBuildOnSceneSave;
        QuantumEditorSettings.Global.AutoBuildOnSceneSave = QuantumMapDataBakeFlags.None;
        var proof = new List<object>();
        try
        {
            foreach (var row in results.OfType<JObject>())
            {
                Original = (string)row["scene"]; var target = (string)row["target"]; var mapPath = (string)row["map"];
                var label = Path.GetFileNameWithoutExtension(Original);
                if (Hash(Original) != (string)row["sourceSceneSha256"] || Hash(target) != (string)row["sceneAfterSha256"] || Hash(mapPath) != (string)row["mapAfterSha256"])
                    throw new InvalidOperationException("A guarded native recovery input changed: " + label);
                var originalMetaHash = Hash(Original + ".meta");
                var sourceGuid = AssetDatabase.AssetPathToGUID(Original);
                var source = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
                RefreshNativeIds(source); RetireKnownBakery(source, label);
                var context = new SnapshotContext(source.GetRootGameObjects());
                AssertSerializedSourceIds(context, Original);
                var byKey = context.Keys.ToDictionary(pair => pair.Value, pair => pair.Key);
                var ledger = ((JArray)row["allGeneratedNativeIdentities"]).OfType<JObject>().ToArray();
                if (ledger.Length != context.TemporaryCount) throw new InvalidOperationException("Native source temporary-object count differs from exact identity ledger: " + label);
                foreach (var entry in ledger)
                {
                    var key = (string)entry["runtimeSnapshotKey"];
                    if (!byKey.TryGetValue(key, out var obj) || DurableIdentity(obj) != null || obj.GetType().FullName != (string)entry["type"] || obj.name != (string)entry["name"])
                        throw new InvalidOperationException("Exact source hierarchy/temporary identity join failed: " + label + " " + key);
                    context.Keys[obj] = (string)entry["actualUnitySavedIdentity"];
                }
                var originalFields = SnapshotScene(source, context);
                var originalData = source.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<QuantumMapData>(true)).Single();
                var dataKey = context.Key(originalData);
                var lighting = LightingSnapshot();
                var recovered = EditorSceneManager.OpenScene(target, OpenSceneMode.Single);
                RefreshNativeIds(recovered);
                var recoveredFields = SnapshotScene(recovered, new SnapshotContext(recovered.GetRootGameObjects(), requireDurable: true));
                AssertOnlySceneReferenceCacheChanged(originalFields, recoveredFields, dataKey);
                if (HashText(lighting) != (string)row["nativeLightingSnapshotHash"] || LightingSnapshot() != lighting)
                    throw new InvalidOperationException("Actual original lighting fields changed after the native SDK prototype bake: " + label);
                // The prepared scene must use local references, not its temporary copy GUID.
                var ownCopyGuid = AssetDatabase.AssetPathToGUID(target);
                if (Regex.IsMatch(File.ReadAllText(target), @"guid: " + Regex.Escape(ownCopyGuid)))
                    throw new InvalidOperationException("Native recovered copy contains a self-GUID reference: " + label);
                EditorSceneManager.CloseScene(recovered, true);
                File.Copy(target, Original, true);
                AssetDatabase.ImportAsset(Original, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                if (Hash(Original + ".meta") != originalMetaHash || AssetDatabase.AssetPathToGUID(Original) != sourceGuid)
                    throw new InvalidOperationException("Original native scene metadata changed: " + label);
                var accepted = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
                RefreshNativeIds(accepted);
                Compare(recoveredFields, SnapshotScene(accepted, new SnapshotContext(accepted.GetRootGameObjects(), requireDurable: true)));
                proof.Add(BuildPersistentProof(accepted, row, originalMetaHash, sourceGuid));
                File.WriteAllText("remaining-map-original-guid-proof.json", JsonConvert.SerializeObject(proof, Formatting.Indented));
                Debug.Log("[Map Source/Original GUID Proof] passed " + label);
            }
        }
        finally { QuantumEditorSettings.Global.AutoBuildOnSceneSave = oldBake; }
    }
    private static void AssertOnlySceneReferenceCacheChanged(Dictionary<string,string> before, Dictionary<string,string> after, string dataKey)
    {
        foreach (var pair in before)
        {
            if (!after.TryGetValue(pair.Key, out var value)) throw new InvalidOperationException("Native SDK bake lost authored source object " + pair.Key);
            if (pair.Value == value) continue;
            if (pair.Key != dataKey)
                throw new InvalidOperationException("Native SDK prototype bake altered an authored component field: " + pair.Key + " expected=" + pair.Value + " actual=" + value);
            var first = JObject.Parse(pair.Value.Substring(pair.Value.IndexOf('{')));
            var second = JObject.Parse(value.Substring(value.IndexOf('{')));
            var firstBody = (JObject)first["MonoBehaviour"]; var secondBody = (JObject)second["MonoBehaviour"];
            if (firstBody["MapEntityReferences"] == null || secondBody["MapEntityReferences"] == null)
                throw new InvalidOperationException("Expected exact native scene reference cache field is absent.");
            firstBody.Remove("MapEntityReferences"); secondBody.Remove("MapEntityReferences");
            if (!JToken.DeepEquals(first, second)) throw new InvalidOperationException("Native prototype bake changed another QuantumMapData field.");
        }
    }
    public static void VerifyRestart()
    {
        RequireIsolated();
        var expected = JArray.Parse(File.ReadAllText("remaining-map-original-guid-proof.json"));
        var results = JArray.Parse(File.ReadAllText("remaining-map-native-recovery-results.json"));
        if (expected.Count != 8 || results.Count != 8) throw new InvalidOperationException("Exact eight original-GUID acceptance rows required.");
        var oldBake = QuantumEditorSettings.Global.AutoBuildOnSceneSave;
        QuantumEditorSettings.Global.AutoBuildOnSceneSave = QuantumMapDataBakeFlags.None;
        var proof = new List<object>();
        try
        {
            foreach (var row in results.OfType<JObject>())
            {
                var scene = EditorSceneManager.OpenScene((string)row["scene"], OpenSceneMode.Single);
                RefreshNativeIds(scene);
                var old = expected.OfType<JObject>().Single(entry => (string)entry["ScenePath"] == (string)row["scene"]);
                var actual = JObject.FromObject(BuildPersistentProof(scene, row, (string)old["SceneMetaSha256"], (string)old["OriginalSceneGuid"]));
                if (!JToken.DeepEquals(old, actual)) throw new InvalidOperationException("Native proof changed across process restart: " + row["scene"]);
                proof.Add(actual);
            }
            var baseline = JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText("remaining-map-native-payload-inventory.json"));
            var acceptedMaps = results.Select(row => (string)row["map"]).ToHashSet();
            foreach (var pair in baseline) if (!acceptedMaps.Contains(pair.Key) && Hash(pair.Key) != pair.Value)
                throw new InvalidOperationException("Another map/navmesh/triangle payload changed across native recovery: " + pair.Key);
            File.WriteAllText("remaining-map-postrestart-proof.json", JsonConvert.SerializeObject(proof, Formatting.Indented));
            // Maze and FTUE have already accepted genuine native prototype bakes. Add exact current cache fingerprints only.
            var existing = JArray.Parse(File.ReadAllText("remaining-map-native-inventory.json")).OfType<JObject>()
                .Where(row => (string)row["status"] == "native_inventory" && (int)row["mapBindings"][0]["nativeMapEntities"] > 0);
            var cacheRows = proof.Cast<JObject>().ToList();
            foreach (var row in existing)
            {
                var scene = EditorSceneManager.OpenScene((string)row["scene"], OpenSceneMode.Single);
                RefreshNativeIds(scene);
                var data = scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<QuantumMapData>(true)).Single();
                var map = data.GetAsset(true);
                var count = (int)row["mapBindings"][0]["nativeMapEntities"];
                if ((count != 708 && count != 17) || map.MapEntities.Length != count || data.MapEntityReferences.Count != count || MissingRoots(scene).Any() || scene.GetRootGameObjects().Sum(MissingScripts) != 0)
                    throw new InvalidOperationException("Previously accepted Maze/FTUE native cache contract changed.");
                cacheRows.Add(JObject.FromObject(MapCacheProof(scene, data, map, Hash((string)row["scene"] + ".meta"), AssetDatabase.AssetPathToGUID(scene.path))));
            }
            if (cacheRows.Count != 10) throw new InvalidOperationException("Exact ten map cache ledger rows required.");
            File.WriteAllText("q3-native-map-cache-ledger.json", JsonConvert.SerializeObject(new { Version=1, Entries=cacheRows }, Formatting.Indented));
            Debug.Log("[Map Postrestart Proof] 8 restored maps and 10 exact native map cache fingerprints passed");
        }
        finally { QuantumEditorSettings.Global.AutoBuildOnSceneSave = oldBake; }
    }
    private static object BuildPersistentProof(Scene scene, JObject row, string metaHash, string originalGuid)
    {
        var data = scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<QuantumMapData>(true)).Single();
        var map = data.GetAsset(true); var path = (string)row["map"];
        if (AssetDatabase.GetAssetPath(map) != path || Hash(path) != (string)row["mapAfterSha256"] || Hash(scene.path) != (string)row["sceneAfterSha256"])
            throw new InvalidOperationException("Native accepted scene/map file hash changed.");
        if (map.MapEntities.Length != (int)row["nativeMapEntities"] || data.MapEntityReferences.Count != (int)row["nativeMapReferences"] ||
            QuantumMapDataBaker.FindLocalObjects<QuantumEntityPrototype>(scene).Count != map.MapEntities.Length || MissingRoots(scene).Any() || scene.GetRootGameObjects().Sum(MissingScripts) != 0)
            throw new InvalidOperationException("Accepted native prototype/source/reference/missing-component counts differ.");
        var visuals = scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject)
            .Where(go=>go.name=="__Modernization_Visual_Placeholder_Unit_Box__").ToArray();
        if (visuals.Length != (int)row["generatedVisualBoxes"] || visuals.Any(go=>go.GetComponent<Collider>()!=null || go.GetComponent<MeshFilter>()?.sharedMesh==null || go.GetComponent<MeshRenderer>()?.sharedMaterial!=VisualMaterial()))
            throw new InvalidOperationException("Native visual placeholder geometry/material/noncollision contract changed.");
        if (HashText(LightingSnapshot()) != (string)row["nativeLightingSnapshotHash"] || Hash(scene.path+".meta") != metaHash || AssetDatabase.AssetPathToGUID(scene.path) != originalGuid)
            throw new InvalidOperationException("Native lighting/original scene metadata changed.");
        var native = JObject.Parse(JsonUtility.ToJson(map));
        var before = JObject.Parse(File.ReadAllText("remaining-map-native-field-evidence/" + Path.GetFileNameWithoutExtension(scene.path) + "-before.json"))["MonoBehaviour"];
        foreach (var property in native.Properties())
            if (property.Name != "MapEntities" && property.Name != "references" && !JToken.DeepEquals(property.Value, before[property.Name]))
                throw new InvalidOperationException("A preserved native Map field changed across recovery: " + property.Name);
        var snapshot = SnapshotScene(scene,new SnapshotContext(scene.GetRootGameObjects(),requireDurable:true));
        var cacheProof=JObject.FromObject(MapCacheProof(scene,data,map,metaHash,originalGuid));
        cacheProof["SourceAllAuthoredFieldsPreservedExceptExactSceneReferenceCache"]=true;
        cacheProof["NativeLightingFieldsSha256"]=HashText(LightingSnapshot());
        cacheProof["NativeSceneObjectFieldsSha256"]=HashText(JsonConvert.SerializeObject(snapshot.OrderBy(pair=>pair.Key).ToDictionary(pair=>pair.Key,pair=>pair.Value)));
        cacheProof["VisualUnitBoxes"]=visuals.Length;
        cacheProof["ColliderBakeInvoked"]=false;cacheProof["NavMeshBakeInvoked"]=false;cacheProof["LightingBakeInvoked"]=false;
        return cacheProof;
    }
    private static object MapCacheProof(Scene scene, QuantumMapData data, Map map, string metaHash, string originalGuid)
    {
        var json = JObject.Parse(JsonUtility.ToJson(map));
        var entities = json["MapEntities"];
        var registry = json["references"];
        var referenced = ((JContainer)entities).DescendantsAndSelf().OfType<JProperty>().Where(p=>p.Name=="rid").Select(p=>(long)p.Value).Where(id=>id>0).ToHashSet();
        if (!referenced.SetEquals(((JArray)registry["RefIds"]).Select(row=>(long)row["rid"])))
            throw new InvalidOperationException("Managed references exist outside the exact native MapEntities cache.");
        var other=(JObject)json.DeepClone();other.Remove("MapEntities");other.Remove("references");
        var refs=data.MapEntityReferences.Select(view=>view==null?"null":NativeId(view)).ToArray();
        return new { UnityMapPath=AssetDatabase.GetAssetPath(map),QuantumGuid=map.Guid.Value,UnityMapGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(map)),
            ScenePath=scene.path,SceneMetaSha256=metaHash,OriginalSceneGuid=originalGuid,SceneFileSha256=Hash(scene.path),
            NativeMapFileSha256=Hash(AssetDatabase.GetAssetPath(map)),PrototypeCount=map.MapEntities.Length,SceneReferenceCount=refs.Length,NonNullSceneReferences=refs.Count(x=>x!="null"),
            NativeMapEntitiesSha256=HashText(Canonical(entities).ToString(Formatting.None)),NativeManagedReferencesSha256=HashText(Canonical(registry).ToString(Formatting.None)),
            NativeOtherMapFieldsSha256=HashText(Canonical(other).ToString(Formatting.None)),NativeSceneReferencesSha256=HashText(JsonConvert.SerializeObject(refs)),
            NativePrototypeSourceCount=QuantumMapDataBaker.FindLocalObjects<QuantumEntityPrototype>(scene).Count,
            SourceMapEntitiesEmpty=true,NativeBaker="QuantumMapDataBaker.BakePrototypes(data,true)",
            ComponentTypeCounts=map.MapEntities.SelectMany(set=>set.Components).GroupBy(component=>component.GetType().FullName).ToDictionary(group=>group.Key,group=>group.Count()) };
    }
    private static JToken Canonical(JToken token) => token is JObject obj ? new JObject(obj.Properties().OrderBy(property=>property.Name,StringComparer.Ordinal).Select(property=>new JProperty(property.Name,Canonical(property.Value)))) :
        token is JArray array ? new JArray(array.Select(Canonical)) : token.DeepClone();

    private static void AssertOnlyNativeEntitiesChanged(string beforeJson, string afterJson)
    {
        var before = JObject.Parse(beforeJson); var after = JObject.Parse(afterJson);
        var beforeEntities = before.Descendants().OfType<JProperty>().Single(p => p.Name == "MapEntities");
        var afterEntities = after.Descendants().OfType<JProperty>().Single(p => p.Name == "MapEntities");
        var ids = ((JContainer)afterEntities.Value).DescendantsAndSelf().OfType<JProperty>().Where(p => p.Name == "rid").Select(p => (long)p.Value).Where(id => id > 0).ToHashSet();
        var beforeRegistry = (JArray)before["MonoBehaviour"]["references"]["RefIds"];
        var afterRegistry = (JArray)after["MonoBehaviour"]["references"]["RefIds"];
        if (beforeRegistry.Count != 0 || !ids.SetEquals(afterRegistry.Select(entry => (long)entry["rid"]))) throw new InvalidOperationException("Native reference registry changed outside the new entity bodies.");
        ((JObject)before["MonoBehaviour"]).Remove("references"); ((JObject)after["MonoBehaviour"]).Remove("references");
        beforeEntities.Remove(); afterEntities.Remove();
        if (!JToken.DeepEquals(before, after)) throw new InvalidOperationException("Prototype-only bake changed another native Map field.");
    }
    private static Dictionary<string, string> RetireKnownBakery(Scene scene, string label)
    {
        var known = new Dictionary<string, string> {
            ["306a56f30ff21b5439963fc745cfe9cc"] = "texName:",
            ["c74ce2158ae608549902afb4112fd042"] = "shadowSpread:",
            ["b7fa80e7116296f4eb4f49ec1544ee22"] = "renderSettingsOutPath:",
            ["ec0b4dd729a12d046982652f834580a2"] = "parentName:"
        };
        var records = MissingScriptRecords(Original);
        var owners = new List<ulong>();
        foreach (var pair in records)
        {
            var guid = Regex.Match(pair.Value, @"m_Script: \{fileID: [^,]+, guid: (\w+), type: 3\}").Groups[1].Value;
            if (!known.TryGetValue(guid, out var field) || !pair.Value.Contains(field)) throw new InvalidOperationException("Unrecognized missing component; native recovery excluded before mutation: " + pair.Key);
            var owner = ulong.Parse(Regex.Match(pair.Value, @"m_GameObject: \{fileID: (\d+)\}").Groups[1].Value);
            if (owner > 0) owners.Add(owner);
        }
        if (owners.Count != 3 || owners.Distinct().Count() != 3) throw new InvalidOperationException("Expected exactly three separate loaded Bakery owners.");
        var backup = Path.GetFullPath("remaining-map-savepoints/" + label); Directory.CreateDirectory(backup);
        File.Copy(Original, Path.Combine(backup, "original.unity"), true);
        File.WriteAllText(Path.Combine(backup, "retired-bakery-records.json"), JsonConvert.SerializeObject(records, Formatting.Indented));
        var objects = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
        foreach (var ownerId in owners)
        {
            var owner = objects.Single(go => NativeFileId(go) == ownerId);
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(owner) != 1) throw new InvalidOperationException("Native Bakery owner component count differs.");
            if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(owner) != 1) throw new InvalidOperationException("Native retirement count differs.");
        }
        return records.ToDictionary(pair => pair.Key, pair => HashText(pair.Value));
    }
    private static string LightingSnapshot() => JsonConvert.SerializeObject(new {
        RenderSettings.fog, RenderSettings.fogMode, fogColor = ColorChannels(RenderSettings.fogColor), RenderSettings.fogDensity, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance,
        RenderSettings.ambientMode, ambientSkyColor = ColorChannels(RenderSettings.ambientSkyColor), ambientEquatorColor = ColorChannels(RenderSettings.ambientEquatorColor), ambientGroundColor = ColorChannels(RenderSettings.ambientGroundColor), RenderSettings.ambientIntensity, ambientLight = ColorChannels(RenderSettings.ambientLight),
        subtractiveShadowColor = ColorChannels(RenderSettings.subtractiveShadowColor), skybox = Identity(RenderSettings.skybox), sun = RenderSettings.sun == null ? null : Identity(RenderSettings.sun),
        RenderSettings.defaultReflectionMode, RenderSettings.defaultReflectionResolution, RenderSettings.reflectionBounces, RenderSettings.reflectionIntensity,
        LightmapSettings.lightmapsMode, lightmaps = LightmapSettings.lightmaps.Select(l => new { color = Identity(l.lightmapColor), direction = Identity(l.lightmapDir), mask = Identity(l.shadowMask) }).ToArray()
    });

    private static float[] ColorChannels(Color value) => new[] { value.r, value.g, value.b, value.a };
    private static Dictionary<string, string> SnapshotScene(Scene scene, SnapshotContext context)
    {
        var result = new Dictionary<string, string>();
        foreach (var root in scene.GetRootGameObjects()) foreach (var pair in Snapshot(root, context)) result.Add(pair.Key, pair.Value);
        return result;
    }
    private static Material VisualMaterial()
    {
        const string path = "Assets/Art/ModernizationPlaceholders/Materials/PLACEHOLDER_MissingPrefab_Visual.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) throw new InvalidOperationException("Actual URP Unlit shader is unavailable.");
        material = new Material(shader) { name = "PLACEHOLDER_MissingPrefab_Visual" };
        material.SetColor("_BaseColor", new Color(1f, 0.55f, 0.1f, 1f));
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
    private static void RequireIsolated()
    {
        if (!Application.dataPath.Contains("/.local/remaining-map-probe/")) throw new InvalidOperationException("Probe requires the isolated project.");
    }
    private static int MissingScripts(GameObject root) => root.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
    private static void AddVisual(GameObject root)
    {
        var proxy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        proxy.name = "__Modernization_Visual_Placeholder_Unit_Box__";
        Object.DestroyImmediate(proxy.GetComponent<Collider>());
        proxy.transform.SetParent(root.transform, false);
        proxy.transform.localPosition = Vector3.zero;
        proxy.transform.localRotation = Quaternion.identity;
        proxy.transform.localScale = Vector3.one;
        proxy.GetComponent<MeshRenderer>().sharedMaterial = VisualMaterial();
    }
    private static IEnumerable<GameObject> MissingRoots(Scene scene) => scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Transform>(true)).Select(transform => transform.gameObject).Where(go => PrefabUtility.IsOutermostPrefabInstanceRoot(go) && PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.MissingAsset).OrderBy(go => DurableIdentity(go.transform) ?? "native-temporary:" + go.GetInstanceID());
    private static string RootIdentity(GameObject root) => DurableIdentity(root.transform) ?? throw new InvalidOperationException("Root has no authored transform identity.");
    private static string Identity(Object obj) => obj == null ? "null" : DurableIdentity(obj) ?? "temporary_untracked";
    private static string DurableIdentity(Object obj)
    {
        if (obj == null) return null;
        // Connected valid prefab objects can acquire scene file hints during saving.
        // Their genuine native composite identity remains stable; keep that representation.
        if (SceneInstance(obj) && PrefabUtility.IsPartOfPrefabInstance(obj) && PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.MissingAsset)
        {
            NativeGlobalFallbacks++;
            var global = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            if (global.identifierType == 0 || global.targetObjectId == 0) return null;
            return "native-scene-global:" + global.targetObjectId + ":" + global.targetPrefabId;
        }
        var id = NativeId(obj);
        if (id == "native-scene-file-id:0" || id.EndsWith("-0-0", StringComparison.Ordinal)) return null;
        return id;
    }
    private sealed class SnapshotContext
    {
        public readonly Dictionary<Object, string> Keys = new();
        public readonly Dictionary<int, Object> NativeReferences = new();
        public readonly bool AllowMissingComponents;
        public int EditorOnlyTransientCount { get; private set; }
        private readonly Dictionary<Object, string> DurableIds = new();
        public int DurableCount => DurableIds.Count;
        public int TemporaryCount => Keys.Count - DurableIds.Count;
        public SnapshotContext(GameObject root, bool requireDurable = false) : this(new[] { root }, requireDurable) { }
        public SnapshotContext(IEnumerable<GameObject> roots, bool requireDurable = false, bool allowMissingComponents = false)
        {
            AllowMissingComponents = allowMissingComponents;
            foreach (var obj in roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).SelectMany(t => new Object[] { t.gameObject }.Concat(t.GetComponents<Component>())))
            {
                if (obj == null)
                {
                    if (AllowMissingComponents) continue;
                    throw new InvalidOperationException("Snapshot contains a missing component.");
                }
                if ((obj.hideFlags & HideFlags.DontSaveInEditor) != 0) { EditorOnlyTransientCount++; continue; }
                var durable = DurableIdentity(obj);
                if (requireDurable && durable == null) throw new InvalidOperationException("Native save left an object without a durable identity: " + obj.GetType().FullName + " name=" + obj.name + " flags=" + obj.hideFlags + " ownerFlags=" + (obj is Component c ? c.gameObject.hideFlags.ToString() : "n/a"));
                NativeReferences[obj.GetInstanceID()] = obj;
                if (obj is Component component)
                {
                    var property = new SerializedObject(component).GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                        var referenced = property.objectReferenceValue;
                        if (referenced != null) NativeReferences[referenced.GetInstanceID()] = referenced;
                    }
                }
                Keys.Add(obj, durable ?? "temporary:" + Keys.Count);
                if (durable != null) DurableIds.Add(obj, durable);
            }
        }
        public string Key(Object obj) => obj == null ? "null" : Keys.TryGetValue(obj, out var key) ? key : DurableIdentity(obj) ?? throw new InvalidOperationException("Reference to an untracked temporary object.");
        public void AssertObjectsRetained()
        {
            if (Keys.Keys.Any(obj => obj == null)) throw new InvalidOperationException("Native unpack destroyed a retained object.");
        }
        public void AssertAuthoredIdsRetained()
        {
            AssertObjectsRetained();
            foreach (var pair in DurableIds) if (DurableIdentity(pair.Key) != pair.Value) throw new InvalidOperationException("Authored local ID changed: " + pair.Value);
        }
    }
    private static Dictionary<string, string> Snapshot(GameObject root, SnapshotContext context)
    {
        var result = new Dictionary<string, string>();
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            var go = transform.gameObject;
            if (!context.Keys.ContainsKey(go)) continue;
            result.Add(context.Key(go), JsonConvert.SerializeObject(new { name = go.name, active = go.activeSelf, layer = go.layer, tag = go.tag, flags = GameObjectUtility.GetStaticEditorFlags(go), parent = transform.parent == null ? null : context.Key(transform.parent) }));
            foreach (var component in go.GetComponents<Component>())
            {
                if (component == null)
                {
                    if (context.AllowMissingComponents) continue;
                    throw new InvalidOperationException("Snapshot contains a missing component.");
                }
                if (!context.Keys.ContainsKey(component)) continue;
                var json = JObject.Parse(EditorJsonUtility.ToJson(component));
            if (component is RenderersContainerMonoComponent)
            {
                var native = new SerializedObject(component);
                var renderers = native.FindProperty("_renderers");
                var generated = Enumerable.Range(0, renderers.arraySize).Where(i =>
                {
                    var renderer = renderers.GetArrayElementAtIndex(i).objectReferenceValue as Renderer;
                    if (renderer == null || renderer.gameObject.name != "__Modernization_Visual_Placeholder_Unit_Box__") return false;
                    if (renderer is not MeshRenderer || renderer.GetComponent<Collider>() != null || renderer.GetComponent<MeshFilter>().sharedMesh == null || renderer.sharedMaterial != VisualMaterial())
                        throw new InvalidOperationException("Derived renderer cache contains an invalid generated visual object.");
                    return true;
                }).OrderByDescending(i => i).ToArray();
                var cache = (JObject)json["MonoBehaviour"];
                foreach (var field in new[] { "_renderers", "_originalMaterials", "_rendererColors", "_rendererAdditiveColors" })
                {
                    var values = (JArray)cache[field];
                    if (values.Count != renderers.arraySize) throw new InvalidOperationException("Renderer cache array lengths are inconsistent.");
                    // The exception is precisely the cache rows of the newly authored native unit-box visuals.
                    foreach (var index in generated) values.RemoveAt(index);
                }
                if (generated.Length > 0) File.AppendAllText("remaining-map-derived-cache-deltas.jsonl", JsonConvert.SerializeObject(new { identity = context.Key(component), generatedVisualRows = generated.Reverse().ToArray(), reason = "native_RenderersContainer_UpdateRenderers_includes_generated_unit_boxes" }) + "\n");
            }

                json.Remove("m_CorrespondingSourceObject"); json.Remove("m_PrefabInstance"); json.Remove("m_PrefabAsset");
                Normalize(json, context);
                result.Add(context.Key(component), component.GetType().AssemblyQualifiedName + ":" + json.ToString(Formatting.None));
            }
        }
        return result;
    }
    private static void Normalize(JToken token, SnapshotContext context)
    {
        foreach (var child in token.Children().ToArray()) Normalize(child, context);
        if (token is JObject obj && obj.Count == 1 && obj["instanceID"] != null)
        {
            var instance = (int)obj["instanceID"];
            if (instance != 0 && !context.NativeReferences.TryGetValue(instance, out _)) throw new InvalidOperationException("Snapshot has an unknown native object reference: " + instance);
            var reference = instance == 0 ? null : context.NativeReferences[instance];
            obj.RemoveAll(); obj.Add("stableObject", context.Key(reference));
        }
    }
    private static void Compare(Dictionary<string, string> expected, Dictionary<string, string> actual)
    {
        foreach (var pair in expected)
            if (!actual.TryGetValue(pair.Key, out var value) || value != pair.Value) throw new InvalidOperationException("Snapshot changed/lost: " + pair.Key + " expected=" + pair.Value + " actual=" + value);
    }
    private static Dictionary<string, string> MissingScriptRecords(string path)
    {
        var result = new Dictionary<string, string>();
        foreach (var block in Regex.Split(File.ReadAllText(path), "(?=^--- !u!)", RegexOptions.Multiline))
        {
            var header = Regex.Match(block, @"^--- !u!114 &(\d+)", RegexOptions.Multiline);
            if (!header.Success) continue;
            var script = Regex.Match(block, @"m_Script: \{fileID: [^,]+, guid: (\w+), type: 3\}").Groups[1].Value;
            if (script.Length != 0 && AssetDatabase.GUIDToAssetPath(script).Length == 0) result.Add(header.Groups[1].Value, block.Trim());
        }
        return result;
    }
    private static string HashText(string value) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant(); }
    private static Dictionary<string, string> AssetHashes() => AssetDatabase.FindAssets("t:Map t:NavMesh t:BinaryData").Select(AssetDatabase.GUIDToAssetPath).Where(File.Exists).Distinct().ToDictionary(path => path, Hash);
    private static string Hash(string path) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
}
