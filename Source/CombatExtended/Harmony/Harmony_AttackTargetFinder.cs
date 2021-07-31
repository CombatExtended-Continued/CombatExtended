using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using CombatExtended.AI;
using CombatExtended.Utilities;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(AttackTargetFinder), "BestAttackTarget")]
    internal static class Harmony_AttackTargetFinder_BestAttackTarget
    {
        private static bool EMPOnlyTargetsMechanoids()
        {
            return false;
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            for (int i = 0; i < instructions.Count(); i++)
            {
                if (codes[i].opcode == OpCodes.Call && ReferenceEquals(codes[i].operand, AccessTools.Method(typeof(VerbUtility), nameof(VerbUtility.IsEMP))))
                {
                    codes[i - 2].opcode = OpCodes.Nop;
                    codes[i - 1].opcode = OpCodes.Nop;
                    codes[i].operand = typeof(Harmony_AttackTargetFinder_BestAttackTarget).GetMethod(nameof(EMPOnlyTargetsMechanoids), AccessTools.all);
                    break;
                }
            }
            return codes;
        }
    }

    [HarmonyPatch(typeof(AttackTargetFinder), nameof(AttackTargetFinder.GetShootingTargetScore))]
    internal static class Harmony_AttackTargetFinder_GetShootingTargetScore
    {
        public static void Postfix(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb, ref float __result)
        {
            float distance = target.Thing.Position.DistanceTo(searcher.Thing.Position);
            if (target.Thing is Pawn other && searcher.Thing is Pawn pawn)
            {
                if ((pawn.pather?.moving ?? false) && pawn.EdgingCloser(other))
                    __result += (verb.EffectiveRange - distance) / (verb.EffectiveRange + 1f) * 20;
            }
            LocalTargetInfo currentTarget = target.TargetCurrentlyAimingAt;
            if (currentTarget.IsValid)
            {
                if (currentTarget.HasThing && currentTarget.Thing is Pawn ally && (ally.Faction?.HostileTo(searcher.Thing.Faction) ?? false))
                {
                    CompSuppressable suppressable = ally.TryGetComp<CompSuppressable>();
                    if (suppressable != null)
                    {
                        if (suppressable.isSuppressed)
                            __result += 10f;
                        if (suppressable.IsHunkering)
                            __result += 25f;
                    }
                    if (ally.health?.HasHediffsNeedingTend() ?? false)
                        __result += 10f;
                }
            }
            if (searcher.Thing.Map?.GetLightingTracker() is LightingTracker tracker)
                __result += tracker.CombatGlowAt(target.Thing.Position) * 5f;
        }
    }

}
