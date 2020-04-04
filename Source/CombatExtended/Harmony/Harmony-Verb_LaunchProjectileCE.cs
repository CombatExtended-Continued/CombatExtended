using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using CombatExtended;

namespace CombatExtended.HarmonyCE

{
    [HarmonyPatch(typeof(Verb), "TryFindShootLineFromTo")]
    internal static class Verb_LaunchProjectileCE_RerouteTryFindShootLineFromTo
    {
        internal static bool Prefix(Verb __instance, ref bool __result, IntVec3 root, LocalTargetInfo targ, ref ShootLine resultingLine)
        {
            if (__instance is Verb_LaunchProjectileCE launchVerbCE)
            {
                __result = (launchVerbCE as Verb_LaunchProjectileCE).TryFindCEShootLineFromTo(root, targ, out resultingLine);
                return false;
            }
            return true;
        }
    }
}