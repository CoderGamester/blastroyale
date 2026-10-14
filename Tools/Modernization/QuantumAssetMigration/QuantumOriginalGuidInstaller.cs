using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Quantum;
using Quantum.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>Game-owned original asset identity registry installer; invokes the real SDK APIs.</summary>
public static class QuantumOriginalGuidInstaller {
  [Serializable] public sealed class Registry { public int Version; public Entry[] Entries; }
  [Serializable] public sealed class Entry {
    public long QuantumGuid;
    public string OriginalPath;
    public string NativePath;
    public string UnityPath;
    public string UnityGuid;
    public long UnityLocalId;
    public string SourceSha256;
    public string Type;
  }
  public static void Apply() {
#if !QUANTUM_DISABLE_ASSET_OBJECT_POSTPROCESSOR
    throw new InvalidOperationException("Restore original identity with QUANTUM_DISABLE_ASSET_OBJECT_POSTPROCESSOR enabled before authored assets are imported. Re-enable the real SDK postprocessor after this step.");
#else
    var args = Environment.GetCommandLineArgs();
    var option = Array.IndexOf(args, "-blastQuantumGuidRegistry");
    if (option < 0 || option + 1 >= args.Length) throw new InvalidOperationException("Missing -blastQuantumGuidRegistry JSON path");
    ApplyRegistry(args[option + 1]);
#endif
  }
  private static void ApplyRegistry(string registryPath) {
    var registry = JsonUtility.FromJson<Registry>(File.ReadAllText(registryPath));
    if (registry == null || registry.Version != 1 || registry.Entries == null || registry.Entries.Length != 213 || registry.Entries.Select(e => e.QuantumGuid).Distinct().Count() != 213)
      throw new InvalidOperationException("Expected the reviewed 213 unique original asset identities");
    var project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    var verified = registry.Entries.Select(entry => {
      var file = Path.GetFullPath(Path.Combine(project, entry.UnityPath));
      if (!entry.UnityPath.StartsWith("Assets/", StringComparison.Ordinal) || !file.StartsWith(project + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !File.Exists(file))
        throw new InvalidOperationException("Missing or unsafe authored asset path: " + entry.UnityPath);
      using (var hash = SHA256.Create()) {
        var digest = BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(file))).Replace("-", "").ToLowerInvariant();
        if (digest != entry.SourceSha256) throw new InvalidOperationException("Authored asset checksum changed; review registry: " + entry.UnityPath);
      }
      if (AssetDatabase.GUIDToAssetPath(entry.UnityGuid) != entry.UnityPath)
        throw new InvalidOperationException("Authored Unity GUID/path differs: " + entry.UnityPath);
      var asset = AssetDatabase.LoadAllAssetsAtPath(entry.UnityPath).OfType<AssetObject>().SingleOrDefault(a =>
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out string guid, out long localId) && guid == entry.UnityGuid && localId == entry.UnityLocalId);
      if (!asset || asset.GetType().FullName != entry.Type)
        throw new InvalidOperationException("Actual native SDK object identity/type differs: " + entry.UnityPath + "[" + entry.UnityLocalId + "]");
      return (entry, asset);
    }).ToArray();
    // Every source is checked before the first mutation. Native SDK settings owns the override representation.
    foreach (var pair in verified) {
      var original = new AssetGuid(pair.entry.QuantumGuid);
      pair.asset.Guid = original;
      pair.asset.Path = pair.entry.NativePath;
      QuantumUnityDBUtilities.SetAssetGuidOverride(pair.asset, original);
      EditorUtility.SetDirty(pair.asset);
    }
    AssetDatabase.SaveAssets();
    foreach (var pair in verified) {
      if (pair.asset.Guid.Value != pair.entry.QuantumGuid || !QuantumUnityDBUtilities.IsAssetGuidOverriden(pair.asset))
        throw new InvalidOperationException("Real SDK original identity registration failed: " + pair.entry.UnityPath);
    }
    Debug.Log("[Quantum Original Guid Installer] Registered all213 original asset IDs through real SDK settings, after exact content/Unity identity/type checks");
  }
}
