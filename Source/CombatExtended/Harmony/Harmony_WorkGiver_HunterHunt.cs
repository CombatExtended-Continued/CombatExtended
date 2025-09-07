using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(WorkGiver_HunterHunt), "HasHuntingWeapon")]
    public class Harmony_WorkGiver_HunterHunt_HasHuntingWeapon_Patch
    {
        public static void Postfix(ref bool __result, Pawn p)
        {
            if (__result)
            {
                // Check if gun has ammo first
                CompAmmoUser comp = p.equipment.Primary.TryGetComp<CompAmmoUser>();
                __result = comp == null || comp.CanBeFiredNow || comp.HasAmmo;
            }
            else
            {
                // Change result to true if we have melee weapon and melee hunting is allowed in settings
                ThingWithComps eq = p.equipment.Primary;
                __result = eq != null && Controller.settings.AllowMeleeHunting && eq.def.IsMeleeWeapon;
            }
        }
    }
}
