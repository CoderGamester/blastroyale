using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Quantum;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirstLight.Modernization.NavMeshMigration
{
	public static class Q3MazeMapEntityImport
	{
		public static void ApplyPreparedExport()
		{
			NativeBinaryMigrationScope.RequireIsolatedProject();
			var input=Environment.GetEnvironmentVariable("BLAST_NATIVE_MAZE_MAP_EXPORT_FILE");
			var evidence=Environment.GetEnvironmentVariable("BLAST_NAVMESH_EVIDENCE_DIR");
			if(string.IsNullOrEmpty(input)||string.IsNullOrEmpty(evidence))throw new InvalidOperationException("Set reviewed native Maze prototype export/evidence paths.");
			AssetObject[] imported;
			using(var stream=File.OpenRead(input))imported=new QuantumUnityJsonSerializer().DeserializeAssets(stream);
			try
			{
				var source=imported.OfType<Map>().Single();
				var target=AssetDatabase.LoadAssetAtPath<Map>("Assets/AddressableResources/Maps/MazeMayhem/MazeMayhem.asset");
				if(target==null||source.Guid!=target.Guid||source.Path!=target.Path||source.MapEntities?.Length!=708)
					throw new InvalidDataException("The reviewed actual708-prototype Maze export identity differs.");
				var before=JObject.Parse(JsonUtility.ToJson(target));var oldCount=target.MapEntities?.Length??0;
				target.MapEntities=source.MapEntities;
				var after=JObject.Parse(JsonUtility.ToJson(target));
				before.Remove("MapEntities");after.Remove("MapEntities");before.Remove("references");after.Remove("references");
				if(!JToken.DeepEquals(before,after))throw new InvalidDataException("Import changed a map field outside the actual authored MapEntities payload.");
				EditorUtility.SetDirty(target);AssetDatabase.SaveAssets();
				File.WriteAllText(System.IO.Path.Combine(evidence,"q3-maze-authored-prototype-import.json"),new JObject
				{
					["Path"]=target.Path,["Guid"]=target.Guid.Value,["BeforeCount"]=oldCount,["NativeAuthoredCount"]=708,
					["EveryOtherSerializedMapFieldUnchanged"]=true,["SourceExport"]=input,
					["PrototypeComponents"]=JObject.FromObject(target.MapEntities.SelectMany(s=>s.Components).GroupBy(c=>c.GetType().FullName).ToDictionary(g=>g.Key,g=>g.Count()))
				}.ToString());
			}
			finally{foreach(var asset in imported)Object.DestroyImmediate(asset);}
		}
	}
}
