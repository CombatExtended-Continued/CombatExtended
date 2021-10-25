using System;
using RimWorld.Planet;
using Verse;

namespace CombatExtended.WorldObjects
{
    public class HealthComp : WorldObjectComp
    {
        private float health = 1.0f;

        public HealthComp()
        {
        }

        public override void CompTick()
        {
            base.CompTick();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref health, "health", 1.0f);
        }
    }
}

