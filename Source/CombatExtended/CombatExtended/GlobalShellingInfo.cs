using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public struct GlobalShellingInfo : IExposable
    {
        public static readonly GlobalShellingInfo Invalid;

        private bool _valid;

        private int _shooterId;
        private int _casterId;

        private ThingDef _casterDef;
        private ThingDef _shooterDef;

        private Thing _caster;
        private Pawn _shooter;                

        public int targetTile;
        public int sourceTile;       
        public IntVec3 targetCell;
        public IntVec3 sourceMapExitCell;
        public Vector3 outboundVec;        
        public float tilesPerTick;

        public Thing Caster
        {
            get
            {
                if (_caster != null) return _caster;
                if (_casterId == 0 || _casterDef == null) return null;
                int id = _casterId;
                foreach (Map map in Find.Maps)
                {
                    if(map.listerThings.listsByDef.TryGetValue(_casterDef, out var things))
                    {
                        _caster = things.First(t => t.thingIDNumber == id);
                        if(_caster != null)
                        {
                            return _caster;
                        }
                    }
                }
                return null;
            }
            set
            {
                _caster = value;
                _casterId = _caster?.thingIDNumber ?? 0;
                _casterDef = _caster?.def ?? null;
            }
        }

        public Pawn Shooter
        {
            get
            {
                if (_shooter != null) return _shooter;
                if (_shooterId == 0 || _shooterDef == null) return null;
                int id = _casterId;
                foreach (Map map in Find.Maps)
                {
                    if (map.listerThings.listsByDef.TryGetValue(_shooterDef, out var things))
                    {
                        _shooter = things.First(t => t.thingIDNumber == id) as Pawn;
                        if (_shooter != null)
                        {
                            return _shooter;
                        }
                    }
                }
                return null;
            }
            set
            {
                _shooter = value;
                _shooterId = _shooter?.thingIDNumber ?? 0;
                _shooterDef = _shooter?.def ?? null;
            }
        }       

        public bool TargetingMapCell
        {
            get => targetCell.IsValid;
        }

        public bool IsValid
        {            
            get => _valid;
        }

        public Vector3 InboundVec
        {
            get => -1f * outboundVec;
        }        

        public GlobalShellingInfo(int sourceTile,int targetTileIndex, float tilesPerTick , Vector3 outboundVec, IntVec3? sourceMapExitCell = null, IntVec3? targetCell = null, Thing caster=null, Pawn shooter = null)
        {
            this.sourceTile = sourceTile;
            this.targetCell = !targetCell.HasValue ? IntVec3.Invalid : targetCell.Value;
            this.targetTile = targetTileIndex;
            this.sourceMapExitCell = sourceMapExitCell.HasValue ? sourceMapExitCell.Value : IntVec3.Invalid;
            this.tilesPerTick = tilesPerTick;
            this.outboundVec = outboundVec;
            this._caster = caster;            
            this._shooter = shooter;
            this._valid = true;
            this._casterId =  _caster?.thingIDNumber ?? 0;
            this._shooterId = _shooter?.thingIDNumber ?? 0;
            this._casterDef = _caster?.def ?? null;
            this._shooterDef = _shooter?.def ?? null;
        }

        public void ExposeData()
        {            
            Scribe_Values.Look(ref targetTile, "targetTileIndex");
            Scribe_Values.Look(ref sourceTile, "sourceTile");           
            Scribe_Values.Look(ref targetCell, "targetCell", IntVec3.Invalid);
            Scribe_Values.Look(ref tilesPerTick, "tilesPerTick");
            Scribe_Values.Look(ref sourceMapExitCell, "sourceMapExitCell", IntVec3.Invalid);
            Scribe_Values.Look(ref outboundVec, "outboundVec");
            
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                _casterDef = _caster?.def ?? null;
                _casterId = _caster?.thingIDNumber ?? 0;
                _shooterDef = _shooter?.def ?? null;                
                _shooterId = _shooter?.thingIDNumber ?? 0;
            }          
            Scribe_Values.Look(ref _shooterId, "caster_id");
            Scribe_Defs.Look(ref _shooterDef, "caster_def");
            Scribe_Values.Look(ref _casterId, "shooter_id");            
            Scribe_Defs.Look(ref _casterDef, "shooter_def");
            Scribe_Values.Look(ref _valid, "_valid", false);
        }
    }
}
