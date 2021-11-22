using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
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
            //if (!Find.Selector.SelectedPawns.NullOrEmpty())
            //{
            //    Reset();
            //    var pawn = Find.Selector.SelectedPawns.First();
            //    var direction = UI.MouseCell().ToVector3() - pawn.Position.ToVector3();
            //    ShadowCastingUtility.CastVisibility(map, pawn.Position, direction, (cell) => Set(cell, 0.5f), direction.magnitude, 10, out _);
            //}
        }

        public void Reset()
        {
            this.signature++;
            this.lastTick = GenTicks.TicksGame;
        }
        
        public void Set(int i, int j, float visibility) => Set(new IntVec3(i, 0, j), visibility);
        public void Set(IntVec3 cell, float visibility)
        {
            if (!cell.InBounds(map))
            {
                return;
            }
            this.sigGrid[cell.x][cell.z] = signature;
            this.visGrid[cell.x][cell.z] = visibility;
            //if (DebugSettings.godMode)
            //{
            //    map.debugDrawer.FlashCell(cell, Mathf.Clamp(visibility / 2f, 0.01f, 0.5f), visibility.ToString("#.##"), 2);
            //}
        }

        public bool TryGet(int i, int j, out float visibility) => TryGet(new IntVec3(i, 0, j), out visibility);
        public bool TryGet(IntVec3 cell, out float visibility)
        {            
            if (!cell.InBounds(map) || this.sigGrid[cell.x][cell.z] != signature)
            {
                visibility = 0f;
                return false;
            }
            visibility = this.visGrid[cell.x][cell.z];
            return true;
        }                
    }
}

