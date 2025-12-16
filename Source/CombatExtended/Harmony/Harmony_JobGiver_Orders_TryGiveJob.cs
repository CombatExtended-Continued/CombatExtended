using HarmonyLib;
using RimWorld;
using RimWorld.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE;
[HarmonyPatch(typeof(JobGiver_Orders), "TryGiveJob")]
internal static class Harmony_JobGiver_Orders_TryGiveJob
{
    internal static void Postfix(Pawn pawn, Job __result)
    {
        if (__result != null && __result.expiryInterval < 0)
        {
            __result.expiryInterval = GenTicks.TicksPerRealSecond * 5; //Let other jobs (like a reload) start sometimes
        }
    }
}
