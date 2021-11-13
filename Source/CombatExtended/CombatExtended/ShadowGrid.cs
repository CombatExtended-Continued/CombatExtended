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
            //
            //if (!Find.Selector.SelectedPawns.NullOrEmpty())
            //{
            //    var pawn = Find.Selector.SelectedPawns.First();
            //    var target = UI.MouseMapPosition().ToIntVec3();
            //    if (target.InBounds(map))
            //    {
            //        map.CastWeighted(pawn.Position, target, 20);
            //    }
            //}
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
            //if (DebugSettings.godMode && (this.lastTick = GenTicks.TicksGame) % 10 == 0)
            //{
            //    map.debugDrawer.FlashCell(new IntVec3(i, 0, j), visibility, $"{visibility}", duration: 10);
            //}            
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

