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

public static class ModernizationFtueProbe
{
    private const string Original = "Assets/AddressableResources/Maps/FtueDeck/FtueDeckScene.unity";
    private const string Folder = "Assets/ModernizationProbe";
    private static readonly List<object> Cases = new();

    public static void Diagnose()
    {
        if (!Application.dataPath.Contains("/.local/maze-native-probe/")) throw new InvalidOperationException("Probe requires the isolated project.");
        var scene = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
        var roots = MissingRoots(scene).Take(8).ToArray();
        var records = roots.Select(root => new { name = root.name, objects = root.GetComponentsInChildren<Transform>(true).SelectMany(t => new Object[] { t.gameObject }.Concat(t.GetComponents<Component>())).Take(40).Select(obj => {
            var found = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long localId);
            var global = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            return new { name = obj.name, type = obj.GetType().FullName, found, guid, localId, global = global.ToString(), json = obj is Component ? EditorJsonUtility.ToJson(obj) : null };
        }).ToArray() }).ToArray();
        File.WriteAllText("ftue-native-identity-diagnostics.json", JsonConvert.SerializeObject(records, Formatting.Indented));
    }

    public static void Run()
    {
        RequireIsolated();
        Directory.CreateDirectory(Folder);
        Cases.Clear();
        var sourceHash = Hash(Original);
        var assetHashes = AssetHashes();
        var settings = QuantumEditorSettings.Global;
        var oldBake = settings.AutoBuildOnSceneSave;
        settings.AutoBuildOnSceneSave = QuantumMapDataBakeFlags.None;
        try
        {
            var scene = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
            var candidates = MissingRoots(scene).GroupBy(go => go.name.Split(new[] { "guid: " }, StringSplitOptions.None).Last()).Select(group => RootIdentity(group.First())).Take(5).ToArray();
            var rootCount = MissingRoots(scene).Count();
            foreach (var identity in candidates)
            {
                scene = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
                var root = MissingRoots(scene).First(go => RootIdentity(go) == identity);
                var originalName = root.name;
                try
                {
                    var context = new SnapshotContext(root);
                    var before = Snapshot(root, context);
                    var missingScripts = MissingScripts(root);
                    if (missingScripts > 0) throw new InvalidOperationException("Excluded: missing MonoScripts: " + missingScripts);
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    if (PrefabUtility.IsPartOfPrefabInstance(root)) throw new InvalidOperationException("Native unpack retained a prefab connection.");
                    context.AssertObjectsRetained();
                    Compare(before, Snapshot(root, context));
                    AddVisual(root);
                    var path = Folder + "/MazeProbe_" + Cases.Count + ".unity";
                    if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Could not save probe scene.");
                    context.AssertAuthoredIdsRetained();
                    Compare(before, Snapshot(root, context));
                    var savedRootIdentity = RootIdentity(root);
                    var expected = Snapshot(root, new SnapshotContext(root, requireDurable: true));
                    EditorSceneManager.CloseScene(scene, true);
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var reopened = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Transform>(true)).Select(transform => transform.gameObject).FirstOrDefault(go => DurableIdentity(go.transform) == savedRootIdentity);
                    if (reopened == null) throw new InvalidOperationException("Authored root identity did not survive save/reopen.");
                    Compare(expected, Snapshot(reopened, new SnapshotContext(reopened, requireDurable: true)));
                    if (MissingRoots(scene).Count() != rootCount - 1) throw new InvalidOperationException("Unexpected missing-root count after replacement.");
                    Compare(assetHashes, AssetHashes());
                    if (Hash(Original) != sourceHash) throw new InvalidOperationException("Original clone scene changed.");
                    Cases.Add(new { identity, originalName, status = "native_unpack_roundtrip_pass", scene = path, authoredObjects = context.DurableCount, retainedTemporaryObjects = context.TemporaryCount, geometry = "unit_box_visual_approximation" });
                }
                catch (Exception exception) { Cases.Add(new { identity, originalName, status = "excluded_native_roundtrip_failed", error = exception.ToString() }); }
            }
            Compare(assetHashes, AssetHashes());
            if (Hash(Original) != sourceHash) throw new InvalidOperationException("Original clone scene changed.");
            File.WriteAllText("ftue-native-probe-results.json", JsonConvert.SerializeObject(new { status = "probe_completed", editor = Application.unityVersion, missingRoots = rootCount, attempted = Cases.Count, sourceHash, nativeCollisionAssetHashes = assetHashes, cases = Cases }, Formatting.Indented));
        }
        finally { settings.AutoBuildOnSceneSave = oldBake; }
    }

    public static void RunBanners()
    {
        RequireIsolated();
        Directory.CreateDirectory(Folder);
        var results = new List<object>();
        foreach (var name in new[] { "Superstar", "Unicorn", "Demon", "Tombstone" })
        {
            var source = "Assets/AddressableResources/MainMenuAssets/Banners/" + name + ".prefab";
            var originalHash = Hash(source);
            var copy = Folder + "/" + name + "_VisualPlaceholderProbe.prefab";
            GameObject root = null;
            try
            {
                if (AssetDatabase.LoadMainAssetAtPath(copy) != null) AssetDatabase.DeleteAsset(copy);
                if (!AssetDatabase.CopyAsset(source, copy)) throw new IOException("Native asset copy failed.");
                AssetDatabase.ImportAsset(copy, ImportAssetOptions.ForceSynchronousImport);
                root = PrefabUtility.LoadPrefabContents(copy);
                var missing = root.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).Where(go => PrefabUtility.IsAnyPrefabInstanceRoot(go) && PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.MissingAsset).ToArray();
                if (missing.Length != 1) throw new InvalidOperationException("Expected one native missing geometry root; found " + missing.Length);
                if (MissingScripts(root) != 0) throw new InvalidOperationException("Prefab has missing scripts.");
                var context = new SnapshotContext(root);
                var before = Snapshot(root, context);
                var missingName = missing[0].name;
                PrefabUtility.UnpackPrefabInstance(missing[0], PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                context.AssertObjectsRetained();
                Compare(before, Snapshot(root, context));
                var meshReplacements = RestoreExplicitBannerParticleMesh(source, root, context, before);
                AddVisual(missing[0]);
                PrefabUtility.SaveAsPrefabAsset(root, copy, out var success);
                if (!success) throw new IOException("Native prefab save failed.");
                context.AssertAuthoredIdsRetained();
                Compare(before, Snapshot(root, context));
                var expected = Snapshot(root, new SnapshotContext(root, requireDurable: true));
                PrefabUtility.UnloadPrefabContents(root); root = null;
                AssetDatabase.ImportAsset(copy, ImportAssetOptions.ForceSynchronousImport);
                root = PrefabUtility.LoadPrefabContents(copy);
                Compare(expected, Snapshot(root, new SnapshotContext(root, requireDurable: true)));
                if (root.GetComponentsInChildren<Transform>(true).Any(t => PrefabUtility.GetPrefabInstanceStatus(t.gameObject) == PrefabInstanceStatus.MissingAsset)) throw new InvalidOperationException("Missing geometry instance remains.");
                if (Hash(source) != originalHash) throw new InvalidOperationException("Original clone prefab changed.");
                results.Add(new { name, source, copy, status = "native_unpack_roundtrip_pass", missingName, authoredObjects = context.DurableCount, retainedTemporaryObjects = context.TemporaryCount, explicitParticleMeshReplacements = meshReplacements });
            }
            catch (Exception exception) { results.Add(new { name, source, status = "excluded_native_roundtrip_failed", error = exception.ToString() }); }
            finally { if (root != null) PrefabUtility.UnloadPrefabContents(root); }
        }
        File.WriteAllText("banner-native-probe-results.json", JsonConvert.SerializeObject(new { editor = Application.unityVersion, results }, Formatting.Indented));
    }

    public static void DiagnoseMissingSceneEntries()
    {
        RequireIsolated();
        var scene = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
        var raw = File.ReadAllText(Original);
        var blocks = Regex.Split(raw, "(?=^--- !u!)", RegexOptions.Multiline);
        var result = new List<object>();
        foreach (var root in scene.GetRootGameObjects()) foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            var go = transform.gameObject;
            var components = go.GetComponents<Component>();
            if (components.All(c => c != null)) continue;
            var id = GlobalObjectId.GetGlobalObjectIdSlow(go);
            var records = blocks.Where(b => Regex.IsMatch(b, @"m_GameObject: \{fileID: " + id.targetObjectId + @"\}")).Select(b => new { header = b.Split('\n')[0], script = Regex.Match(b, @"m_Script: \{fileID: [^,]+, guid: (\w+), type: 3\}").Groups[1].Value }).Select(r => new { r.header, r.script, scriptPath = AssetDatabase.GUIDToAssetPath(r.script) }).ToArray();
            result.Add(new { name = go.name, identity = id.ToString(), root = root.name, parent = transform.parent?.name, missing = components.Count(c => c == null), missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go), records });
        }
        File.WriteAllText("ftue-missing-scene-components.json", JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    public static void RunFull()
    {
        RequireIsolated();
        Directory.CreateDirectory(Folder);
        var assetHashes = AssetHashes();
        var sourceHash = Hash(Original);
        var settings = QuantumEditorSettings.Global;
        var oldBake = settings.AutoBuildOnSceneSave;
        settings.AutoBuildOnSceneSave = QuantumMapDataBakeFlags.None;
        var resultPath = "ftue-native-full-results.json";
        try
        {
            var scene = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
            var roots = MissingRoots(scene).ToArray();
            var lightingBefore = LightingSnapshot();
            var retiredBakeryRecordHashes = RetireBakery(scene);
            var context = new SnapshotContext(scene.GetRootGameObjects());
            var before = SnapshotScene(scene, context);
            if (scene.GetRootGameObjects().Sum(MissingScripts) != 0) throw new InvalidOperationException("Unexpected missing components remain after the three approved Bakery retirements.");
            var restored = new List<object>();
            foreach (var root in roots)
            {
                var identity = RootIdentity(root);
                var originalName = root.name;
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                if (PrefabUtility.IsPartOfPrefabInstance(root)) throw new InvalidOperationException("Unpack retained connection: " + identity);
                AddVisual(root);
                restored.Add(new { identity, originalName, geometry = "unit_box_visual_approximation", generatedPhysicsColliders = 0 });
            }
            context.AssertObjectsRetained();
            Compare(before, SnapshotScene(scene, context));
            var target = Folder + "/FtueDeckScene_FREE_PLACEHOLDER_PROBE.unity";
            if (!EditorSceneManager.SaveScene(scene, target)) throw new IOException("Native scene save failed.");
            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
            context.AssertAuthoredIdsRetained();
            Compare(before, SnapshotScene(scene, context));
            if (MissingScriptRecords(target).Count != 0) throw new InvalidOperationException("Saved scene still contains missing scripts.");
            if (lightingBefore != LightingSnapshot()) throw new InvalidOperationException("Native lighting settings changed.");
            var expected = SnapshotScene(scene, new SnapshotContext(scene.GetRootGameObjects(), requireDurable: true));
            EditorSceneManager.CloseScene(scene, true);
            scene = EditorSceneManager.OpenScene(target, OpenSceneMode.Single);
            Compare(expected, SnapshotScene(scene, new SnapshotContext(scene.GetRootGameObjects(), requireDurable: true)));
            if (scene.GetRootGameObjects().Sum(MissingScripts) != 0 || MissingScriptRecords(target).Count != 0) throw new InvalidOperationException("Reopened scene contains missing scripts.");
            if (lightingBefore != LightingSnapshot()) throw new InvalidOperationException("Native lighting settings changed on reopen.");
            var remaining = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Transform>(true)).Count(t => PrefabUtility.GetPrefabInstanceStatus(t.gameObject) == PrefabInstanceStatus.MissingAsset);
            if (remaining != 0) throw new InvalidOperationException("Native missing prefab objects remain: " + remaining);
            Compare(assetHashes, AssetHashes());
            if (Hash(Original) != sourceHash) throw new InvalidOperationException("Source clone scene changed.");
            File.WriteAllText(resultPath, JsonConvert.SerializeObject(new { status = "native_full_roundtrip_pass", editor = Application.unityVersion, source = Original, target, sourceHash, targetHash = Hash(target), missingRootsBefore = roots.Length, missingRootsAfter = remaining, authoredObjects = context.DurableCount, retainedTemporaryObjects = context.TemporaryCount, editorOnlyTransientComponentsExcluded = context.EditorOnlyTransientCount, retiredBakeryRecordHashes, nativeLightingSnapshotHash = HashText(lightingBefore), lightingRebake = "not_run", nativeCollisionAssetHashes = assetHashes, restored }, Formatting.Indented));
        }
        catch (Exception exception)
        {
            File.WriteAllText(resultPath, JsonConvert.SerializeObject(new { status = "excluded_native_full_roundtrip_failed", editor = Application.unityVersion, error = exception.ToString(), sourceHash }, Formatting.Indented));
            throw;
        }
        finally { settings.AutoBuildOnSceneSave = oldBake; }

    }
    private static Dictionary<string, string> RetireBakery(Scene scene)
    {
        var expected = new[] {
            new { owner = 1541802023UL, name = "Skylight", component = "1541802024", guid = "306a56f30ff21b5439963fc745cfe9cc", fingerprint = "texName: sky.dds" },
            new { owner = 1589549742UL, name = "Directional Light (1)", component = "1589549743", guid = "c74ce2158ae608549902afb4112fd042", fingerprint = "shadowSpread: 0.01" },
            new { owner = 1630253436UL, name = "!ftraceLightmaps", component = "1630253437", guid = "b7fa80e7116296f4eb4f49ec1544ee22", fingerprint = "renderSettingsOutPath: AddressableResources/Maps/FtueDeck/Lightmaps" }
        };
        var records = MissingScriptRecords(Original);
        var approvedOrphans = new HashSet<string> { "275781418", "734784427", "821118485" };
        if (records.Count != 6 || expected.Any(e => !records.TryGetValue(e.component, out var record) || !record.Contains(e.guid) || !record.Contains(e.fingerprint)) || records.Where(p => !expected.Any(e => e.component == p.Key)).Any(p => !approvedOrphans.Contains(p.Key) || !p.Value.Contains("ec0b4dd729a12d046982652f834580a2") || !p.Value.Contains("m_GameObject: {fileID: 0}"))) throw new InvalidOperationException("Bakery record identity/fingerprint precheck failed; unresolved raw component records=" + JsonConvert.SerializeObject(records.ToDictionary(p => p.Key, p => p.Value.Substring(0, Math.Min(400, p.Value.Length)))));
        var folder = Path.GetFullPath("../savepoints/ftue-bakery-retirement");
        Directory.CreateDirectory(folder);
        File.Copy(Original, Path.Combine(folder, "FtueDeckScene_before.unity"), true);
        File.WriteAllText(Path.Combine(folder, "retired-records.json"), JsonConvert.SerializeObject(records, Formatting.Indented));
        var hashes = records.ToDictionary(pair => pair.Key, pair => HashText(pair.Value));
        File.WriteAllText(Path.Combine(folder, "retired-record-hashes.json"), JsonConvert.SerializeObject(hashes, Formatting.Indented));
        var all = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
        foreach (var entry in expected)
        {
            var owner = all.Single(go => GlobalObjectId.GetGlobalObjectIdSlow(go).targetObjectId == entry.owner);
            if (owner.name != entry.name || GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(owner) != 1) throw new InvalidOperationException("Bakery owner/count precheck failed: " + entry.name);
            if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(owner) != 1) throw new InvalidOperationException("Native removal count mismatch: " + entry.name);
        }
        return hashes;
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
    private static int RestoreExplicitBannerParticleMesh(string source, GameObject root, SnapshotContext context, Dictionary<string, string> before)
    {
        var ids = new HashSet<ulong>();
        foreach (var block in Regex.Split(File.ReadAllText(source), "(?=^--- !u!)", RegexOptions.Multiline))
        {
            var header = Regex.Match(block, @"^--- !u!199 &(\d+)", RegexOptions.Multiline);
            if (!header.Success) continue;
            if (Regex.IsMatch(block, @"m_Mesh: \{fileID: [^,]+, guid: (53bfc932f076bc843baecf10d6092e04|8b74da159f5d3c24493810a2751ab3bb|d6a3e9a4d47251b43b9677f79a166f03|c7965aedffd3e6f4fbb13757cc4ad65b), type: 3\}")) ids.Add(ulong.Parse(header.Groups[1].Value));
        }
        if (ids.Count == 0) return 0;
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/ModernizationPlaceholders/Meshes/PLACEHOLDER_6d6cd06c2176aab46a6da9dd4e1f83e9.asset");
        if (mesh == null) throw new InvalidOperationException("Verified real native placeholder mesh is unavailable.");
        var changed = 0;
        foreach (var renderer in root.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            if (!ids.Contains(GlobalObjectId.GetGlobalObjectIdSlow(renderer).targetObjectId)) continue;
            var key = context.Key(renderer);
            var oldValue = JObject.Parse(before[key].Substring(before[key].IndexOf(':') + 1));
            renderer.mesh = mesh;
            var updated = Snapshot(root, context)[key];
            var newValue = JObject.Parse(updated.Substring(updated.IndexOf(':') + 1));
            ((JObject) oldValue["ParticleSystemRenderer"]).Remove("m_Mesh");
            ((JObject) newValue["ParticleSystemRenderer"]).Remove("m_Mesh");
            if (!JToken.DeepEquals(oldValue, newValue)) throw new InvalidOperationException("Particle visual fallback changed fields: " + JsonConvert.SerializeObject(oldValue.Properties().Where(p => !JToken.DeepEquals(p.Value, newValue[p.Name])).Select(p => new { name = p.Name, before = p.Value, after = newValue[p.Name] }).ToArray()));
            before[key] = updated;
            changed++;
        }
        if (changed != ids.Count) throw new InvalidOperationException("Not all explicitly missing particle mesh references were located.");
        return changed;
    }

    private static void RequireIsolated()
    {
        if (!Application.dataPath.Contains("/.local/maze-native-probe/")) throw new InvalidOperationException("Probe requires the isolated project.");
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
    private static IEnumerable<GameObject> MissingRoots(Scene scene) => scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Transform>(true)).Select(transform => transform.gameObject).Where(go => PrefabUtility.IsOutermostPrefabInstanceRoot(go) && PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.MissingAsset).OrderBy(RootIdentity);
    private static string RootIdentity(GameObject root) => DurableIdentity(root.transform) ?? throw new InvalidOperationException("Root has no authored transform identity.");
    private static string Identity(Object obj) => obj == null ? "null" : DurableIdentity(obj) ?? "temporary_untracked";
    private static string DurableIdentity(Object obj)
    {
        var id = GlobalObjectId.GetGlobalObjectIdSlow(obj);
        if (id.identifierType == 0 || id.targetObjectId == 0) return null;
        return id.identifierType == 2 ? "scene:" + id.targetObjectId + ":" + id.targetPrefabId : id.assetGUID + ":" + id.targetObjectId + ":" + id.targetPrefabId;
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
                if (generated.Length > 0) File.AppendAllText("ftue-native-derived-cache-deltas.jsonl", JsonConvert.SerializeObject(new { identity = context.Key(component), generatedVisualRows = generated.Reverse().ToArray(), reason = "native_RenderersContainer_UpdateRenderers_includes_generated_unit_boxes" }) + "\n");
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
