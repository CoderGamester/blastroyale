using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photon.Deterministic;
using Quantum;
using UnityEditor;
using Object = UnityEngine.Object;

namespace FirstLight.Modernization.NavMeshMigration
{
	public static unsafe class Q3NativeTriangleImport
	{
		private static FPVector3 Vector(JToken v) => new FPVector3(FP.FromRaw((long)v["X"]["RawValue"]),FP.FromRaw((long)v["Y"]["RawValue"]),FP.FromRaw((long)v["Z"]["RawValue"]));
		private static string Raw(FPVector3 v) => $"({v.X.RawValue},{v.Y.RawValue},{v.Z.RawValue})";
		private static void Require(bool value, string message) {if(!value)throw new InvalidDataException(message);}
		public static void ApplyPreparedExport()
		{
			NativeBinaryMigrationScope.RequireIsolatedProject();
			var source=Environment.GetEnvironmentVariable("BLAST_Q2_TRIANGLE_EXPORT_FILE");
			var evidence=Environment.GetEnvironmentVariable("BLAST_NAVMESH_EVIDENCE_DIR");
			if(string.IsNullOrEmpty(source)||string.IsNullOrEmpty(evidence))throw new InvalidOperationException("Set reviewed triangle export/evidence paths.");
			FPMathUtils.LoadLookupTables();
			var results=new List<object>();
			foreach(var row in JArray.Parse(File.ReadAllText(source)))
			{
				var path=(string)row["Path"];var guid=(long)row["Guid"];var binaryGuid=(long)row["BinaryGuid"];
				var asset=AssetDatabase.LoadAssetAtPath<Map>("Assets/"+path+".asset");
				Require(asset!=null&&asset.Guid.Value==guid&&asset.StaticColliders3DTrianglesData.Id.Value==binaryGuid,"Native triangle map identity mismatch "+path);
				var temporary=Object.Instantiate(asset);var readback=Object.Instantiate(asset);var allocator=new QuantumUnityNativeAllocator();
				try
				{
					temporary.CollidersManagedTriangles=new SortedDictionary<int,MeshTriangleVerticesCcw>();
					foreach(var group in row["Groups"])
					{
						var vertices=new List<FPVector3>();var indices=new List<int>();
						foreach(var triangle in group["Triangles"])
							// SDK Create converts clockwise mesh input to CCW. Q2 decoded data is already CCW.
							foreach(var corner in new[]{"C","B","A"}){indices.Add(vertices.Count);vertices.Add(Vector(triangle[corner]));}
						var mesh=MeshTriangleVerticesCcw.Create(vertices,indices,null);
						mesh.MeshColliderIndex=(int)group["ColliderIndex"];
						temporary.CollidersManagedTriangles.Add(mesh.MeshColliderIndex,mesh);
					}
					byte[] bytes;
					using(var stream=new ByteStream(temporary.GetStaticColliderTrianglesSerializedSize(true)))
					{temporary.SerializeStaticColliderTriangles(stream,null,true);bytes=stream.ToArray();}
					using(var stream=new ByteStream(bytes))
					{readback.SerializeStaticColliderTriangles(stream,allocator,false);Require(stream.Position==bytes.Length,"Native triangle readback did not consume all bytes "+path);}
					Require(readback.CollidersRuntimeTriangles.Count==row["Groups"].Count(),"Native triangle group count changed "+path);
					var total=0;
					foreach(var group in row["Groups"])
					{
						var index=(int)group["ColliderIndex"];
						Require(readback.CollidersRuntimeTriangles.TryGetValue(index,out var mesh),"Missing native triangle collider "+index);
						var triangles=mesh.GetTriangles(out var count);Require(count==group["Triangles"].Count(),"Native triangle count changed "+path);
						for(var i=0;i<count;i++)
						{
							var original=group["Triangles"][i];
							Require(triangles[i].A==Vector(original["A"])&&triangles[i].B==Vector(original["B"])&&triangles[i].C==Vector(original["C"])&&triangles[i].StaticDataIndex==index,
								"Native triangle geometry/winding/collider index changed "+path+":"+i+" expected="+Raw(Vector(original["A"]))+Raw(Vector(original["B"]))+Raw(Vector(original["C"]))+" actual="+Raw(triangles[i].A)+Raw(triangles[i].B)+Raw(triangles[i].C)+" collider="+triangles[i].StaticDataIndex);
						}
						total+=count;
					}
					var binary=QuantumUnityDB.Global.GetAssetEditorInstance<BinaryData>(asset.StaticColliders3DTrianglesData);
					Require(binary!=null,"Missing triangle binary "+path);
					binary.SetData(bytes,binary.IsCompressed);
					asset.SerializedTriangleDataUncompressedSize=bytes.Length;
					EditorUtility.SetDirty(binary);EditorUtility.SetDirty(asset);
					results.Add(new{Path=path,Guid=guid,BinaryGuid=binaryGuid,OriginalBytes=(int)row["OriginalByteCount"],NativeBytes=bytes.Length,Groups=row["Groups"].Count(),Triangles=total,NativeReadbackPreservedEveryRawVertexWindingAndColliderIndex=true});
				}
				finally {readback.Disposed(null,allocator);allocator.Dispose();Object.DestroyImmediate(temporary);Object.DestroyImmediate(readback);}
			}
			AssetDatabase.SaveAssets();Directory.CreateDirectory(evidence);
			File.WriteAllText(System.IO.Path.Combine(evidence,"q3-native-triangle-conversion.json"),JsonConvert.SerializeObject(results,Formatting.Indented));
		}
	}
}
