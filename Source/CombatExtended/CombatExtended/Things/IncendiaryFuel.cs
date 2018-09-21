using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class IncendiaryFuel : Filth
    {
        private const float maxFireSize = 1.75f;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            List<Thing> list = new List<Thing>(Position.GetThingList(map));
            foreach (Thing thing in list)
            {
                if (thing.HasAttachment(ThingDefOf.Fire))
                {
                    Fire fire = (Fire)thing.GetAttachment(ThingDefOf.Fire);
                    if (fire != null)
                        fire.fireSize = maxFireSize;
                }
                else
                {
                    thing.TryAttachFire(maxFireSize);
                }
            }
        }

        public override void Tick()
        {
            if (Position.GetThingList(base.Map).Any(x => x.def == ThingDefOf.Filth_FireFoam))
            {
                if (!Destroyed)
                    Destroy();
            }
            else
            {
                FireUtility.TryStartFireIn(Position, base.Map, maxFireSize);
            }
        }
    }
}
