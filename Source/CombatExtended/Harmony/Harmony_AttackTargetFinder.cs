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
        private static SightTracker.SightReader sightReader;
        private static TurretTracker turretTracker;
        private static CompTacticalManager manager;
        private static CombatReservationManager combatReservationManager;

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

            internal static void Prefix(IAttackTargetSearcher searcher)
            {               
                map = searcher.Thing?.Map;
                combatReservationManager = map.GetComponent<CombatReservationManager>();
                interceptors = searcher.Thing?.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor)
                                               .Select(t => t.TryGetComp<CompProjectileInterceptor>())
                                               .ToList() ?? new List<CompProjectileInterceptor>();
                if(searcher.Thing is Pawn pawn && pawn.Faction != null)
                {
                    manager = pawn.GetComp<CompTacticalManager>();                    
                    pawn.GetSightReader(out sightReader);
                    
                    if (pawn.Faction.HostileTo(map.ParentFaction))
                        turretTracker = map.GetComponent<TurretTracker>();
                }                
            }

            internal static void Postfix()
            {
                sightReader = null;
                turretTracker = null;
            }
        }

        [HarmonyPatch(typeof(AttackTargetFinder), nameof(AttackTargetFinder.GetShootingTargetScore))]
        internal static class Harmony_AttackTargetFinder_GetShootingTargetScore
        {
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                bool finished = false;
                for (int i = 0; i < instructions.Count(); i++)
                {
                    if (!finished)
                    {
                        if (codes[i].opcode == OpCodes.Ldc_R4)
                        {
                            finished = true;
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldarg_1);
                            yield return new CodeInstruction(OpCodes.Ldarg_2);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_AttackTargetFinder_GetShootingTargetScore), nameof(Harmony_AttackTargetFinder_GetShootingTargetScore.GetShootingTargetBaseScore)));
                            continue;
                        }
                    }
                    yield return codes[i];
                }                
            }

            public static float GetShootingTargetBaseScore(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb)
            {
                float result = 60f;
                float distSqr = target.Thing.Position.DistanceToSquared(searcher.Thing.Position);                

                if (combatReservationManager != null && combatReservationManager.Reserved(target.Thing, out List<Pawn> attackers))
                {                   
                    for(int i = 0; i < attackers.Count; i++)
                    {                        
                        Pawn attacker = attackers[i];                        
                        if (attacker.stances?.curStance is Stance_Warmup)
                            result -= 5f;
                        if ((attacker.jobs?.curJob?.def.alwaysShowWeapon ?? false)
                            && attacker.pather != null
                            && attacker.pather.curPath?.NodesConsumedCount > attacker.pather.curPath?.NodesLeftCount)
                            result -= 5f;
                        if (attacker.Position.DistanceToSquared(target.Thing.Position) < 16 && distSqr > 255)
                            result -= 2.5f;
                    }
                    result += 10 - attackers.Count * 3.5f;
                }                
                if (target.Thing is Pawn other && searcher.Thing is Pawn pawn)
                {
                    if ((pawn.pather?.moving ?? false) && pawn.EdgingCloser(other))
                        result += (verb.EffectiveRange * verb.EffectiveRange - distSqr) / (verb.EffectiveRange * verb.EffectiveRange + 1f) * 10;
                }                
                if (target.Thing != null && (verb.IsMeleeAttack || verb.EffectiveRange <= 25))
                {                                                                
                    if (sightReader != null)
                        result += 15 - sightReader.GetSightCoverRating(target.Thing.Position);

                    result += 10 - Mathf.Abs(16f * 16f - distSqr) / (16f * 16f) * 10;
                }                
                if (verb.EffectiveRange >= 25)
                {
                    if (searcher.Thing.Map?.GetLightingTracker() is LightingTracker tracker)
                        result *= tracker.CombatGlowAt(target.Thing.Position) * 0.5f;

                    if (map != null)
                    {
                        Vector3 srcPos = searcher.Thing.Position.ToVector3();
                        Vector3 trgPos = target.Thing.Position.ToVector3();

                        for (int i = 0; i < interceptors.Count; i++)
                        {
                            CompProjectileInterceptor interceptor = interceptors[i];
                            float radiusSqr = interceptor.Props.radius * interceptor.Props.radius;
                            if (interceptor.Active)
                            {
                                if (interceptor.parent.Position.DistanceToSquared(target.Thing.Position) < radiusSqr)
                                {
                                    if (interceptor.parent.Position.DistanceToSquared(searcher.Thing.Position) < radiusSqr)
                                        result += 60f;
                                    else
                                        result -= 30;
                                }
                                else if (interceptor.parent.Position.DistanceToSquared(searcher.Thing.Position) > radiusSqr
                                      && interceptor.parent.Position.ToVector3().DistanceToSegmentSquared(srcPos, trgPos, out _) < radiusSqr)
                                    result -= 30;
                            }
                        }
                    }
                }
                return result;
            }
        }
    }
}
