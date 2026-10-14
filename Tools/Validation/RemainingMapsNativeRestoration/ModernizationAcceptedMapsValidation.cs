using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quantum;
using Quantum.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Game-owned read-only validation. All prototypes, sorting and view resolution use the installed native SDK.
public static class ModernizationAcceptedMapsValidation
{
    private static readonly Func<Object, ulong> FileIdHint = (Func<Object, ulong>)typeof(Unsupported)
        .GetMethod("GetFileIDHint", BindingFlags.Static | BindingFlags.NonPublic).CreateDelegate(typeof(Func<Object, ulong>));
    public static void Run()
    {
        if (!Application.dataPath.Replace('\\','/').EndsWith("/.local/remaining-map-validation-project/Assets", StringComparison.Ordinal))
            throw new InvalidOperationException("Use the isolated project made by prepare_project.py.");
        var ledger = JObject.Parse(File.ReadAllText("native-map-entity-cache-ledger.json"));
        var rows = ((JArray)ledger["Entries"]).OfType<JObject>().ToArray();
        if ((int)ledger["Version"] != 1 || rows.Length != 10) throw new InvalidOperationException("Exact ten-map ledger required.");
        var allFiles = rows.SelectMany(row => new[]{(string)row["ScenePath"],(string)row["ScenePath"]+".meta",(string)row["UnityMapPath"]}).ToDictionary(path=>path,HashFile);
        var oldBake = QuantumEditorSettings.Global.AutoBuildOnSceneSave;
        QuantumEditorSettings.Global.AutoBuildOnSceneSave = QuantumMapDataBakeFlags.None;
        FPMathUtils.LoadLookupTables();
        var proof = new List<object>();
        try
        {
            foreach (var row in rows)
            {
                var path = (string)row["ScenePath"];
                if (HashFile(path)!=(string)row["SceneFileSha256"] || HashFile(path+".meta")!=(string)row["SceneMetaSha256"] || AssetDatabase.AssetPathToGUID(path)!=(string)row["OriginalSceneGuid"])
                    throw new InvalidOperationException("Exact native scene bytes/original metadata changed: "+path);
                var scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                var objects = scene.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject).ToArray();
                if (objects.Any(go=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go)!=0 || PrefabUtility.GetPrefabInstanceStatus(go)==PrefabInstanceStatus.MissingAsset))
                    throw new InvalidOperationException("Native scene still contains missing scripts/prefab connections: "+path);
                var data=objects.SelectMany(go=>go.GetComponents<QuantumMapData>()).Single();
                var map=data.GetAsset(true);var mapPath=AssetDatabase.GetAssetPath(map);
                if (mapPath!=(string)row["UnityMapPath"] || HashFile(mapPath)!=(string)row["NativeMapFileSha256"] || map.Guid.Value!=(long)row["QuantumGuid"] || AssetDatabase.AssetPathToGUID(mapPath)!=(string)row["UnityMapGuid"])
                    throw new InvalidOperationException("Exact native map identity/bytes changed: "+path);
                var json=JObject.Parse(JsonUtility.ToJson(map));var other=(JObject)json.DeepClone();other.Remove("MapEntities");other.Remove("references");
                if (HashToken(json["MapEntities"])!=(string)row["NativeMapEntitiesSha256"] || HashToken(json["references"])!=(string)row["NativeManagedReferencesSha256"] || HashToken(other)!=(string)row["NativeOtherMapFieldsSha256"])
                    throw new InvalidOperationException("Native typed cache or another map field changed: "+path);
                var prototypes=QuantumMapDataBaker.FindLocalObjects<QuantumEntityPrototype>(scene).ToArray();
                // Invoke the real private SDK sorting helper; no copied vendor sorting implementation.
                typeof(QuantumMapDataBaker).GetMethod("SortBySiblingIndex",BindingFlags.Static|BindingFlags.NonPublic)
                    .MakeGenericMethod(typeof(QuantumEntityPrototype)).Invoke(null,new object[]{prototypes});
                if (prototypes.Length!=(int)row["PrototypeCount"] || map.MapEntities.Length!=prototypes.Length || data.MapEntityReferences.Count!=prototypes.Length)
                    throw new InvalidOperationException("Native prototype/source/reference slot count differs: "+path);
                var buffer=new List<ComponentPrototype>();var selfViews=0;var assetViews=0;var pureSimulation=0;
                for(var i=0;i<prototypes.Length;i++)
                {
                    buffer.Clear();prototypes[i].SerializeImplicitComponents(buffer,out var selfView);
                    if (selfView!=data.MapEntityReferences[i]) throw new InvalidOperationException("Native scene view differs from the genuine SDK's source selfView at slot "+i+": "+path);
                    if(selfView!=null)selfViews++;
                    else if(buffer.Any(component=>component is Quantum.Prototypes.ViewPrototype))assetViews++;
                    else pureSimulation++;
                }
                var refs=data.MapEntityReferences.Select(view=>view==null?"null":NativeReference(view)).ToArray();
                if(selfViews!=(int)row["NonNullSceneReferences"] || HashText(JsonConvert.SerializeObject(refs))!=(string)row["NativeSceneReferencesSha256"])
                    throw new InvalidOperationException("Native non-null view count/exact source view identities changed: "+path);
                var visuals=objects.Where(go=>go.name=="__Modernization_Visual_Placeholder_Unit_Box__").ToArray();
                if(visuals.Any(go=>go.GetComponent<Collider>()!=null || go.GetComponent<MeshFilter>()?.sharedMesh==null || go.GetComponent<MeshRenderer>()?.sharedMaterial==null))
                    throw new InvalidOperationException("Marked native visual placeholders must have real geometry/material and no added collider: "+path);
                proof.Add(new{Scene=path,Map=mapPath,Prototypes=prototypes.Length,SceneReferenceSlots=refs.Length,NativeSelfViews=selfViews,NativeAssetViewPrototypes=assetViews,PureSimulationPrototypes=pureSimulation,MarkedVisualUnitBoxes=visuals.Length,EverySceneReferenceEqualsActualSdkSelfView=true,OriginalMetadataAndAllMapFieldsMatchLedger=true});
                Debug.Log("[Accepted Native Maps] "+Path.GetFileNameWithoutExtension(path)+" prototypes="+prototypes.Length+" selfViews="+selfViews+" assetViews="+assetViews+" pureSimulation="+pureSimulation);
            }
            foreach(var pair in allFiles)if(HashFile(pair.Key)!=pair.Value)throw new InvalidOperationException("Read-only validation changed a scene/meta/map file: "+pair.Key);
            File.WriteAllText("accepted-native-maps-validation.json",JsonConvert.SerializeObject(proof,Formatting.Indented));
            Debug.Log("[Accepted Native Maps] all10 native maps/source view slots verified; zero map/scene/meta writes");
        }
        finally{QuantumEditorSettings.Global.AutoBuildOnSceneSave=oldBake;}
    }
    private static string NativeReference(Object obj)
    {
        var id=FileIdHint(obj);
        return id==0?GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString():"native-scene-file-id:"+id;
    }
    private static JToken Canonical(JToken token)=>token is JObject obj?new JObject(obj.Properties().OrderBy(p=>p.Name,StringComparer.Ordinal).Select(p=>new JProperty(p.Name,Canonical(p.Value)))):token is JArray arr?new JArray(arr.Select(Canonical)):token.DeepClone();
    private static string HashToken(JToken token)=>HashText(Canonical(token).ToString(Formatting.None));
    private static string HashText(string value){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
    private static string HashFile(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
}
