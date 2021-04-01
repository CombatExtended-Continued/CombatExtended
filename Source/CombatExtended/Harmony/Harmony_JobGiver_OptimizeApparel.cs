using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    ///Give melee/ballistic shields a negative apparel score if the pawn has a two-handed weapon.
    ///Mimics vanilla behavior regarding shield belts and ranged weapons.
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel), "ApparelScoreGain_NewTmp")]
    internal static class Harmony_JobGiver_OptimizeApparel_ApparelScoreGain
    {
        internal static bool Prefix(Pawn pawn, Apparel ap, ref float __result)
        {
            if (ap is Apparel_Shield && pawn?.equipment?.Primary != null && pawn?.equipment?.Primary?.def?.weaponTags?.Contains(Apparel_Shield.OneHandedTag) != true)
            {
                __result = -1000f;
                return false;
            }
            return true;
        }
    }
}