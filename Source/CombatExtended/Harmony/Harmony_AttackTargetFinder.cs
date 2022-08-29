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
    internal static class Harmony_AttackTargetFinder
    {
        private static Map map;
        private static List<CompProjectileInterceptor> interceptors;

        [HarmonyPatch(typeof(AttackTargetFinder), "BestAttackTarget")]
        internal static class Harmony_AttackTargetFinder_BestAttackTarget
        {
            /// <summary>
            /// List of potential attack targets used by <see cref="FindAttackTargetForRangedAttack"/>.
            /// </summary>
            private static List<IAttackTarget> validTargets = new List<IAttackTarget>();

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

                // Replace the vanilla logic to determine the best target to be engaged with a ranged attack
                // with an optimized equivalent that avoids redundant and duplicate validation.
                var hasRangedAttack = AccessTools.Method(typeof(AttackTargetFinder), nameof(AttackTargetFinder.HasRangedAttack));
                var innerValidatorField = AccessTools.FindIncludingInnerTypes(
                    typeof(AttackTargetFinder),
                    (type) => AccessTools.DeclaredField(type, "innerValidator")
                );
                var instructionsToInsert = new List<CodeInstruction>() {
                    new CodeInstruction(OpCodes.Ldarg_1), // targetScanFlags
                    new CodeInstruction(OpCodes.Ldarg_0), // searcher
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldfld, innerValidatorField),
                    new CodeInstruction(OpCodes.Ldarg_S, 4), // maxDist
                    new CodeInstruction(OpCodes.Ldarg_S, 7), // canBashDoors
                    new CodeInstruction(OpCodes.Ldarg_S, 9), // canBashFences
                    new CodeInstruction(
                        OpCodes.Call,
                        AccessTools.Method(
                            typeof(Harmony_AttackTargetFinder_BestAttackTarget),
                            nameof(Harmony_AttackTargetFinder_BestAttackTarget.FindAttackTargetForRangedAttack)
                        )
                    ),
                    new CodeInstruction(OpCodes.Ret)
                };

                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].Branches(out Label? label) && codes[i - 1].Calls(hasRangedAttack))
                    {
                        var blockEnd = codes.FindIndex(instr => instr.labels.Contains(label.Value));
                        var blockEndInstr = codes[blockEnd];

                        var blockStart = 1 + codes.FindLastIndex(instr => instr.Branches(out Label? blockEndLabel) && blockEndInstr.labels.Contains(blockEndLabel.Value));
                        var blockStartInstr = codes[blockStart];

                        codes.RemoveRange(blockStart, blockEnd - blockStart - 1);
                        codes.InsertRange(blockStart, instructionsToInsert);

                        blockStartInstr.MoveLabelsTo(codes[blockStart]);

                        break;
                    }
                }

                return codes;
            }

            internal static void Prefix(IAttackTargetSearcher searcher)
            {
                map = searcher.Thing?.Map;
                interceptors = searcher.Thing?.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor)
                                               .Select(t => t.TryGetComp<CompProjectileInterceptor>())
                                               .ToList() ?? new List<CompProjectileInterceptor>();
            }

            /// <summary>
            /// Find the best target for a ranged attacker (which may require the attacker to change their shooting position).
            /// </summary>
            /// <remarks>
            /// This is an optimized equivalent of the vanilla logic in <see cref="AttackTargetFinder.BestAttackTarget"/>.
            /// </remarks>
            private static IAttackTarget FindAttackTargetForRangedAttack(
                TargetScanFlags targetScanFlags,
                IAttackTargetSearcher searcher,
                Predicate<IAttackTarget> attackTargetValidator,
                float maxDist,
                bool canBashDoors,
                bool canBashFences
            )
            {
                validTargets.Clear();

                var searcherThing = searcher.Thing;
                var potentialAttackTargets = searcherThing.Map.attackTargetsCache.GetPotentialTargetsFor(searcher);
                var searcherPos = searcherThing.Position;

                for (int i = 0; i < potentialAttackTargets.Count; i++)
                {
                    var target = potentialAttackTargets[i];
                    // Optimization: Check if the target is within the allowed maximum distance before running expensive validation logic
                    if (target.Thing.Position.InHorDistOf(searcherPos, maxDist) && attackTargetValidator(potentialAttackTargets[i]))
                    {
                        validTargets.Add(target);
                    }
                }

                // No valid targets exist for this attack, so abort early
                if (validTargets.Count == 0)
                {
                    return null;
                }

                var targetToHit = AttackTargetFinder.GetRandomShootingTargetByScore(validTargets, searcher, searcher.CurrentEffectiveVerb);

                // If a valid target can be fired on from the current position, prioritize engaging that.
                // Otherwise attempt to find the closest valid target (which may trigger the searcher to move in to engage / change position),
                // unless the searcher is a turret or a pawn operating a turret, which is ipso facto immobile.
                if (targetToHit != null || searcher is Building_Turret || (searcher is Pawn searcherPawn && searcherPawn.CurJobDef == JobDefOf.ManTurret))
                {
                    return targetToHit;
                }

                var checkForReachability = (targetScanFlags & TargetScanFlags.NeedReachableIfCantHitFromMyPos) != 0 || (targetScanFlags & TargetScanFlags.NeedReachable) != 0;

                if (checkForReachability)
                {
                    return (IAttackTarget)Verse.GenClosest.ClosestThing_Global(
                        searcherPos,
                        validTargets,
                        maxDist,
                        (Thing target) => AttackTargetFinder.CanReach(searcherThing, target, canBashDoors, canBashFences)
                    );
                }

                return (IAttackTarget)Verse.GenClosest.ClosestThing_Global(searcherPos, validTargets, maxDist);
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

                if (map != null)
                {
                    Vector3 srcPos = searcher.Thing.Position.ToVector3();
                    Vector3 trgPos = target.Thing.Position.ToVector3();

                    for (int i = 0; i < interceptors.Count; i++)
                    {
                        CompProjectileInterceptor interceptor = interceptors[i];
                        if (interceptor.Active)
                        {
                            if (interceptor.parent.Position.DistanceTo(target.Thing.Position) < interceptor.Props.radius)
                            {
                                if (interceptor.parent.Position.DistanceTo(searcher.Thing.Position) < interceptor.Props.radius)
                                    __result += 30;
                                else
                                    __result -= 30f;
                            }
                            else if (interceptor.parent.Position.DistanceTo(searcher.Thing.Position) > interceptor.Props.radius
                                && interceptor.parent.Position.ToVector3().DistanceToSegment(srcPos, trgPos, out _) < interceptor.Props.radius)
                                __result -= 30;
                        }
                    }
                }
            }
        }
    }
}
