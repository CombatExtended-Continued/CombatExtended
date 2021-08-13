using System;
using UnityEngine;
using Verse;

namespace CombatExtended
{
	[StaticConstructorOnStartup]
    public static class CE_MeshPool
    {
		public static readonly Mesh plane10Top;
		public static readonly Mesh plane10Mid;
		public static readonly Mesh plane10Bot;		

		public static readonly Mesh plane10FlipTop;
		public static readonly Mesh plane10FlipMid;
		public static readonly Mesh plane10FlipBot;

		static CE_MeshPool()
        {
			plane10Top = NewPlaneMesh(Vector2.one, depth: -0.000f);
			plane10Mid = NewPlaneMesh(Vector2.one, depth: -0.015f);
			plane10Bot = NewPlaneMesh(Vector2.one, depth: -0.03f);

			plane10FlipTop = NewPlaneMesh(Vector2.one, depth: -0.000f, flipped: true);
			plane10FlipMid = NewPlaneMesh(Vector2.one, depth: -0.015f, flipped: true);
			plane10FlipBot = NewPlaneMesh(Vector2.one, depth: -0.030f, flipped: true);
		}

		private static Mesh NewPlaneMesh(Vector2 size, float depth = 0, bool flipped = false)
		{
			Vector3[] vertices = new Vector3[4];
			Vector2[] uv = new Vector2[4];
			int[] indexes = new int[6];
			vertices[0] = new Vector3(-0.5f * size.x, depth, -0.5f * size.y);
			vertices[1] = new Vector3(-0.5f * size.x, depth, 0.5f * size.y);
			vertices[2] = new Vector3(0.5f * size.x, depth, 0.5f * size.y);
			vertices[3] = new Vector3(0.5f * size.x, depth, -0.5f * size.y);			
			if (!flipped)
			{
				uv[0] = new Vector2(0f, 0f);
				uv[1] = new Vector2(0f, 1f);
				uv[2] = new Vector2(1f, 1f);
				uv[3] = new Vector2(1f, 0f);
			}
			else
			{
				uv[0] = new Vector2(1f, 0f);
				uv[1] = new Vector2(1f, 1f);
				uv[2] = new Vector2(0f, 1f);
				uv[3] = new Vector2(0f, 0f);
			}
			indexes[0] = 0;
			indexes[1] = 1;
			indexes[2] = 2;
			indexes[3] = 0;
			indexes[4] = 2;
			indexes[5] = 3;
			Mesh mesh = new Mesh();
			mesh.name = "NewPlaneMesh()";
			mesh.vertices = vertices;
			mesh.uv = uv;
			mesh.SetTriangles(indexes, 0);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}
	}
}
