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
            //BlockerRegistry.RegisterImpactSomethingCallback(ImpactSomething); //temp commented
            BlockerRegistry.RegisterCheckForCollisionCallback(Hediff_Overshield_InterceptCheck);
            BlockerRegistry.RegisterCheckForCollisionBetweenCallback(AOE_CheckIntercept);
        }

        private static bool ImpactSomething(ProjectileCE projectile, Thing launcher)
        {
            return Hediff_Overshield_InterceptCheck(projectile, projectile.ExactPosition.ToIntVec3(), launcher);
        }
        public static bool Hediff_Overshield_InterceptCheck(ProjectileCE projectile, IntVec3 cell, Thing launcher)
        {
            foreach (var interceptor in projectile.Map.thingGrid.ThingsListAt(cell).OfType<Pawn>()
                .SelectMany(x => x.health.hediffSet.hediffs)
                .Where(x => x.GetType() == typeof(Hediff_Overshield)).Cast<Hediff_Overshield>())
            {

                var def = projectile.def;
                Vector3 lastExactPos = projectile.LastPos;
                var newExactPos = projectile.ExactPosition;
                var result = interceptor.pawn != launcher && (interceptor.pawn.Position == cell || PreventTryColideWithPawn(projectile, interceptor.pawn, newExactPos));
                if (result)
                {
                    OnIntercepted(interceptor, projectile);
                    return result;
                }
            }
            return false;
        }
        public static bool AOE_CheckIntercept(ProjectileCE projectile, Vector3 from, Vector3 newExactPos)
        {
            var def = projectile.def;
            foreach (var interceptor in projectile.Map.listerThings.ThingsInGroup(ThingRequestGroup.Pawn).Cast<Pawn>()
                .SelectMany(x => x.health.hediffSet.hediffs)
                .Where(x => x is Hediff_Overshield && x.GetType() != typeof(Hediff_Overshield)).Cast<Hediff_Overshield>())
            {
                Vector3 shieldPosition = interceptor.pawn.Position.ToVector3ShiftedWithAltitude(0.5f);
                float radius = interceptor.OverlaySize;
                float blockRadius = radius + def.projectile.SpeedTilesPerTick + 0.1f;
                if ((from - shieldPosition).sqrMagnitude < radius * radius)
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

                if ((shieldPosition - from).sqrMagnitude <= Mathf.Pow((float)radius, 2))
                {
                    return false;
                }
                if (!CE_Utility.IntersectLineSphericalOutline(shieldPosition, radius, from, newExactPos))
                {
                    return false;
                }

                OnIntercepted(interceptor, projectile);
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

        private static void OnIntercepted(Hediff_Overshield interceptor, ProjectileCE projectile)
        {
            var newExactPos = projectile.ExactPosition;
            var exactPosition = CE_Utility.IntersectionPoint(projectile.OriginIV3.ToVector3(), projectile.ExactPosition, interceptor.pawn.Position.ToVector3(), interceptor.OverlaySize).OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
            projectile.ExactPosition = exactPosition;
            new Traverse(interceptor).Field("lastInterceptAngle").SetValue(newExactPos.AngleToFlat(interceptor.pawn.TrueCenter()));
            new Traverse(interceptor).Field("lastInterceptTicks").SetValue(Find.TickManager.TicksGame);
            new Traverse(interceptor).Field("drawInterceptCone").SetValue(true);

            Effecter eff = new Effecter(EffecterDefOf.Interceptor_BlockedProjectile);
            eff.Trigger(new TargetInfo(newExactPos.ToIntVec3(), interceptor.pawn.Map, false), TargetInfo.Invalid);
            eff.Cleanup();
            projectile.InterceptProjectile(interceptor, projectile.ExactPosition, true);
        }
    }
}
