using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Quantum;
using UnityEditor;
using UnityEngine;

public static class Q3AssetMigrationValidation {
  public static readonly string Evidence = Path.GetFullPath(Path.Combine(Application.dataPath, "../../quantum-migration"));
  private static void Guard() {
    if (!Application.dataPath.Replace('\\', '/').EndsWith("/.local/quantum3-source-project/Assets", StringComparison.Ordinal))
      throw new InvalidOperationException("This guarded helper only writes the isolated migration project.");
  }
  private static string ScriptReference(ScriptableObject instance) {
    try {
      using (var serialized = new SerializedObject(instance)) {
        var script = serialized.FindProperty("m_Script").objectReferenceValue;
        if (!script || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(script, out var guid, out long fileId))
          throw new InvalidOperationException("Actual SDK script metadata unavailable for " + instance.GetType());
        return $"{{fileID: {fileId}, guid: {guid}, type: 3}}";
      }
    } finally { UnityEngine.Object.DestroyImmediate(instance); }
  }
#if QUANTUM_ENABLE_MIGRATION
  public static void PersistPrefabScriptTransfers() {
    Guard();
#pragma warning disable CS0612, CS0618
    var nativePrototypeScript = ScriptReference(AssetObject.Create<LazyEntityPrototype>());
    var oldPrototypeScript = ScriptReference(ScriptableObject.CreateInstance<global::EntityPrototypeAsset>());
    var oldViewScript = ScriptReference(ScriptableObject.CreateInstance<global::EntityViewAsset>());
#pragma warning restore CS0612, CS0618
    var nativeViewScript = ScriptReference(AssetObject.Create<Quantum.EntityView>());
    var rows = new List<object>();
    foreach (var jsonFile in Directory.GetFiles(Path.Combine(Evidence, "Q2AssetExport"), "*.json")) {
      var source = JObject.Parse(File.ReadAllText(jsonFile));
      var assetPath = (string)source["Identifier"]?["Path"];
      if (assetPath == null || !(assetPath.EndsWith("|EntityPrototype") || assetPath.EndsWith("|EntityView"))) continue;
      var prototype = assetPath.EndsWith("|EntityPrototype");
      var oldReference = prototype ? oldPrototypeScript : oldViewScript;
      var reference = prototype ? nativePrototypeScript : nativeViewScript;
      var sourceName = Path.GetFileNameWithoutExtension(jsonFile);
      var split = sourceName.IndexOf('_');
      var guid = sourceName.Substring(0, split);
      var localId = sourceName.Substring(split + 1);
      var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
      if (prefabPath != "Assets/" + assetPath.Split('|')[0] + ".prefab") throw new InvalidOperationException("Source prefab identity differs: " + assetPath);
      var text = File.ReadAllText(prefabPath);
      var pattern = @"(?m)(^--- !u!114 &" + Regex.Escape(localId) + @"(?: stripped)?\r?\n(?:(?!^---).)*?  m_Script: )([^\r\n]+)";
      var matches = Regex.Matches(text, pattern, RegexOptions.Singleline);
      if (matches.Count != 1) throw new InvalidOperationException("Expected exactly one original exported prefab subasset " + localId + " in " + prefabPath);
      var actual = matches[0].Groups[2].Value;
      if (actual != oldReference && actual != reference) throw new InvalidOperationException("Unexpected source script reference: " + actual);
      if (actual != reference) {
        text = Regex.Replace(text, pattern, m => m.Groups[1].Value + reference, RegexOptions.Singleline);
        File.WriteAllText(prefabPath, text);
      }
      rows.Add(new { Path = prefabPath, LocalId = localId, QuantumPath = assetPath, ActualSdkTargetScript = reference, OriginalReference = oldReference });
    }
    if (rows.Count != 40) throw new InvalidOperationException("Expected exactly 40 exported prefab asset objects, found " + rows.Count);
    File.WriteAllText(Path.Combine(Evidence, "q3-prefab-script-persistence.json"), Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented));
    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    Debug.Log("[Q3 Asset Migration Validation] Persisted 40 actual SDK prefab subasset script transfers at exact original local IDs");
  }
