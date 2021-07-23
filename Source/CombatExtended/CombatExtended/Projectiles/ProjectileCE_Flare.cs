using System;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class ProjectileCE_Flare : ProjectileCE_Explosive
    {
        public override void Impact(Thing hitThing)
        {
            landed = true;
            Flare flare;
            flare = (Flare)ThingMaker.MakeThing(CE_ThingDefOf.Flare, null);
            flare.DrawMode = Flare.FlareDrawMode.FlyOver;
            flare.Position = Position;
            flare.SpawnSetup(Map, false);
            base.Impact(null);
        }
    }
}
