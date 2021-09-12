using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class WorkGiver_HunterHuntCE : WorkGiver_HunterHunt
    {
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return base.ShouldSkip(pawn, forced) || HasMeleeShieldAndTwoHandedWeapon(pawn);
        }

        public static bool HasMeleeShieldAndTwoHandedWeapon(Pawn p)
        {
            if(p.equipment.Primary != null && !(p.equipment.Primary.def.weaponTags?.Contains(Apparel_Shield.OneHandedTag) ?? false))
            {
                List<Apparel> wornApparel = p.apparel.WornApparel;
                foreach (Apparel apparel in wornApparel)
                {
                    if (apparel is Apparel_Shield) return true;
                }
            }
            return false;
        }
    }
}
