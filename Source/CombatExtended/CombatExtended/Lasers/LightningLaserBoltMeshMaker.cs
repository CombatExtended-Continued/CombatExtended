using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace CombatExtended.Lasers
{
    // Token: 0x0200045A RID: 1114
    public static class LightningLaserBoltMeshMaker
    {
        // Token: 0x0600137D RID: 4989 RVA: 0x00095543 File Offset: 0x00093943
        public static Mesh NewBoltMesh(float xMin = -50f, float xMax = 50f, float z = 200f, float str = 3f)
        {
            LightningLaserBoltMeshMaker.lightningTop = new Vector2(Rand.Range(xMin, xMax), z);
            LightningLaserBoltMeshMaker.MakeVerticesBase();
            LightningLaserBoltMeshMaker.PeturbVerticesRandomly(str);
            LightningLaserBoltMeshMaker.DoubleVertices();
            return LightningLaserBoltMeshMaker.MeshFromVerts();
        }

        // Token: 0x0600137D RID: 4989 RVA: 0x00095543 File Offset: 0x00093943
        public static Mesh NewBoltMesh(Vector2 vector, float str = 3f)
        {
            LightningLaserBoltMeshMaker.lightningTop = vector;
            LightningLaserBoltMeshMaker.MakeVerticesBase();
            LightningLaserBoltMeshMaker.PeturbVerticesRandomly(str);
            LightningLaserBoltMeshMaker.DoubleVertices();
            return LightningLaserBoltMeshMaker.MeshFromVerts();
        }

        // Token: 0x0600137E RID: 4990 RVA: 0x00095578 File Offset: 0x00093978
        private static void MakeVerticesBase()
        {
            int num = (int)Math.Ceiling((double)((Vector2.zero - LightningLaserBoltMeshMaker.lightningTop).magnitude / 0.25f));
            Vector2 b = LightningLaserBoltMeshMaker.lightningTop / (float)num;
            LightningLaserBoltMeshMaker.verts2D = new List<Vector2>();
            Vector2 vector = Vector2.zero;
            for (int i = 0; i < num; i++)
            {
                LightningLaserBoltMeshMaker.verts2D.Add(vector);
                vector += b;
            }
        }

        // Token: 0x0600137F RID: 4991 RVA: 0x000955F0 File Offset: 0x000939F0
        private static void PeturbVerticesRandomly(float str)
        {
            float dmod = 1f;
            Perlin perlin = new Perlin(0.0070000002160668373, 2.0, 0.5, 6, Rand.Range(0, int.MaxValue), QualityMode.High);
            List<Vector2> list = LightningLaserBoltMeshMaker.verts2D.ListFullCopy<Vector2>();
            LightningLaserBoltMeshMaker.verts2D.Clear();
            int threshold = (list.Count / 4) * 3;
            for (int i = 0; i < list.Count; i++)
            {
                float d = str * (float)perlin.GetValue((double)i, 0.0, 0.0);
                if (i > threshold)
                {
                    dmod = 1 - (1 * ((float)(i - threshold) / (list.Count - threshold)));
                    //    Log.Message(string.Format("dmod now: {0}", dmod));
                }
                d = d * dmod;
                //    Log.Message(string.Format("d: {0}", d));
                Vector2 item = list[i] + d * Vector2.right;
                LightningLaserBoltMeshMaker.verts2D.Add(item);
            }
        }

        // Token: 0x06001380 RID: 4992 RVA: 0x000956A0 File Offset: 0x00093AA0
        private static void DoubleVertices()
        {
            List<Vector2> list = LightningLaserBoltMeshMaker.verts2D.ListFullCopy<Vector2>();
            Vector3 vector = default(Vector3);
            Vector2 a = default(Vector2);
            LightningLaserBoltMeshMaker.verts2D.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                if (i <= list.Count - 2)
                {
                    vector = Quaternion.AngleAxis(90f, Vector3.up) * (list[i] - list[i + 1]);
                    a = new Vector2(vector.y, vector.z);
                    a.Normalize();
                }
                Vector2 item = list[i] - 1f * a;
                Vector2 item2 = list[i] + 1f * a;
                LightningLaserBoltMeshMaker.verts2D.Add(item);
                LightningLaserBoltMeshMaker.verts2D.Add(item2);
            }
        }

        // Token: 0x06001381 RID: 4993 RVA: 0x0009578C File Offset: 0x00093B8C
        private static Mesh MeshFromVerts()
        {
            Vector3[] array = new Vector3[LightningLaserBoltMeshMaker.verts2D.Count];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new Vector3(LightningLaserBoltMeshMaker.verts2D[i].x, 0f, LightningLaserBoltMeshMaker.verts2D[i].y);
            }
            float num = 0f;
            Vector2[] array2 = new Vector2[LightningLaserBoltMeshMaker.verts2D.Count];
            for (int j = 0; j < LightningLaserBoltMeshMaker.verts2D.Count; j += 2)
            {
                array2[j] = new Vector2(0f, num);
                array2[j + 1] = new Vector2(1f, num);
                num += 0.04f;
            }
            int[] array3 = new int[LightningLaserBoltMeshMaker.verts2D.Count * 3];
            for (int k = 0; k < LightningLaserBoltMeshMaker.verts2D.Count - 2; k += 2)
            {
                int num2 = k * 3;
                array3[num2] = k;
                array3[num2 + 1] = k + 1;
                array3[num2 + 2] = k + 2;
                array3[num2 + 3] = k + 2;
                array3[num2 + 4] = k + 1;
                array3[num2 + 5] = k + 3;
            }
            return new Mesh
            {
                vertices = array,
                uv = array2,
                triangles = array3,
                name = "MeshFromVerts()"
            };
        }

        // Token: 0x04000BEC RID: 3052
        private static List<Vector2> verts2D;

        // Token: 0x04000BED RID: 3053
        private static Vector2 lightningTop;

        // Token: 0x04000BEE RID: 3054
        private const float LightningHeight = 200f;

        // Token: 0x04000BEF RID: 3055
        private const float LightningRootXVar = 50f;

        // Token: 0x04000BF0 RID: 3056
        private const float VertexInterval = 0.25f;

        // Token: 0x04000BF1 RID: 3057
        private const float MeshWidth = 2f;

        // Token: 0x04000BF2 RID: 3058
        private const float UVIntervalY = 0.04f;

        // Token: 0x04000BF3 RID: 3059
        private const float PerturbAmp = 12f;

        // Token: 0x04000BF4 RID: 3060
        private const float PerturbFreq = 0.007f;
    }
}
