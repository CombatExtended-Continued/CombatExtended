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
        private float tilesPerTick;        

        private int distanceInTiles;
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

        public float TraveledPtc => this.distanceTraveled / this.distanceInTiles;
        public override Vector3 DrawPos => Vector3.Slerp(Start, End, TraveledPtc);        

        public TravelingThing()
        {            
        }

        public virtual bool TryTravel(int startingTile, int destinationTile)
        {            
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
            base.Tick();
            distanceTraveled += this.tilesPerTick;
            if(TraveledPtc >= 1.0f)
            {
                Tile = destinationTile;
                Arrived();
                Destroy();
            }
        }        

        public override void ExposeData()
        {
            Scribe_Values.Look(ref startingTile, "startingTile");
            Scribe_Values.Look(ref destinationTile, "destinationTile");
            Scribe_Values.Look(ref tilesPerTick, "tilesPerTick");
            Scribe_Values.Look(ref distanceInTiles, "distanceInTiles");
            Scribe_Values.Look(ref distance, "distance");
            Scribe_Values.Look(ref distanceTraveled, "distanceTraveled");            
        }       
    }
}
