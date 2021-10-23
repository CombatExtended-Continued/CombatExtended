using System;
using Verse;

namespace CombatExtended
{
    public class TravelingShell : TravelingThing
    {
        public ThingDef shellDef;        
        public GlobalShellingInfo shellingInfo;

        public override float TilesPerTick
        {
            get => shellingInfo.tilesPerTick;
        }

        public TravelingShell()
        {                        
        }        

        protected override void Arrived()
        {
        }

        public override void Tick()
        {
            base.Tick();            
        }

        public override void ExposeData()
        {
            Scribe_Deep.Look(ref shellingInfo, "shellInfo");
            Scribe_Defs.Look(ref shellDef, "shellDef");            
            base.ExposeData();            
        }
    }
}
