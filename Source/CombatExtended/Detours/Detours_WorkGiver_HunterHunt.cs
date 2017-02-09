using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace CombatExtended.Detours
{
    internal static class Detours_WorkGiver_HunterHunt
    {
        internal static bool HasHuntingWeapon(Pawn p)
        {
            if (p.equipment.Primary != null && p.equipment.Primary.def.IsRangedWeapon)
            {
                CompAmmoUser comp = p.equipment.Primary.TryGetComp<CompAmmoUser>();
                if (comp == null 
                    || !comp.useAmmo 
                    || (comp.hasMagazine && comp.curMagCount > 0) 
                    || comp.hasAmmo)
                    return true;
            }
            return false;
        }
    }
}
