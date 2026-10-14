using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photon.Deterministic;
using Quantum;

namespace FirstLight.Modernization.NavMeshMigration
{
	public static class Q2NativeNavMeshExport
	{
		private static long[] Vector(FPVector3 v) => new[] {v.X.RawValue, v.Y.RawValue, v.Z.RawValue};
		public static int Main(string[] args)
		{
			var export = args[0];
			Directory.CreateDirectory(args[1]);
			var assets = Directory.GetFiles(export, "*.json").Select(p => new {Path=p, Json=JObject.Parse(File.ReadAllText(p))}).ToArray();
			var binaries = assets.Where(a => a.Json["Data"] != null).ToDictionary(a => (long)a.Json["Identifier"]["Guid"]["Value"]);
			var count = 0;
			foreach (var asset in assets.Where(a => a.Json["DataAsset"] != null && a.Json["GridSizeX"] != null))
			{
				var map = JsonConvert.DeserializeObject<NavMesh>(asset.Json.ToString());
				if (!binaries.TryGetValue(map.DataAsset.Id.Value, out var binaryFile))
				{
					Console.WriteLine("Explicit original dangling navmesh skipped: " + map.Identifier.Path + " DataAsset=" + map.DataAsset.Id.Value);
					continue;
				}
				var binary = JsonConvert.DeserializeObject<BinaryData>(binaryFile.Json.ToString());
				using (var stream = binary.CreateByteStream())
				{
					map.Serialize(stream, false); // The actual Q2.1.10 decoder, never a manual old-byte parser.
					if (stream.Position != stream.Data.Length) throw new InvalidDataException("Native Q2 decoder left unread bytes for " + map.Identifier.Path);
				}
				var data = new NavMeshData
				{
					Guid=map.Identifier.Guid.Value, DataGuid=map.DataAsset.Id.Value, Path=map.Identifier.Path,
					Vertices=map.Vertices?.Select(v=>new VertexData {Point=Vector(v.Point), Borders=v.Borders}).ToArray(),
					Triangles=map.Triangles?.Select(t=>new TriangleData
					{
						Vertex0=t.Vertex0,Vertex1=t.Vertex1,Vertex2=t.Vertex2,
						Neighbors=t.Neighbors?.Select(n=>new[]{n.LeftVertex,n.RightVertex,n.Neighbor}).ToArray(),
						Links=t.Links,Borders=t.Borders,Regions=t.Regions.Mask,Cost=t.Cost.RawValue,Center=Vector(t.Center),Normal=Vector(t.Normal)
					}).ToArray(),
					Borders=map.Borders?.Select(b=>new BorderData {V0=Vector(b.V0),V1=Vector(b.V1),Normal=Vector(b.Normal),Regions=b.Regions.Mask,RegionsOffmesh=b.RegionsOffmesh.Mask}).ToArray(),
					Links=map.Links?.Select(l=>new LinkData {Start=Vector(l.Start),End=Vector(l.End),Triangle=l.Triangle,CostOverride=l.CostOverride.RawValue,Region=l.Region.Mask,Name=l.Name}).ToArray(),
					TrianglesGrid=map.TrianglesGrid?.Select(n=>n.Triangles).ToArray(),
					BorderGrid=map.BorderGrid?.Select(n=>n.Borders).ToArray(),
					TrianglesCenterGrid=map.TrianglesCenterGrid?.Select(n=>n.Triangles).ToArray()
				};
				File.WriteAllText(System.IO.Path.Combine(args[1], data.Guid+".json"),JsonConvert.SerializeObject(data));
				Console.WriteLine(data.Path+" vertices="+data.Vertices?.Length+" triangles="+data.Triangles?.Length+" borders="+data.Borders?.Length);
				count++;
			}
			Console.WriteLine("Native Q2 navmeshes decoded and exported: " + count);
			return 0;
		}
	}
}
