using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public abstract class LoadoutGenerator_List : LoadoutGenerator
    {
        protected List<ThingDef> availableDefs;

        public override void GenerateLoadout(CompInventory inventory)
        {
            compInvInt = inventory;
            // Initialize available ThingDefs
            InitAvailableDefs();
            if (availableDefs.NullOrEmpty())
            {
                return;
            }
            base.GenerateLoadout(inventory);
        }

        protected abstract void InitAvailableDefs();

        protected abstract float GetWeightForDef(ThingDef def);

        protected override Thing GenerateLoadoutThing(int max)
        {
            ThingDef def = availableDefs.RandomElementByWeight(GetWeightForDef);
            thingToMake = def;
            return base.GenerateLoadoutThing(max);
        }
    }
}
