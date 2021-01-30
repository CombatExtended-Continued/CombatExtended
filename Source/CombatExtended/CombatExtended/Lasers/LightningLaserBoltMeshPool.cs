using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CombatExtended.Lasers
{
    // Token: 0x02000459 RID: 1113
    public static class LightningLaserBoltMeshPool
    {
        // Token: 0x170002A6 RID: 678
        // (get) Token: 0x0600137B RID: 4987 RVA: 0x000954FC File Offset: 0x000938FC
        public static Mesh RandomBoltMesh
        {
            get
            {
                if (LightningLaserBoltMeshPool.boltMeshes.Count < 20)
                {
                    Mesh mesh = LightningLaserBoltMeshMaker.NewBoltMesh();
                    LightningLaserBoltMeshPool.boltMeshes.Add(mesh);
                    return mesh;
                }
                return LightningLaserBoltMeshPool.boltMeshes.RandomElement<Mesh>();
            }
        }

        // Token: 0x04000BEA RID: 3050
        private static List<Mesh> boltMeshes = new List<Mesh>();

        // Token: 0x04000BEB RID: 3051
        private const int NumBoltMeshesMax = 20;
    }
}
