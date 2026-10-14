using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Reproduce only the two retired native objects; the clone never runs Boot initialization.
[InitializeOnLoad]
public static class NativeBootMissingScriptProbe
{
    private const string Active = "Blast.BootMissing.Active";
    private const string Stage = "Blast.BootMissing.Stage";
    private const string Warnings = "Blast.BootMissing.Warnings";
    private const string Deadline = "Blast.BootMissing.Deadline";
    private const string Original = "Assets/Scenes/NativeRetiredPairOriginal.unity";
    private const string Candidate = "Assets/Scenes/NativeRetiredPairCandidate.unity";
    private static int _frames;

    static NativeBootMissingScriptProbe() { if (SessionState.GetBool(Active, false)) Subscribe(); }

    public static void Run()
    {
        if (!Application.dataPath.Contains("/.local/boot-missing-script-project/"))
            throw new InvalidOperationException("Owned isolated project required.");
        var original = EditorSceneManager.OpenScene("Assets/Scenes/NativeRetiredPairInput.unity", OpenSceneMode.Single);
        ValidateOwners(original);
        var owners = MissingOwners(original).Select(owner =>
        {
            var globalId = GlobalObjectId.GetGlobalObjectIdSlow(owner);
            var hasAssetLocalId = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(owner, out string guid, out long localId);
            return new { owner = owner.name, globalId = globalId.ToString(), hasAssetLocalId, guid, localId };
        }).ToArray();
        var paths = new[] { "97a8a3caac8b73541aa8a9a1e330f479", "b7fa80e7116296f4eb4f49ec1544ee22" }
            .Select(guid => new { guid, path = AssetDatabase.GUIDToAssetPath(guid) }).ToArray();
        if (paths.Any(row => !string.IsNullOrEmpty(row.path))) throw new InvalidOperationException("A retired script resolves.");
        File.WriteAllText("boot-missing-script-owner-inventory.json", JsonConvert.SerializeObject(new { owners, paths }, Formatting.Indented));
        if (!EditorSceneManager.SaveScene(original, Original)) throw new InvalidOperationException("Native subset save failed.");
        SessionState.SetBool(Active, true);
        SessionState.SetString(Deadline, DateTime.UtcNow.AddSeconds(90).Ticks.ToString());
        Subscribe();
        // Prime the actual Search database before Play suspends delayed Editor startup.
        var search = Type.GetType("UnityEditor.Search.SearchDatabase, UnityEditor.QuickSearchModule", true);
        search.GetMethod("GetDefaultSearchDatabase", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
        BeginPlay("Original");
    }

    private static void Subscribe()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
        Application.logMessageReceived -= CountWarning;
        Application.logMessageReceived += CountWarning;
    }

    private static void BeginPlay(string name)
    {
        SessionState.SetString(Stage, name);
        SessionState.SetInt(Warnings, 0);
        _frames = 0;
        Debug.Log("[Boot Pair] Begin " + name);
        EditorApplication.isPlaying = true;
    }

    private static void Update()
    {
        if (!SessionState.GetBool(Active, false)) return;
        try
        {
            if (DateTime.UtcNow.Ticks > long.Parse(SessionState.GetString(Deadline, "0")))
                throw new TimeoutException("Native pair PlayMode probe deadline exceeded.");
            var stage = SessionState.GetString(Stage, "");
            if (EditorApplication.isPlaying)
            {
                if (++_frames < 3) return;
                var missing = MissingOwners(SceneManager.GetActiveScene()).Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
                File.WriteAllText("boot-missing-script-" + stage.ToLowerInvariant() + ".json",
                    JsonConvert.SerializeObject(new { stage, warnings = SessionState.GetInt(Warnings, 0), missingComponents = missing }, Formatting.Indented));
                SessionState.SetString(Stage, stage + "Finished");
                EditorApplication.isPlaying = false;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (stage == "OriginalFinished")
            {
                var pair = EditorSceneManager.OpenScene(Original, OpenSceneMode.Single);
                ValidateOwners(pair);
                var removed = MissingOwners(pair).Sum(GameObjectUtility.RemoveMonoBehavioursWithMissingScript);
                if (removed != 2 || MissingOwners(pair).Length != 0) throw new InvalidOperationException("Native exact-two removal failed.");
                if (!EditorSceneManager.SaveScene(pair, Candidate)) throw new InvalidOperationException("Native candidate save failed.");
                BeginPlay("Candidate");
            }
            else if (stage == "CandidateFinished")
            {
                var original = JsonConvert.DeserializeObject<Case>(File.ReadAllText("boot-missing-script-original.json"));
                var candidate = JsonConvert.DeserializeObject<Case>(File.ReadAllText("boot-missing-script-candidate.json"));
                if (original.missingComponents != 2 || candidate.missingComponents != 0)
                    throw new InvalidOperationException("Native PlayMode warning reduction failed.");
                File.WriteAllText("boot-missing-script-native-proof.json", JsonConvert.SerializeObject(new
                {
                    status = "pass", editor = Application.unityVersion, original, candidate,
                    nativeRemoved = 2, warningCapture = "Native deserialization occurs before managed log callbacks; log markers are authoritative", fullBootStartupValidation = "root Main run required",
                    actualBootInitializationRun = false, originalSceneSaved = false, lightingRebake = "not_run"
                }, Formatting.Indented));
                SessionState.SetBool(Active, false);
                Debug.Log("[Boot Missing Script Probe] PASS: exact native components 2 -> 0; compare native deserialization warnings between log markers.");
                EditorApplication.Exit(0);
            }
        }
        catch (Exception exception)
        {
            SessionState.SetBool(Active, false);
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateOwners(Scene scene)
    {
        var owners = MissingOwners(scene);
        if (owners.Length != 2 || owners.Any(owner =>
                (owner.name != "ExternalServices" && owner.name != "!ftraceLightmaps") ||
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(owner) != 1) ||
            owners.Select(owner => owner.name).Distinct().Count() != 2)
            throw new InvalidOperationException("Exact native retired owner/count guard failed: " + JsonConvert.SerializeObject(owners.Select(owner => new { name = owner.name, missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(owner), global = GlobalObjectId.GetGlobalObjectIdSlow(owner).ToString() })));
    }

    private static GameObject[] MissingOwners(Scene scene) => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Select(transform => transform.gameObject)
        .Where(owner => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(owner) != 0).ToArray();

    private static void CountWarning(string message, string stack, LogType type)
    {
        if (type == LogType.Warning && SessionState.GetBool(Active, false) &&
            message == "The referenced script (Unknown) on this Behaviour is missing!")
            SessionState.SetInt(Warnings, SessionState.GetInt(Warnings, 0) + 1);
    }

    private sealed class Case
    {
        public string stage { get; set; }
        public int warnings { get; set; }
        public int missingComponents { get; set; }
    }
}
