using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Verse;
using Verse.AI;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(Verb_LaunchProjectileCE), "TryFindShootLineFromTo")]
    internal static class Verb_LaunchProjectileCE_RerouteTryFindShootLineFromTo
    {
        internal static bool Prefix(Verb_LaunchProjectileCE __instance, ref bool __result, IntVec3 root, LocalTargetInfo targ, out ShootLine resultingLine)
        {
            __result = __instance.TryFindCEShootLineFromTo(root, targ, out resultingLine);
            return false;
        }
    }
}