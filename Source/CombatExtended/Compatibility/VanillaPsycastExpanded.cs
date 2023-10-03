using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VanillaPsycastsExpanded;
using Verse;

namespace CombatExtended.Compatibility
{
    public class VanillaPsycastExpanded : IPatch
    {
        const string ModName = "Vanilla Psycasts Expanded";
        public bool CanInstall()
        {
            Log.Message("Vanilla Psycasts Expanded loaded: " + ModLister.HasActiveModWithName(ModName));
            return ModLister.HasActiveModWithName(ModName);
        }

        public IEnumerable<string> GetCompatList()
        {
            yield break;
        }
        public void Install()
        {
            BlockerRegistry.RegisterImpactSomethingCallback(ImpactSomething);
            BlockerRegistry.RegisterCheckForCollisionCallback(CheckIntercept);
        }

        private static bool ImpactSomething(ProjectileCE projectile, Thing launcher)
        {


            var interceptor = projectile.Map.thingGrid.ThingsAt(projectile.ExactPosition.ToIntVec3()).FirstOrDefault(x => (x is Pawn) && ((x as Pawn).health?.hediffSet.hediffs.Any(x => x is Hediff_Overshield) ?? false)) as Pawn;
            if (interceptor != null)
            {
                var hediff = interceptor.health.hediffSet.hediffs.FirstOrDefault(x => x is Hediff_Overshield) as Hediff_Overshield;
                projectile.ExactPosition = CE_Utility.IntersectionPoint(projectile.OriginIV3.ToVector3(), projectile.ExactPosition, interceptor.DrawPos, hediff.OverlaySize).OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
                projectile.landed = true;

                new Traverse(interceptor).Field("lastInterceptAngle").SetValue(projectile.ExactPosition.AngleToFlat(interceptor.TrueCenter()));
                new Traverse(interceptor).Field("lastInterceptTicks").SetValue(Find.TickManager.TicksGame);
                new Traverse(interceptor).Field("drawInterceptCone").SetValue(true);

                Effecter eff = new Effecter(EffecterDefOf.Interceptor_BlockedProjectile);
                eff.Trigger(new TargetInfo(projectile.ExactPosition.ToIntVec3(), interceptor.Map, false), TargetInfo.Invalid);
                eff.Cleanup();
                projectile.InterceptProjectile(interceptor, projectile.ExactPosition, true);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool CheckIntercept(ProjectileCE projectile, IntVec3 cell, Thing launcher)
        {
            foreach (var interceptor in projectile.Map.listerThings.ThingsInGroup(ThingRequestGroup.Pawn).Cast<Pawn>()
                .SelectMany(x => x.health.hediffSet.hediffs)
                .Where(x => typeof(Hediff_Overshield).IsAssignableFrom(x.def.hediffClass)).Cast<Hediff_Overshield>())
            {
                var def = projectile.def;
                Vector3 lastExactPos = projectile.LastPos;
                var newExactPos = projectile.ExactPosition;
                if (interceptor.GetType() == typeof(Hediff_Overshield))
                {
                    var result = interceptor.pawn != launcher && (interceptor.pawn.Position == cell || PreventTryColideWithPawn(projectile, interceptor.pawn, newExactPos));
                    if (result)
                    {
                        projectile.ExactPosition = CE_Utility.IntersectionPoint(projectile.OriginIV3.ToVector3(), projectile.ExactPosition, interceptor.pawn.DrawPos, interceptor.OverlaySize).OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
                        projectile.landed = true;

                        new Traverse(interceptor).Field("lastInterceptAngle").SetValue(newExactPos.AngleToFlat(interceptor.pawn.TrueCenter()));
                        new Traverse(interceptor).Field("lastInterceptTicks").SetValue(Find.TickManager.TicksGame);
                        new Traverse(interceptor).Field("drawInterceptCone").SetValue(true);

                        Effecter e = new Effecter(EffecterDefOf.Interceptor_BlockedProjectile);
                        e.Trigger(new TargetInfo(newExactPos.ToIntVec3(), interceptor.pawn.Map, false), TargetInfo.Invalid);
                        e.Cleanup();
                        projectile.InterceptProjectile(interceptor, projectile.ExactPosition, true);
                    }

                    return result;
                }
                #region AOE shields
                Vector3 shieldPosition = interceptor.pawn.Position.ToVector3ShiftedWithAltitude(0.5f);
                float radius = interceptor.OverlaySize;
                float blockRadius = radius + def.projectile.SpeedTilesPerTick + 0.1f;
                if ((lastExactPos - shieldPosition).sqrMagnitude < radius * radius)
                {
                    return false;
                }
                if ((newExactPos - shieldPosition).sqrMagnitude > Mathf.Pow(blockRadius, 2))
                {
                    return false;
                }

                if (projectile.def.projectile.flyOverhead)
                {
                    return false;
                }

                if ((shieldPosition - lastExactPos).sqrMagnitude <= Mathf.Pow((float)radius, 2))
                {
                    return false;
                }
                if (!IntersectLineSphericalOutline(shieldPosition, radius, lastExactPos, newExactPos))
                {
                    return false;
                }
                var exactPosition = CE_Utility.IntersectionPoint(projectile.OriginIV3.ToVector3(), projectile.ExactPosition, interceptor.pawn.Position.ToVector3(), (radius)).OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
                projectile.ExactPosition = exactPosition;
                projectile.landed = true;

                new Traverse(interceptor).Field("lastInterceptAngle").SetValue(newExactPos.AngleToFlat(interceptor.pawn.TrueCenter()));
                new Traverse(interceptor).Field("lastInterceptTicks").SetValue(Find.TickManager.TicksGame);
                new Traverse(interceptor).Field("drawInterceptCone").SetValue(true);

                Effecter eff = new Effecter(EffecterDefOf.Interceptor_BlockedProjectile);
                eff.Trigger(new TargetInfo(newExactPos.ToIntVec3(), interceptor.pawn.Map, false), TargetInfo.Invalid);
                eff.Cleanup();
                projectile.InterceptProjectile(interceptor, projectile.ExactPosition, true);
                return true;
            }

            return false;
        }
        private static bool PreventTryColideWithPawn(ProjectileCE projectile, Thing pawn, Vector3 newExactPos)
        {
            if (newExactPos.ToIntVec3() == pawn.Position)
            {
                return true;
            }

            var bounds = CE_Utility.GetBoundsFor(pawn);
            if (!bounds.IntersectRay(projectile.ShotLine, out var dist))
            {
                return false;
            }
            if (dist * dist > projectile.ExactMinusLastPos.sqrMagnitude + (projectile.minCollisionDistance * 2))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Copy of ProjectileCE method
        /// </summary>
        private static bool IntersectLineSphericalOutline(Vector3 center, float radius, Vector3 pointA, Vector3 pointB)
        {
            var pointAInShield = (center - pointA).sqrMagnitude <= Mathf.Pow(radius, 2);
            var pointBInShield = (center - pointB).sqrMagnitude <= Mathf.Pow(radius, 2);

            if (pointAInShield && pointBInShield)
            {
                return false;
            }
            if (!pointAInShield && !pointBInShield)
            {
                return false;
            }

            return true;
        }
    }
}
#endregion
