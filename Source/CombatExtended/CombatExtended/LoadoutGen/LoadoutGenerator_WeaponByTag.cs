using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class LoadoutGenerator_WeaponByTag : LoadoutGenerator_List
    {
        public string tag;
        public FloatRange baseMoney = FloatRange.Zero;
        private static Dictionary<string, List<ThingDef>> tagListDict = new Dictionary<string, List<ThingDef>>();

        protected override void InitAvailableDefs()
        {
            // Initialize and cache list of all weapons with our tag so we can use them in the future
            if (!tagListDict.ContainsKey(tag))
            {
                List<ThingDef> taggedWeaponList = new List<ThingDef>();
                foreach(ThingDef def in CE_Utility.allWeaponDefs)
                {
                    if (!def.weaponTags.NullOrEmpty() && def.weaponTags.Contains(tag))
                    {
                        taggedWeaponList.Add(def);
                    }
                }
                tagListDict.Add(tag, taggedWeaponList);
            }
            // Fetch cached list from dictionary
            tagListDict.TryGetValue(tag, out availableDefs);
        }

        protected override float GetWeightForDef(ThingDef def)
        {
            if (!baseMoney.Includes(def.BaseMarketValue))
            {
                return 0;
            }
            return 1;
        }

        protected override Thing GenerateLoadoutThing(int max)
        {
            Thing thing = base.GenerateLoadoutThing(max);
            Pawn pawn = compInvInt.parent as Pawn;
            if (pawn != null)
                PawnGenerator.PostProcessGeneratedGear(thing, compInvInt.parent as Pawn);
            return thing;
        }
    }
}
