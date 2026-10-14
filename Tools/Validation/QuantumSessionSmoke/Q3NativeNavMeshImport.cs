using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Photon.Deterministic;
using Quantum;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirstLight.Modernization.NavMeshMigration
{
	public static class Q3NativeNavMeshImport
	{
		public static void ApplyPreparedExport()
		{
			var source = Environment.GetEnvironmentVariable("BLAST_Q2_NAVMESH_EXPORT_DIR");
			var evidence = Environment.GetEnvironmentVariable("BLAST_NAVMESH_EVIDENCE_DIR");
			if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(evidence))
				throw new InvalidOperationException("Set BLAST_Q2_NAVMESH_EXPORT_DIR and BLAST_NAVMESH_EVIDENCE_DIR to reviewed local directories.");
			ApplyNativeExports(source, evidence);
		}

		private static string Key(long[] v) => v[0] + ":" + v[1] + ":" + v[2];
		private static FPVector3 Vector(long[] v)
		{
			if (v == null || v.Length != 3) throw new InvalidDataException("Expected three native FP raw coordinates.");
			foreach (var value in v)
				if (value < int.MinValue || value > int.MaxValue) throw new InvalidDataException("Q3 navmesh raw coordinate cannot represent the original value exactly: " + value);
			return new FPVector3(FP.FromRaw(v[0]), FP.FromRaw(v[1]), FP.FromRaw(v[2]));
		}
		private static NavMeshRegionMask Mask(ulong value) => new NavMeshRegionMask(value, 0, 0, 0, 0, 0, 0, 0);

		public static void Populate(NavMesh map, NavMeshData data)
		{
			if (data.Version != 1 || map.Guid.Value != data.Guid || map.DataAsset.Id.Value != data.DataGuid || map.Path != data.Path)
				throw new InvalidDataException("Native navmesh identity/transport schema mismatch: " + data.Path);
			// Q2 uses one 64-bit region word; two Q3 32-bit serialized blocks preserve all original bits.
			map.RegionMaskBlockCount = 2;
			map.SerializeType = NavMeshSerializeType.FullWithMetaData;
			map.Vertices = data.Vertices?.Select(v=>new NavMeshVertex {Point=Vector(v.Point),Borders=v.Borders}).ToArray();
			var indices = new Dictionary<string, int>();
			for (var i = 0; i < (data.Vertices?.Length ?? 0); i++)
				if (!indices.ContainsKey(Key(data.Vertices[i].Point))) indices.Add(Key(data.Vertices[i].Point), i);
			int BorderVertex(long[] point)
			{
				if (!indices.TryGetValue(Key(point), out var index)) throw new InvalidDataException("Q2 border endpoint has no exact native vertex: " + data.Path);
				return index;
			}
			map.Triangles = data.Triangles?.Select(t=>new NavMeshTriangle
			{
				Vertex0=t.Vertex0,Vertex1=t.Vertex1,Vertex2=t.Vertex2,
				Neighbors=t.Neighbors?.Select(n=>new NavMeshPortal {LeftVertex=n[0],RightVertex=n[1],Neighbor=n[2]}).ToArray(),
				Links=t.Links,Borders=t.Borders,Regions=Mask(t.Regions),Cost=FP.FromRaw(t.Cost),Area=0,
				Center=Vector(t.Center),Normal=Vector(t.Normal)
			}).ToArray();
			map.Borders = data.Borders?.Select(b=>new NavMeshBorder {V0=BorderVertex(b.V0),V1=BorderVertex(b.V1),Normal=Vector(b.Normal),Regions=Mask(b.Regions),RegionsOffmesh=Mask(b.RegionsOffmesh)}).ToArray();
			map.Links = data.Links?.Select(l=>new NavMeshLink {Start=Vector(l.Start),End=Vector(l.End),Triangle=l.Triangle,CostOverride=FP.FromRaw(l.CostOverride),Region=Mask(l.Region),Name=l.Name}).ToArray();
			map.TrianglesGrid=data.TrianglesGrid?.Select(n=>new NavMeshTriangleNode {Triangles=n}).ToArray();
			map.BorderGrid=data.BorderGrid?.Select(n=>new NavMeshBorderNode {Borders=n}).ToArray();
			map.TrianglesCenterGrid=data.TrianglesCenterGrid?.Select(n=>new NavMeshTriangleCenterGridNode {Triangles=n}).ToArray();
		}

		private static void Require(bool condition, string field, NavMeshData data)
		{
			if (!condition) throw new InvalidDataException("Native Q3 roundtrip changed " + field + " for " + data.Path);
		}
		private static bool Same(int[] a, int[] b) => a == null ? b == null : b != null && a.SequenceEqual(b);
		private static bool Same(FPVector3 v, long[] d) => v.X.RawValue==d[0] && v.Y.RawValue==d[1] && v.Z.RawValue==d[2];
		private static bool Same(NavMeshRegionMask m, ulong value)
		{
			for(var bit=0;bit<64;bit++) if(m.IsRegionEnabled(bit)!=((value & (1UL<<bit))!=0)) return false;
			for(var bit=64;bit<NavMeshRegionMask.MAX_REGIONS;bit++) if(m.IsRegionEnabled(bit)) return false;
			return true;
		}
		public static void VerifyRoundtrip(NavMesh map, NavMeshData data)
		{
			Require(map.Vertices?.Length==data.Vertices?.Length,"vertex count",data);
			for(var i=0;i<(data.Vertices?.Length??0);i++) Require(Same(map.Vertices[i].Point,data.Vertices[i].Point)&&Same(map.Vertices[i].Borders,data.Vertices[i].Borders),"vertex "+i,data);
			Require(map.Triangles?.Length==data.Triangles?.Length,"triangle count",data);
			for(var i=0;i<(data.Triangles?.Length??0);i++)
			{
				var a=map.Triangles[i];var b=data.Triangles[i];
				Require(a.Vertex0==b.Vertex0&&a.Vertex1==b.Vertex1&&a.Vertex2==b.Vertex2&&a.Area==0&&a.Cost.RawValue==b.Cost&&Same(a.Regions,b.Regions)&&Same(a.Center,b.Center)&&Same(a.Normal,b.Normal)&&Same(a.Links,b.Links)&&Same(a.Borders,b.Borders),"triangle "+i,data);
				Require(a.Neighbors?.Length==b.Neighbors?.Length,"portal count "+i,data);
				for(var n=0;n<(b.Neighbors?.Length??0);n++) Require(a.Neighbors[n].LeftVertex==b.Neighbors[n][0]&&a.Neighbors[n].RightVertex==b.Neighbors[n][1]&&a.Neighbors[n].Neighbor==b.Neighbors[n][2],"portal "+i+":"+n,data);
			}
			Require(map.Borders?.Length==data.Borders?.Length,"border count",data);
			for(var i=0;i<(data.Borders?.Length??0);i++)
			{
				var a=map.Borders[i];var b=data.Borders[i];
				// Q3 derives border Normal from exact endpoint geometry. It has no serialized Q2 normal field.
				Require(Same(map.Vertices[a.V0].Point,b.V0)&&Same(map.Vertices[a.V1].Point,b.V1)&&Same(a.Regions,b.Regions)&&Same(a.RegionsOffmesh,b.RegionsOffmesh),"border "+i,data);
			}
			Require(map.Links?.Length==data.Links?.Length,"link count",data);
			for(var i=0;i<(data.Links?.Length??0);i++)
			{
				var a=map.Links[i];var b=data.Links[i];Require(Same(a.Start,b.Start)&&Same(a.End,b.End)&&a.Triangle==b.Triangle&&a.CostOverride.RawValue==b.CostOverride&&a.Name==b.Name&&Same(a.Region,b.Region),"link "+i,data);
			}
			Require(map.TrianglesGrid?.Length==data.TrianglesGrid?.Length&&map.BorderGrid?.Length==data.BorderGrid?.Length&&map.TrianglesCenterGrid?.Length==data.TrianglesCenterGrid?.Length,"grid counts",data);
			for(var i=0;i<(data.TrianglesGrid?.Length??0);i++)Require(Same(map.TrianglesGrid[i].Triangles,data.TrianglesGrid[i]),"triangle grid "+i,data);
			for(var i=0;i<(data.BorderGrid?.Length??0);i++)Require(Same(map.BorderGrid[i].Borders,data.BorderGrid[i]),"border grid "+i,data);
			for(var i=0;i<(data.TrianglesCenterGrid?.Length??0);i++)Require(Same(map.TrianglesCenterGrid[i].Triangles,data.TrianglesCenterGrid[i]),"center grid "+i,data);
		}

		public static void ApplyNativeExports(string exportDirectory, string evidenceDirectory)
		{
			NativeBinaryMigrationScope.RequireIsolatedProject();
			FPMathUtils.LoadLookupTables();
			var evidence=new List<object>();
			foreach(var file in Directory.GetFiles(exportDirectory,"*.json"))
			{
				var data=JsonConvert.DeserializeObject<NavMeshData>(File.ReadAllText(file));
				var asset=AssetDatabase.LoadAssetAtPath<NavMesh>("Assets/"+data.Path+".asset");
				if(asset==null)throw new InvalidDataException("Missing real navmesh asset "+data.Path);
				var temporary=Object.Instantiate(asset);
				var roundtrip=Object.Instantiate(asset);
				try
				{
					Populate(temporary,data);
					byte[] bytes;
					// Use the actual SDK baker's bounded serialization capacity; ByteStream does not grow.
					using(var stream=new ByteStream(QuantumMapDataBaker.NavMeshSerializationBufferSize)) {temporary.Serialize(stream,true);bytes=stream.ToArray();}
					roundtrip.SerializeType=temporary.SerializeType;roundtrip.RegionMaskBlockCount=temporary.RegionMaskBlockCount;
					using(var stream=new ByteStream(bytes))
					{
						roundtrip.Serialize(stream,false);
						Require(stream.Position==bytes.Length,"complete binary consumption",data);
					}
					VerifyRoundtrip(roundtrip,data);
					var binary=QuantumUnityDB.Global.GetAssetEditorInstance<BinaryData>(asset.DataAsset);
					if(binary==null)throw new InvalidDataException("Missing real binary asset "+data.DataGuid);
					var oldByteCount=binary.Data.Length;
					binary.SetData(bytes,false);
					asset.SerializeType=temporary.SerializeType;asset.RegionMaskBlockCount=temporary.RegionMaskBlockCount;
					EditorUtility.SetDirty(binary);EditorUtility.SetDirty(asset);
					evidence.Add(new{data.Path,data.Guid,data.DataGuid,OriginalBytes=oldByteCount,NativeBytes=bytes.Length,Vertices=data.Vertices?.Length,Triangles=data.Triangles?.Length,Borders=data.Borders?.Length,RoundtripPreservedAllAuthoredGeometryTopologyRegionsCostsLinksAndGridNodes=true,NativeAreaDefault=0,NativeBorderNormalDerivedFromExactEndpoints=true});
				}
				finally {Object.DestroyImmediate(temporary);Object.DestroyImmediate(roundtrip);}
			}
			AssetDatabase.SaveAssets();
			Directory.CreateDirectory(evidenceDirectory);
			File.WriteAllText(System.IO.Path.Combine(evidenceDirectory,"q3-native-navmesh-conversion.json"),JsonConvert.SerializeObject(evidence,Formatting.Indented));
		}
	}
}
