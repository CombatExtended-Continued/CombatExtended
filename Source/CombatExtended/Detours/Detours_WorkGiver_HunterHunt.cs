using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HugsLib.Source.Detour;

namespace CombatExtended.Detours
{
    internal static class Detours_WorkGiver_HunterHunt
    {
        internal static bool HasHuntingWeapon(Pawn p)
        {
            if (p.equipment.Primary != null && p.equipment.Primary.def.IsRangedWeapon)
            {
                CompAmmoUser comp = p.equipment.Primary.TryGetComp<CompAmmoUser>();
                if (comp == null || comp.hasAndUsesAmmoOrMagazine)
                    return true;
            }
            return false;
        }
    }
}
