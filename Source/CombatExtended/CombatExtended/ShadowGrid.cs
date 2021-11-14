using System;
using System.Linq;
using System.Numerics;
using Verse;

namespace CombatExtended
{
    public class ShadowGrid : MapComponent
    {
        private readonly short[][] sigGrid;
        private readonly float[][] visGrid;

        private short signature = 1;
        private int lastTick;

        private readonly int sizeX;
        private readonly int sizeZ;

        public ShadowGrid(Map map) : base(map)
        {
            this.sigGrid = new short[map.Size.x][];
            this.visGrid = new float[map.Size.x][];            
            this.sizeX = map.Size.x;
            this.sizeZ = map.Size.z;
            for (int i = 0; i < map.Size.x; i++)
            {
                this.sigGrid[i] = new short[map.Size.z];
                this.visGrid[i] = new float[map.Size.z];
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();                     
        }

        public void Reset()
        {
            this.signature++;
            this.lastTick = GenTicks.TicksGame;
        }        

        public void Set(IntVec3 cell, float visibility) => Set(cell.x, cell.z, visibility);

        public void Set(int i, int j, float visibility)
        {
            if (i < 0 || j < 0 || i >= this.sizeX || j >= this.sizeZ)
            {
                return;
            }       
            this.sigGrid[i][j] = signature;
            this.visGrid[i][j] = visibility;
        }

        public bool TryGet(IntVec3 cell, out float visibility) => TryGet(cell.x, cell.z, out visibility);

        public bool TryGet(int i, int j, out float visibility)
        {
            if(i < 0 || j < 0 || i >= this.sizeX || j >= this.sizeZ)
            {
                visibility = 0f;
                return false;
            }
            if (this.sigGrid[i][j] != signature)
            {
                visibility = 0f;
                return false;
            }
            visibility = this.visGrid[i][j];
            return true;
        }
    }
}

