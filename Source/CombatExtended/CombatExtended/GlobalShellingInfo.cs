using System;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public struct GlobalShellingInfo : IExposable
    {
        public static readonly GlobalShellingInfo Invalid;        

        public int targetTile;
        public int sourceTile;       
        public IntVec3 targetCell;
        public IntVec3 sourceMapExitCell;
        public float tilesPerTick;

        private bool _valid;        

        public bool TargetingMapCell
        {
            get => targetCell.IsValid;
        }

        public bool IsValid
        {            
            get => _valid;
        }

        public GlobalShellingInfo(int sourceTile,int targetTileIndex, float tilesPerTick ,  IntVec3? sourceMapExitCell = null, IntVec3? targetCell = null)
        {
            this.sourceTile = sourceTile;
            this.targetCell = !targetCell.HasValue ? IntVec3.Invalid : targetCell.Value;
            this.targetTile = targetTileIndex;
            this.sourceMapExitCell = sourceMapExitCell.HasValue ? sourceMapExitCell.Value : IntVec3.Invalid;
            this.tilesPerTick = tilesPerTick;
            this._valid = true;
        }

        public void ExposeData()
        {            
            Scribe_Values.Look(ref targetTile, "targetTileIndex");
            Scribe_Values.Look(ref sourceTile, "sourceTile");
            Scribe_Values.Look(ref targetCell, "targetCell", IntVec3.Invalid);
            Scribe_Values.Look(ref tilesPerTick, "tilesPerTick");
            Scribe_Values.Look(ref sourceMapExitCell, "sourceMapExitCell", IntVec3.Invalid);
            Scribe_Values.Look(ref _valid, "_valid", false);
        }
    }
}
