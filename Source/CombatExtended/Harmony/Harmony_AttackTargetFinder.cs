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
        private static SightGrid sightGrid;
        private static TurretTracker turretTracker;
        private static CompTacticalManager manager;        

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
                interceptors = searcher.Thing?.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor)
                                               .Select(t => t.TryGetComp<CompProjectileInterceptor>())
                                               .ToList() ?? new List<CompProjectileInterceptor>();
                if(searcher.Thing is Pawn pawn && pawn.Faction != null)
                {
                    manager = pawn.GetComp<CompTacticalManager>();
                    map.GetComponent<SightTracker>().TryGetGrid(pawn, out sightGrid);
                    if (pawn.Faction.HostileTo(map.ParentFaction))
                        turretTracker = map.GetComponent<TurretTracker>();
                }                
            }

            internal static void Postfix()
            {
                sightGrid = null;
                turretTracker = null;
            }
        }

        [HarmonyPatch(typeof(AttackTargetFinder), nameof(AttackTargetFinder.GetShootingTargetScore))]
        internal static class Harmony_AttackTargetFinder_GetShootingTargetScore
        {
            public static void Postfix(IAttackTarget target, IAttackTargetSearcher searcher, Verb verb, ref float __result)
            {
                float distance = target.Thing.Position.DistanceToSquared(searcher.Thing.Position);
                if(manager != null)
                {
                    List<Pawn> allies = manager.TargetedByEnemy;                    
                    if (allies.Count(d => d.Position.DistanceToSquared(target.Thing.Position) < 16f) > 3)
                        __result -= allies.Count() * 2;
                    else
                        __result -= allies.Count();
                }
                if (target.Thing is Pawn other && searcher.Thing is Pawn pawn)
                {
                    if ((pawn.pather?.moving ?? false) && pawn.EdgingCloser(other))
                        __result += (verb.EffectiveRange * verb.EffectiveRange - distance) / (verb.EffectiveRange * verb.EffectiveRange + 1f) * 20;
                }                
                if (target.Thing != null)
                {                    
                    if (turretTracker != null)
                        __result -= turretTracker.GetTurretsVisibleCount(map.cellIndices.CellToIndex(target.Thing.Position)) * 3f;
                    if (sightGrid != null)
                        __result -= sightGrid.GetCellSightCoverRating(target.Thing.Position);                    
                }
                if (verb.EffectiveRange >= 25)
                {
                    if (searcher.Thing.Map?.GetLightingTracker() is LightingTracker tracker)
                        __result *= tracker.CombatGlowAt(target.Thing.Position) * 0.5f;

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
                                        __result += 60f;
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
}
