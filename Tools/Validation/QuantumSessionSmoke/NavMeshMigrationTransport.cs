using System;

namespace FirstLight.Modernization.NavMeshMigration
{
#if UNITY_EDITOR
	public static class NativeBinaryMigrationScope
	{
		public static void RequireIsolatedProject()
		{
			if (!UnityEngine.Application.dataPath.Replace('\\', '/').EndsWith("/.local/quantum3-source-project/Assets", StringComparison.Ordinal))
				throw new InvalidOperationException("This migration helper only writes the reviewed isolated Quantum3 project.");
		}
	}
#endif
	// Primitive transport between two real SDK serializers. These are not simulation replacement types.
	[Serializable] public sealed class NavMeshData
	{
		public int Version = 1;
		public long Guid, DataGuid;
		public string Path;
		public VertexData[] Vertices;
		public TriangleData[] Triangles;
		public BorderData[] Borders;
		public LinkData[] Links;
		public int[][] TrianglesGrid, BorderGrid, TrianglesCenterGrid;
	}
	[Serializable] public sealed class VertexData { public long[] Point; public int[] Borders; }
	[Serializable] public sealed class TriangleData
	{
		public int Vertex0, Vertex1, Vertex2;
		public int[][] Neighbors;
		public int[] Links, Borders;
		public ulong Regions;
		public long Cost;
		public long[] Center, Normal;
	}
	[Serializable] public sealed class BorderData
	{
		public long[] V0, V1, Normal;
		public ulong Regions, RegionsOffmesh;
	}
	[Serializable] public sealed class LinkData
	{
		public long[] Start, End;
		public int Triangle;
		public long CostOverride;
		public ulong Region;
		public string Name;
	}
}