#endif
  private static void AssertSourceFields(JToken source, JToken actual, string field) {
    if (source is JObject sourceObject) {
      if (!(actual is JObject actualObject)) throw new InvalidOperationException("Field shape changed: " + field);
      foreach (var property in sourceObject.Properties()) {
        var value = actualObject[property.Name];
        if (value == null) throw new InvalidOperationException("Source field is missing: " + field + "." + property.Name);
        AssertSourceFields(property.Value, value, field + "." + property.Name);
      }
    } else if (source is JArray sourceArray) {
      if (!(actual is JArray actualArray) || sourceArray.Count != actualArray.Count) throw new InvalidOperationException("Source array changed: " + field);
      for (int i = 0; i < sourceArray.Count; i++) AssertSourceFields(sourceArray[i], actualArray[i], field + "[" + i + "]");
    } else if (!JToken.DeepEquals(source, actual)) {
      if ((source.Type == JTokenType.Integer || source.Type == JTokenType.Float) && (actual.Type == JTokenType.Integer || actual.Type == JTokenType.Float) && (decimal)source == (decimal)actual) return;
      throw new InvalidOperationException("Source value changed: " + field + ": " + source + " vs " + actual);
    }
  }
  public static void DumpRestoredAssetSnapshots() {
    Guard();
    var directory = Path.Combine(Evidence, "Q3RestoredSnapshots");
    Directory.CreateDirectory(directory);
    foreach (var jsonFile in Directory.GetFiles(Path.Combine(Evidence, "Q2AssetExport"), "*.json")) {
      var sourceName = Path.GetFileNameWithoutExtension(jsonFile);
      var split = sourceName.IndexOf('_');
      var unityGuid = sourceName.Substring(0, split);
      var localId = long.Parse(sourceName.Substring(split + 1));
      var unityPath = AssetDatabase.GUIDToAssetPath(unityGuid);
      var asset = AssetDatabase.LoadAllAssetsAtPath(unityPath).OfType<AssetObject>().SingleOrDefault(a => AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out string guid, out long id) && guid == unityGuid && id == localId);
      if (!asset) throw new InvalidOperationException("Original restored asset absent: " + sourceName);
      File.WriteAllText(Path.Combine(directory, sourceName + ".json"), JsonUtility.ToJson(asset, true));
    }
    Debug.Log("[Q3 Asset Migration Validation] Dumped all 213 actual restored SDK instances for semantic schema audit");
  }
  private static JToken CanonicalMapCacheToken(JToken token) => token is JObject obj
    ? new JObject(obj.Properties().OrderBy(property => property.Name, StringComparer.Ordinal).Select(property => new JProperty(property.Name, CanonicalMapCacheToken(property.Value))))
    : token is JArray array ? new JArray(array.Select(CanonicalMapCacheToken)) : token.DeepClone();
  private static string MapCacheHash(JToken token) {
    using (var sha = System.Security.Cryptography.SHA256.Create())
      return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(CanonicalMapCacheToken(token).ToString(Newtonsoft.Json.Formatting.None)))).Replace("-", "").ToLowerInvariant();
  }
  private static void ValidateExactNativeMapEntityCache(JObject source, JObject actual, Quantum.Map map, List<string> accepted) {
    if (JToken.DeepEquals(source["MapEntities"], actual["MapEntities"])) return;
    if (!(source["MapEntities"] is JArray oldCache) || oldCache.Count != 0)
      throw new InvalidOperationException("Only originally empty exported map entity caches can use genuine native restoration evidence.");
    var allowed = new Dictionary<string, int> {
      ["Assets/AddressableResources/Maps/Battlelands/BattlelandsMap.asset"] = 479,
      ["Assets/AddressableResources/Maps/Cemetery/Cemetery.asset"] = 139,
      ["Assets/AddressableResources/Maps/District/District.asset"] = 223,
      ["Assets/AddressableResources/Maps/Fortress/Fortress.asset"] = 140,
      ["Assets/AddressableResources/Maps/FtueDeck/FtueDeck.asset"] = 17,
      ["Assets/AddressableResources/Maps/IslandOne/IslandOne.asset"] = 2824,
      ["Assets/AddressableResources/Maps/Islands/IslandsMap.asset"] = 943,
      ["Assets/AddressableResources/Maps/MazeMayhem/MazeMayhem.asset"] = 708,
      ["Assets/AddressableResources/Maps/Test/TestScene.asset"] = 80,
      ["Assets/AddressableResources/Maps/TestAssets/TestAssetsMap.asset"] = 10
    };
    var ledgerPath = Environment.GetEnvironmentVariable("BLAST_NATIVE_MAP_CACHE_LEDGER");
    if (string.IsNullOrEmpty(ledgerPath)) ledgerPath = Path.Combine(Evidence, "q3-native-map-cache-ledger.json");
    if (!File.Exists(ledgerPath)) throw new InvalidOperationException("Exact native map cache acceptance ledger is required: " + ledgerPath);
    var ledger = JObject.Parse(File.ReadAllText(ledgerPath));
    var entries = ((JArray)ledger["Entries"]).OfType<JObject>().ToArray();
    if ((int)ledger["Version"] != 1 || entries.Length != 10 || !allowed.Keys.ToHashSet().SetEquals(entries.Select(entry => (string)entry["UnityMapPath"])))
      throw new InvalidOperationException("Exact accepted ten-map identity set required.");
    var path = AssetDatabase.GetAssetPath(map);
    var row = entries.Single(entry => (string)entry["UnityMapPath"] == path);
    if (!allowed.TryGetValue(path, out var expectedCount) || (int)row["PrototypeCount"] != expectedCount || (int)row["NativePrototypeSourceCount"] != expectedCount ||
        (int)row["SceneReferenceCount"] != expectedCount || (long)row["QuantumGuid"] != map.Guid.Value ||
        (string)row["UnityMapGuid"] != AssetDatabase.AssetPathToGUID(path) || (bool)row["SourceMapEntitiesEmpty"] != true ||
        (string)row["NativeBaker"] != "QuantumMapDataBaker.BakePrototypes(data,true)" || ((JArray)actual["MapEntities"]).Count != expectedCount)
      throw new InvalidOperationException("Exact native map cache identity/source/count contract differs: " + path);
    Func<string,string> fileHash = file => {
      using (var sha = System.Security.Cryptography.SHA256.Create())
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(file))).Replace("-", "").ToLowerInvariant();
    };
    var scene = (string)row["ScenePath"];
    if (fileHash(path) != (string)row["NativeMapFileSha256"] || fileHash(scene) != (string)row["SceneFileSha256"] ||
        fileHash(scene + ".meta") != (string)row["SceneMetaSha256"] || AssetDatabase.AssetPathToGUID(scene) != (string)row["OriginalSceneGuid"])
      throw new InvalidOperationException("Verified native map/scene files or original scene metadata changed: " + path);
    if (MapCacheHash(actual["MapEntities"]) != (string)row["NativeMapEntitiesSha256"] || MapCacheHash(actual["references"]) != (string)row["NativeManagedReferencesSha256"])
      throw new InvalidOperationException("Native prototype bodies or typed managed references differ from exact accepted cache: " + path);
    var cacheIds = ((JContainer)actual["MapEntities"]).DescendantsAndSelf().OfType<JProperty>().Where(property => property.Name == "rid")
      .Select(property => (long)property.Value).Where(id => id > 0).ToHashSet();
    if (!cacheIds.SetEquals(((JArray)actual["references"]["RefIds"]).Select(entry => (long)entry["rid"])))
      throw new InvalidOperationException("Native managed reference registry contains bodies outside the exact entity cache: " + path);
    var other = (JObject)actual.DeepClone(); other.Remove("MapEntities"); other.Remove("references");
    if (MapCacheHash(other) != (string)row["NativeOtherMapFieldsSha256"])
      throw new InvalidOperationException("Another native map field differs from its accepted preservation fingerprint: " + path);
    // All other original fields continue through strict AssertSourceFields below.
    source["MapEntities"] = actual["MapEntities"].DeepClone();
    accepted.Add("Exact originally empty MapEntities -> actual SDK prototype-only bake, exact native typed cache/other fields/scene/meta hashes, " + expectedCount + " authored sources and scene references");
  }

  private static List<string> ValidateSourceFields(JObject source, JObject actual, AssetObject asset, string sourceName) {
    var retiredFields = new List<string>();
    if (asset is Quantum.BinaryData && !JToken.DeepEquals(source["Data"], actual["Data"])) {
      var guid = asset.Guid.Value;
      JObject conversion = null;
      foreach (var ledger in new[] { "q3-native-triangle-conversion.json", "q3-native-navmesh-conversion.json" }) {
        var path = Path.Combine(Evidence, ledger);
        if (!File.Exists(path)) continue;
        var entries = JArray.Parse(File.ReadAllText(path));
        var triangle = ledger.Contains("triangle");
        if (entries.Count != (triangle ? 10 : 9)) throw new InvalidOperationException("Native binary conversion count differs: " + ledger);
        var matches = entries.OfType<JObject>().Where(row => (long)row[triangle ? "BinaryGuid" : "DataGuid"] == guid).ToArray();
        if (matches.Length == 0) continue;
        if (matches.Length != 1 || conversion != null) throw new InvalidOperationException("Native conversion identity is ambiguous: " + guid);
        conversion = matches[0];
        var verified = triangle ? "NativeReadbackPreservedEveryRawVertexWindingAndColliderIndex" : "RoundtripPreservedAllAuthoredGeometryTopologyRegionsCostsLinksAndGridNodes";
        if ((bool?)conversion[verified] != true) throw new InvalidOperationException("Native binary readback did not verify: " + guid);
      }
      if (conversion == null) throw new InvalidOperationException("Binary changed without native serializer/readback evidence: " + guid);
      Func<JToken, string> byteHash = token => {
        if (!(token is JArray values)) throw new InvalidOperationException("Binary JSON must contain an actual byte array");
        using (var sha = System.Security.Cryptography.SHA256.Create())
          return BitConverter.ToString(sha.ComputeHash(values.Select(value => checked((byte)(int)value)).ToArray())).Replace("-", "").ToLowerInvariant();
      };
      if (byteHash(source["Data"]) != (string)conversion["OriginalStoredSha256"] || byteHash(actual["Data"]) != (string)conversion["NativeStoredSha256"] ||
          (bool)source["IsCompressed"] != (bool)conversion["OriginalIsCompressed"] || (bool)actual["IsCompressed"] != (bool)conversion["NativeIsCompressed"])
        throw new InvalidOperationException("Exact source/native binary hashes or compression differ from genuine conversion evidence: " + guid);
      source["Data"] = actual["Data"].DeepClone();
      retiredFields.Add("Exact Q2 binary hash -> exact actual Q3 serializer/readback-verified payload hash; authored geometry and topology preserved");
    }
    // Q3 removed the legacy file-loader path. Every original navmesh uses BinaryData DataAsset and has an empty legacy path.
    if (asset is Quantum.NavMesh && source["DataFilepath"] != null && actual["DataFilepath"] == null) {
      if ((string)source["DataFilepath"] != "") throw new InvalidOperationException("A nonempty legacy navmesh file path needs explicit migration");
      source.Remove("DataFilepath");
      retiredFields.Add("DataFilepath (empty legacy loader path; real DataAsset retained)");
    }
    if (asset is Quantum.Map nativeMap) {
      ValidateExactNativeMapEntityCache(source, actual, nativeMap, retiredFields);
      if ((int)source["TriangleMeshCellSize"] != 4 || (int)actual["TriangleMeshCellSize"] != -1 || (long)actual["SceneMeshCellSize"]?["RawValue"] != 262144)
        throw new InvalidOperationException("Map's integer-to-FP scene mesh cell migration differs");
      source.Remove("TriangleMeshCellSize"); actual.Remove("TriangleMeshCellSize");
      retiredFields.Add("TriangleMeshCellSize4 -> native FP SceneMeshCellSize262144");
      if ((bool)source["OptionalTriangleMetadataSerializationEnabled"] != true || (string)source["StaticColliders3DTrianglesBinaryFile"] != "")
        throw new InvalidOperationException("Nondefault retired map serialization fields require explicit migration");
      source.Remove("OptionalTriangleMetadataSerializationEnabled"); source.Remove("StaticColliders3DTrianglesBinaryFile");
      retiredFields.Add("OptionalTriangleMetadataSerializationEnabled=true and empty obsolete triangle file path; actual SerializeTrianglesMetadata/BinaryData refs retained");
      foreach (var collider in (JArray)source["StaticColliders2D"]) {
        var polygon = collider["PolygonCollider"] as JObject;
        if (polygon?["Identifier"] == null) continue;
        var identifier = polygon["Identifier"];
        if ((string)identifier["Path"] != "" || (long)identifier["Guid"]?["Value"] != 0)
        throw new InvalidOperationException("Nonempty legacy embedded polygon identifier requires explicit migration");
        polygon.Remove("Identifier");
      }
      retiredFields.Add("Empty embedded polygon AssetObject identifiers retired; complete shape geometry retained");
      foreach (var collider in (JArray)source["StaticColliders3D"]) {
        var item = (JObject)collider;
        if (item["MutableMode"] == null) continue;
        if ((int)item["MutableMode"] != 0) throw new InvalidOperationException("Nondefault legacy collider mutability needs explicit migration");
        item.Remove("MutableMode");
      }
    }
    if (asset is Quantum.NavMeshAgentConfig && source["ShowDebugSteering"] != null && actual["ShowDebugSteering"] == null) {
      if ((bool)source["ShowDebugSteering"] != false) throw new InvalidOperationException("Enabled retired steering debug flag requires explicit migration");
      source.Remove("ShowDebugSteering"); retiredFields.Add("Disabled legacy steering debug display flag retired");
    }
    if (asset is Quantum.QuantumGameModeConfigs && source["QuantumConfigs"] is JArray gameModes) {
      const string legacySystem = "Quantum.Systems.ShrinkingCircleSystem, quantum.code, Version=2.1.0.0, Culture=neutral, PublicKeyToken=null";
      var nativeSystem = typeof(Quantum.Systems.ShrinkingCircleSystem).AssemblyQualifiedName;
      foreach (var mode in gameModes) {
        if (!(mode["Systems"] is JArray systems)) continue;
        for (int i = 0; i < systems.Count; i++) if ((string)systems[i] == legacySystem) systems[i] = nativeSystem;
      }
      retiredFields.Add("Two exact legacy shrinking-circle system assembly identities -> actual native typeof.AssemblyQualifiedName");
    }
    AssertSourceFields(source, actual, sourceName);
    return retiredFields;
  }
  public static void ValidateRestoredAssets() {
    Guard();
    var registryPath = Environment.GetEnvironmentVariable("BLAST_QUANTUM_ORIGINAL_GUID_REGISTRY");
    if (string.IsNullOrEmpty(registryPath)) registryPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Tools/Modernization/quantum-original-guid-registry.json"));
    var registry = JObject.Parse(File.ReadAllText(registryPath));
    var entries = ((JArray)registry["Entries"]).OfType<JObject>().ToArray();
    if ((int)registry["Version"] != 1 || entries.Length != 213 || entries.Select(row => (long)row["QuantumGuid"]).Distinct().Count() != 213)
      throw new InvalidOperationException("The exact one-to-one213 canonical native asset registry is required.");
    var seen = new HashSet<long>();
    var rows = new List<object>();
    var nativeRows = new List<object>();
    foreach (var jsonFile in Directory.GetFiles(Path.Combine(Evidence, "Q2AssetExport"), "*.json")) {
      var source = JObject.Parse(File.ReadAllText(jsonFile));
      var sourceName = Path.GetFileNameWithoutExtension(jsonFile);
      var originalGuid = (long)source["Identifier"]["Guid"]["Value"];
      if (!seen.Add(originalGuid)) throw new InvalidOperationException("Duplicated original exported Quantum GUID: " + sourceName);
      var entry = entries.Single(row => (long)row["QuantumGuid"] == originalGuid);
      if ((string)source["Identifier"]["Path"] != (string)entry["OriginalPath"])
        throw new InvalidOperationException("Original export path differs from its exact canonical registry identity: " + sourceName);
      var unityPath = (string)entry["UnityPath"];
      var unityGuid = (string)entry["UnityGuid"];
      var localId = (long)entry["UnityLocalId"];
      var assets = AssetDatabase.LoadAllAssetsAtPath(unityPath).OfType<AssetObject>();
      var asset = assets.SingleOrDefault(a => AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out string guid, out long id) && guid == unityGuid && id == localId);
      if (!asset || asset.Guid.Value != originalGuid || asset.Path != (string)entry["NativePath"] || asset.GetType().FullName != (string)entry["Type"])
        throw new InvalidOperationException("Exact canonical asset Unity path/GUID/local ID/native type/original Quantum GUID differs: " + sourceName);
      using (var sha = System.Security.Cryptography.SHA256.Create()) {
        var hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(unityPath))).Replace("-", "").ToLowerInvariant();
        if (hash != (string)entry["SourceSha256"]) throw new InvalidOperationException("Exact canonical native asset bytes differ from registry: " + unityPath);
      }
      var actual = JObject.Parse(JsonUtility.ToJson(asset));
      var storageMoved = (string)entry["OriginalPath"] != (string)entry["NativePath"];
      if (storageMoved) {
        if (!(asset is Quantum.EntityPrototype) || !unityPath.EndsWith(".qprototype", StringComparison.Ordinal) || !((string)entry["OriginalPath"]).EndsWith("|EntityPrototype", StringComparison.Ordinal))
          throw new InvalidOperationException("Unexpected canonical storage relocation: " + sourceName);
        source["Identifier"]["Path"] = (string)entry["NativePath"];
      }
      var retiredFields = ValidateSourceFields(source, actual, asset, sourceName);
      if (storageMoved) retiredFields.Add("Exact registered Q2 prefab prototype path -> actual canonical native .qprototype storage; original Quantum GUID preserved one-to-one");
      rows.Add(new { OriginalExport = sourceName, OriginalQuantumPath = (string)entry["OriginalPath"], UnityPath = unityPath, UnityGuid = unityGuid, LocalId = localId, QuantumGuid = asset.Guid.Value, QuantumPath = asset.Path, NativeType = asset.GetType().FullName, PreservedAllActiveExportedFields = true, RetiredFields = retiredFields });
      if (asset is Quantum.EntityPrototype nativePrototype) {
        var prefabPath = "Assets/" + ((string)entry["OriginalPath"]).Split('|')[0] + ".prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var parent = prefab ? prefab.GetComponent<QuantumEntityPrototype>() : null;
        if (!parent) throw new InvalidOperationException("Exact original native prototype parent prefab missing: " + prefabPath);
        var view = AssetDatabase.LoadAllAssetsAtPath(prefabPath).OfType<Quantum.EntityView>().SingleOrDefault();
        var prototype = AssetObject.Create<Quantum.EntityPrototype>();
        try {
          prototype.Identifier = asset.Identifier;
          parent.InitializeAssetObject(prototype, view);
          var components = prototype.Container.Components.Select(c => new { Type = c.GetType().FullName, Data = JObject.Parse(JsonUtility.ToJson(c)) }).ToList();
          var stored = nativePrototype.Container.Components.Select(c => new { Type = c.GetType().FullName, Data = JObject.Parse(JsonUtility.ToJson(c)) }).ToList();
          if (components.Count == 0 || !JToken.DeepEquals(JArray.FromObject(components), JArray.FromObject(stored)))
            throw new InvalidOperationException("Actual canonical prototype cache differs from genuine source SDK reconstruction: " + unityPath);
          nativeRows.Add(new { Path = asset.Path, Guid = asset.Guid.Value, Components = components, CanonicalStorageMatchesNativeSourceReconstruction = true });
        } finally { UnityEngine.Object.DestroyImmediate(prototype); }
      }
    }
    if (seen.Count != 213 || !seen.SetEquals(entries.Select(row => (long)row["QuantumGuid"])) || rows.Count != 213 || nativeRows.Count != 20)
      throw new InvalidOperationException("Exact original/canonical/native counts differ: " + seen.Count + "/" + rows.Count + "/" + nativeRows.Count);
    File.WriteAllText(Path.Combine(Evidence, "q3-restored-assets-validation.json"), Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented));
    File.WriteAllText(Path.Combine(Evidence, "Q3PreparedPrototypeComponents.json"), Newtonsoft.Json.JsonConvert.SerializeObject(nativeRows, Newtonsoft.Json.Formatting.Indented));
    Debug.Log("[Q3 Asset Migration Validation] All213 exact original Quantum GUIDs +registered canonical Unity GUID/local IDs/types/bytes and exported fields preserved;20 genuine native prototype caches match source reconstruction");
  }
  public static void ValidateSceneMarkers() {
    Guard();
    var rows = new List<object>();
    var storms = 0; var drops = 0;
    foreach (var path in new[] {
      "Assets/AddressableResources/Maps/IslandOne/IslandOneScene.unity",
      "Assets/AddressableResources/Maps/MazeMayhem/MazeMayhemScene.unity",
      "Assets/AddressableResources/Maps/Battlelands/BattlelandsMapScene.unity",
      "Assets/AddressableResources/Maps/District/DistrictScene.unity"
    }) {
      UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
      foreach (var component in UnityEngine.Object.FindObjectsByType<QuantumUnityComponentPrototype>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
        if (!(component is QPrototypeStormAnchor) && !(component is QPrototypeAirDropSpawner)) continue;
        var parent = component.GetComponent<QuantumEntityPrototype>();
        if (!parent) throw new InvalidOperationException("Native marker parent missing: " + component.name);
        var prototype = component.CreatePrototype(new QuantumEntityPrototypeConverter(parent));
        if (prototype.GetType() == typeof(Quantum.Prototypes.StormAnchorPrototype)) storms++;
        else if (prototype.GetType() == typeof(Quantum.Prototypes.AirDropSpawnerPrototype)) drops++;
        else throw new InvalidOperationException("Marker's actual native prototype type differs: " + prototype.GetType());
        rows.Add(new { Scene = path, Name = component.gameObject.name, Type = prototype.GetType().FullName });
      }
    }
    if (storms != 287 || drops != 387) throw new InvalidOperationException("Native scene marker counts differ: " + storms + "/" + drops);
    File.WriteAllText(Path.Combine(Evidence, "Q3NativeSceneMarkers.json"), Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented));
    Debug.Log("[Q3 Asset Migration Validation] Actual native marker factories built all 287 StormAnchor + 387 AirDropSpawner prototypes");
  }

  public static void UpgradeNativePrototypeStorage() {
    Guard();
    var rows = new List<object>();
    var exports = Directory.GetFiles(Path.Combine(Evidence, "Q2AssetExport"), "*.json");
    // Register actual original IDs for remaining assets before the SDK postprocessor becomes active.
    var nonPrototypeOverrides = 0;
    foreach (var file in exports) {
      var source = JObject.Parse(File.ReadAllText(file));
      var name = Path.GetFileNameWithoutExtension(file); var split = name.IndexOf('_');
      var unityGuid = name.Substring(0, split); var localId = long.Parse(name.Substring(split + 1));
      var path = AssetDatabase.GUIDToAssetPath(unityGuid);
      var asset = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AssetObject>().Single(a => AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out string guid, out long id) && guid == unityGuid && id == localId);
      if (asset is Quantum.EntityPrototype) continue;
      var originalGuid = new AssetGuid((long)source["Identifier"]?["Guid"]?["Value"]);
      if (asset.Guid != originalGuid) throw new InvalidOperationException("Original asset ID changed before registration: " + path);
      if (Quantum.Editor.QuantumUnityDBUtilities.CreateDeterministicAssetGuid(asset) != originalGuid) {
        Quantum.Editor.QuantumUnityDBUtilities.SetAssetGuidOverride(asset, originalGuid);
        nonPrototypeOverrides++;
      }
    }
    foreach (var file in exports) {
      var source = JObject.Parse(File.ReadAllText(file));
      var originalPath = (string)source["Identifier"]?["Path"];
      if (originalPath == null || !originalPath.EndsWith("|EntityPrototype")) continue;
      var name = Path.GetFileNameWithoutExtension(file); var split = name.IndexOf('_');
      var unityGuid = name.Substring(0, split); var localId = name.Substring(split + 1);
      var prefabPath = AssetDatabase.GUIDToAssetPath(unityGuid);
      var originalGuid = new AssetGuid((long)source["Identifier"]?["Guid"]?["Value"]);
      var nativePath = Quantum.Editor.QuantumEntityPrototypeAssetObjectImporter.GetPathForPrefab(prefabPath);
      if (File.Exists(nativePath)) throw new InvalidOperationException("Unexpected standalone prototype exists: " + nativePath);
      var text = File.ReadAllText(prefabPath);
      var pattern = @"(?m)^--- !u!114 &" + Regex.Escape(localId) + @"(?: stripped)?\r?\n.*?(?=^--- !u!|\z)";
      if (Regex.Matches(text, pattern, RegexOptions.Singleline).Count != 1) throw new InvalidOperationException("Original nested prototype identity differs: " + prefabPath);
      // Imported nested ScriptableObjects are not persistently editable on this Unity version. Remove only the proven prototype document.
      File.WriteAllText(prefabPath, Regex.Replace(text, pattern, "", RegexOptions.Singleline));
      AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceSynchronousImport);
      File.WriteAllText(nativePath, unityGuid);
      AssetDatabase.ImportAsset(nativePath, ImportAssetOptions.ForceSynchronousImport);
      var native = AssetDatabase.LoadAssetAtPath<Quantum.EntityPrototype>(nativePath);
      if (!native || native.Container.Components.Length == 0) throw new InvalidOperationException("SDK importer produced empty native prototype: " + nativePath);
      Quantum.Editor.QuantumUnityDBUtilities.SetAssetGuidOverride(native, originalGuid);
      AssetDatabase.SaveAssets();
      AssetDatabase.ImportAsset(nativePath, ImportAssetOptions.ForceSynchronousImport);
      native = AssetDatabase.LoadAssetAtPath<Quantum.EntityPrototype>(nativePath);
      if (native.Guid != originalGuid) throw new InvalidOperationException("SDK standalone GUID override failed: " + nativePath);
      rows.Add(new { OriginalPath = originalPath, NativePath = native.Path, OriginalQuantumGuid = originalGuid.Value, NativeQuantumGuid = native.Guid.Value, PrefabPath = prefabPath, NativeUnityPath = nativePath, OriginalLocalId = localId });
    }
    if (rows.Count != 20) throw new InvalidOperationException("Expected 20 native standalone prototypes, got " + rows.Count);
    QuantumEditorSettings.Global.AssetSearchPaths = new[] { "Assets/AddressableResources", "Assets/Resources", "Assets/QuantumUser/Resources" };
    EditorUtility.SetDirty(QuantumEditorSettings.Global);
    AssetDatabase.SaveAssets();
    File.WriteAllText(Path.Combine(Evidence, "q3-native-prototype-storage.json"), Newtonsoft.Json.JsonConvert.SerializeObject(new { PrototypeCount = rows.Count, NonPrototypeGuidOverrides = nonPrototypeOverrides, Prototypes = rows }, Newtonsoft.Json.Formatting.Indented));
    Debug.Log("[Q3 Asset Migration Validation] SDK importer built 20 native standalone prototypes with original GUIDs; registered " + nonPrototypeOverrides + " other required original GUID overrides");
  }



  public static void ReimportAndValidateFinalAssets() {
    Guard();
    var addressables = AssetDatabase.LoadAssetAtPath<UnityEditor.AddressableAssets.Settings.AddressableAssetSettings>("Assets/AddressableAssetsData/AddressableAssetSettings.asset");
    if (!addressables) throw new InvalidOperationException("Real original Addressables settings missing");
    UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings = addressables;
    var searchPaths = QuantumEditorSettings.Global.AssetSearchPaths;
    foreach (var guid in AssetDatabase.FindAssets("t:AssetObject", searchPaths)) {
      AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceSynchronousImport);
    }
    Quantum.Editor.QuantumUnityDBUtilities.SetQuantumUnityDBRefreshMode(true);
    Quantum.Editor.QuantumUnityDBUtilities.RefreshGlobalDB(force: true);
    AssetDatabase.SaveAssets();
    ValidateFinalNativeAssets();
  }
  public static void ValidateFinalNativeAssets() {
    Guard();
    var all = Quantum.Editor.QuantumUnityDBUtilities.IterateAssets().Select(x => (AssetObject)x.pptrValue).Where(x => x).ToArray();
    if (all.Any(x => x.GetType().Name.Contains("LazyEntityPrototype"))) throw new InvalidOperationException("Legacy lazy prototype remains");
    if (all.Any(x => !x.Guid.IsValid)) throw new InvalidOperationException("Invalid actual AssetObject GUID remains");
    var lookup = all.ToDictionary(x => x.Guid.Value);
    var storage = JObject.Parse(File.ReadAllText(Path.Combine(Evidence, "q3-native-prototype-storage.json")));
    var prototypePaths = ((JArray)storage["Prototypes"]).ToDictionary(x => (long)x["OriginalQuantumGuid"], x => (string)x["NativePath"]);
    var rows = new List<object>(); var nativeRows = new List<object>();
    foreach (var file in Directory.GetFiles(Path.Combine(Evidence, "Q2AssetExport"), "*.json")) {
      var source = JObject.Parse(File.ReadAllText(file));
      var guid = (long)source["Identifier"]?["Guid"]?["Value"];
      if (!lookup.TryGetValue(guid, out var asset)) throw new InvalidOperationException("Original Quantum GUID missing from native assets: " + guid);
      var originalPath = (string)source["Identifier"]?["Path"];
      var expectedPath = prototypePaths.TryGetValue(guid, out var nativePath) ? nativePath : originalPath;
      if (asset.Path != expectedPath) throw new InvalidOperationException("Unexpected native asset path: " + asset.Path + " vs " + expectedPath);
      source["Identifier"]["Path"] = expectedPath;
      var retired = ValidateSourceFields(source, JObject.Parse(JsonUtility.ToJson(asset)), asset, Path.GetFileName(file));
      if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string unityGuid, out long unityLocalId)) throw new InvalidOperationException("Final native Unity identity missing");
      var unityPath = AssetDatabase.GetAssetPath(asset);
      var sha256 = System.Security.Cryptography.SHA256.Create();
      var sourceHash = BitConverter.ToString(sha256.ComputeHash(File.ReadAllBytes(unityPath))).Replace("-", "").ToLowerInvariant();
      rows.Add(new { QuantumGuid = guid, OriginalPath = originalPath, NativePath = asset.Path, UnityPath = unityPath, UnityGuid = unityGuid, UnityLocalId = unityLocalId, SourceSha256 = sourceHash, Type = asset.GetType().FullName, RetiredFields = retired });
      if (asset is Quantum.EntityPrototype prototype) {
        var components = prototype.Container.Components.Select(c => new { Type = c.GetType().FullName, Data = JObject.Parse(JsonUtility.ToJson(c)) }).ToList();
        if (components.Count == 0) throw new InvalidOperationException("Final native prototype is empty: " + asset.Path);
        nativeRows.Add(new { Path = originalPath, NativePath = asset.Path, Guid = guid, Components = components });
      }
    }
    if (rows.Count != 213 || nativeRows.Count != 20) throw new InvalidOperationException("Final counts differ");
    File.WriteAllText(Path.Combine(Evidence, "q3-final-native-assets-validation.json"), Newtonsoft.Json.JsonConvert.SerializeObject(new { OriginalAssetCount = rows.Count, ActualAssetObjectCount = all.Length, AdditionalSdkAssets = all.Where(x => rows.All(r => (long)r.GetType().GetProperty("QuantumGuid").GetValue(r) != x.Guid.Value)).Select(x => new { Guid = x.Guid.Value, Path = x.Path, Type = x.GetType().FullName }), OriginalAssets = rows }, Newtonsoft.Json.Formatting.Indented));
    File.WriteAllText(Path.Combine(Evidence, "Q3FinalPrototypeComponents.json"), Newtonsoft.Json.JsonConvert.SerializeObject(nativeRows, Newtonsoft.Json.Formatting.Indented));
    Quantum.Editor.QuantumUnityDBUtilities.ExportAsJson(Path.Combine(Evidence, "Q3FinalRuntimeDB.json"));
    Debug.Log("[Q3 Asset Migration Validation] Final native asset validation passed: all 213 original GUIDs, 20 populated standalone prototypes, " + all.Length + " actual assets including SDK additions");
  }

  public static void MigrateOptionalSystemNames() {
    Guard();
    const string legacy = "Quantum.Systems.ShrinkingCircleSystem, quantum.code, Version=2.1.0.0, Culture=neutral, PublicKeyToken=null";
    var native = typeof(Quantum.Systems.ShrinkingCircleSystem).AssemblyQualifiedName;
    const string path = "Assets/AddressableResources/Configs/GameModeConfigs.asset";
    var configs = AssetDatabase.LoadAssetAtPath<Quantum.QuantumGameModeConfigs>(path);
    if (!configs) throw new InvalidOperationException("Native game mode config asset is missing");
    var changed = 0;
    foreach (var mode in configs.QuantumConfigs) {
      if (mode.Systems == null) continue;
      for (int i = 0; i < mode.Systems.Count; i++) {
        if (mode.Systems[i] == legacy) { mode.Systems[i] = native; changed++; }
      }
    }
    if (changed != 2) throw new InvalidOperationException("Expected exactly two original system references, found " + changed);
    EditorUtility.SetDirty(configs);
    AssetDatabase.SaveAssets();
    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
    File.WriteAllText(Path.Combine(Evidence, "q3-optional-system-identity-migration.json"), Newtonsoft.Json.JsonConvert.SerializeObject(new { Path = path, Count = changed, Original = legacy, Native = native, ActualType = typeof(Quantum.Systems.ShrinkingCircleSystem).FullName }, Newtonsoft.Json.Formatting.Indented));
    ValidateFinalNativeAssets();
    Debug.Log("[Q3 Asset Migration Validation] Migrated exactly two optional system references to the genuine native simulation assembly");
  }

}
