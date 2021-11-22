using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public abstract class TravelingThing : WorldObject
    {        
        private int startingTile;
        private int destinationTile;
        private int distanceInTiles;

        private float tilesPerTick;                
        private float distance;
        private float distanceTraveled;

        private Vector3? _start;
        protected virtual Vector3 Start
        {
            get => _start.HasValue ? _start.Value : (_start = Find.WorldGrid.GetTileCenter(startingTile)).Value;
        }
        private Vector3? _end;
        protected virtual Vector3 End
        {
            get => _end.HasValue ? _end.Value : (_end = Find.WorldGrid.GetTileCenter(destinationTile)).Value;
        }        

        public virtual float TilesPerTick
        {
            get => 0.03f;
        }
        public int StartTile
        {
            get => startingTile;
        }
        public int DestinationTile
        {
            get => startingTile;
        }

        public float TraveledPtc => this.distanceTraveled / this.distanceInTiles;
        public override Vector3 DrawPos => Vector3.Slerp(Start, End, TraveledPtc);        

        public TravelingThing()
        {            
        }        

        public virtual bool TryTravel(int startingTile, int destinationTile)
        {
            if(startingTile <= -1 || destinationTile <= -1 || startingTile == destinationTile)
            {
                Log.Warning($"CE: TryTravel in thing {this} got {startingTile} {destinationTile}");
                return false;
            }
            this.startingTile = this.Tile = startingTile;
            this.destinationTile = destinationTile;
            this.tilesPerTick = TilesPerTick;            
            
            Vector3 start = Find.WorldGrid.GetTileCenter(startingTile);
            Vector3 end = Find.WorldGrid.GetTileCenter(destinationTile);               

            this.distance = GenMath.SphericalDistance(start.normalized, end.normalized);
            this.distanceInTiles = (int) Find.World.grid.ApproxDistanceInTiles(this.distance);            
            return true;
        }
               
        protected abstract void Arrived();

        public override void Tick()
        {
            try
            {
                base.Tick();
                distanceTraveled += this.tilesPerTick;
                if (TraveledPtc >= 1.0f)
                {
                    Tile = destinationTile;
                    Arrived();
                    Destroy();
                }
            }catch(Exception er)
            {
                Log.Error($"CE: TravelingThing {this} threw an exception {er}");
                Log.Warning($"CE: TravelingThing {this} is being destroyed to prevent further errors");
                Destroy();
            }
        }        

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref startingTile, "startingTile");
            Scribe_Values.Look(ref destinationTile, "destinationTile");
            Scribe_Values.Look(ref tilesPerTick, "tilesPerTick");
            Scribe_Values.Look(ref distanceInTiles, "distanceInTiles");
            Scribe_Values.Look(ref distance, "distance");
            Scribe_Values.Look(ref distanceTraveled, "distanceTraveled");            
        }       
    }
}
