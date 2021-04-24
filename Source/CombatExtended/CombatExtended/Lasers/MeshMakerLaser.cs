using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CombatExtended.Lasers
{
    public static class MeshMakerLaser
    {
        static int textureSeamPrecision = 256;
        static int geometrySeamPrecision = 512;
        static Dictionary<int, Mesh> cachedMeshes = new Dictionary<int, Mesh>();

        /// <summary>
        /// Creates a mesh for a laser beam consisting of three parts: top cap, middle, bottom cap.
        /// Both caps are separated from middle part by a speam, whose position is specified in function's
        /// arguments.
        /// </summary>
        /// <param name="st">position of seam in texture (from 0 to 0.5 on y axis)</param>
        /// <param name="sv">position of seam in vertex geometry (from 0 to 0.5 on z axis)</param>
        /// <returns>the mesh</returns>
        public static Mesh Mesh(float st, float sv)
        {
            if (st < 0) st = 0;
            if (st > 0.5f) st = 0.5f;
            if (sv < 0) sv = 0;
            if (sv > 0.5f) sv = 0.5f;

            int textureSeamIndex = (int)(st / 0.5f * textureSeamPrecision);
            int geometrySeamIndex = (int)(sv / 0.5f * geometrySeamPrecision);
            int index = geometrySeamIndex + (textureSeamPrecision + 1) * geometrySeamPrecision;

            Mesh mesh;
            if (cachedMeshes.TryGetValue(index, out mesh))
            {
                return mesh;
            }

            st = 0.5f * textureSeamIndex / textureSeamPrecision;
            sv = 0.5f * geometrySeamIndex / geometrySeamPrecision;

            float rt = 1.0f - st;
            float vsf = 0.5f - sv;

            Vector3[] vertices = {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, -vsf),
                new Vector3(0.5f, 0f, -vsf),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, vsf),
                new Vector3(0.5f, 0f, vsf),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f),
            };

            Vector2[] uv = {
                new Vector2(0f, 0f),
                new Vector2(0f, st),
                new Vector2(1f, st),
                new Vector2(1f, 0f),
                new Vector2(0f, rt),
                new Vector2(1f, rt),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
            };

            int[] triangles = {
                0, 1, 2,
                0, 2, 3,
                1, 4, 5,
                1, 5, 2,
                4, 6, 7,
                4, 7, 5,
            };

            mesh = new Mesh();
            mesh.name = "NewLaserMesh()";
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            cachedMeshes[index] = mesh;
            return mesh;
        }
    }
}
