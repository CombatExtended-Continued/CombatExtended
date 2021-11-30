using System;
using UnityEngine;
using Verse;

namespace CombatExtended
{
	[StaticConstructorOnStartup]
    public static class CE_MeshMaker
    {
		public const float DEPTH_TOP = -0.000f;
		public const float DEPTH_MID = -0.015f;
		public const float DEPTH_BOT = -0.030f;

		public readonly static Mesh plane10Top;
		public readonly static Mesh plane10Mid;
		public readonly static Mesh plane10Bot;

		public readonly static Mesh plane10FlipTop;
		public readonly static Mesh plane10FlipMid;
		public readonly static Mesh plane10FlipBot;

		static CE_MeshMaker()
        {
            plane10Top = NewPlaneMesh(Vector2.zero, Vector2.one,  depth: 0.00f);
            plane10Mid = NewPlaneMesh(Vector2.zero, Vector2.one, depth: -0.01f);
            plane10Bot = NewPlaneMesh(Vector2.zero, Vector2.one, depth: -0.02f);

            plane10FlipTop = NewPlaneMesh(Vector2.zero, Vector2.one, depth: -0.00f, flipped: true);
            plane10FlipMid = NewPlaneMesh(Vector2.zero, Vector2.one, depth: -0.01f, flipped: true);
            plane10FlipBot = NewPlaneMesh(Vector2.zero, Vector2.one, depth: -0.02f, flipped: true);
        }

        public static Mesh NewPlaneMesh(Vector2 offset, Vector2 scale, float depth = 0, bool flipped = false)
		{
			Vector3[] vertices = new Vector3[4];
			Vector2[] uv = new Vector2[4];
			int[] indexes = new int[6];

			// This is the default form 
			// vertices[0] = new Vector3(-0.5f * scale.x, depth, -0.5f * scale.y);
			// vertices[1] = new Vector3(-0.5f * scale.x, depth, 0.5f * scale.y);
			// vertices[2] = new Vector3(0.5f * scale.x, depth, 0.5f * scale.y);
			// vertices[3] = new Vector3(0.5f * scale.x, depth, -0.5f * scale.y);

			if (flipped)
			{
				vertices[0] = new Vector3(-0.5f + offset.x, depth, -0.5f + offset.y);
				vertices[1] = vertices[0] + new Vector3(0f, 0f, scale.y);
				vertices[2] = vertices[0] + new Vector3(scale.x, 0f, scale.y);
				vertices[3] = vertices[0] + new Vector3(scale.x, 0f, 0f);
            }
            else
            {
				vertices[0] = new Vector3((0.5f - offset.x - scale.x), depth, -0.5f + offset.y);
				vertices[1] = vertices[0] + new Vector3(0f, 0f, scale.y);
				vertices[2] = vertices[0] + new Vector3(scale.x, 0f, scale.y);
				vertices[3] = vertices[0] + new Vector3(scale.x, 0f, 0f);
			}

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
