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
                projectile.ExactPosition = IntersectionPoint(projectile.OriginIV3.ToVector3(), projectile.ExactPosition, interceptor.DrawPos, hediff.OverlaySize).OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
                projectile.landed = true;

                new Traverse(interceptor).Field("lastInterceptAngle").SetValue(projectile.ExactPosition.AngleToFlat(interceptor.TrueCenter()));
                new Traverse(interceptor).Field("lastInterceptTicks").SetValue(Find.TickManager.TicksGame);
                new Traverse(interceptor).Field("drawInterceptCone").SetValue(true);

                Effecter eff = new Effecter(EffecterDefOf.Interceptor_BlockedProjectile);
                eff.Trigger(new TargetInfo(projectile.ExactPosition.ToIntVec3(), interceptor.Map, false), TargetInfo.Invalid);
                eff.Cleanup();
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
                Vector3 lastExactPos = (Vector3)new Traverse(projectile).Field("lastExactPos").GetValue();//Why this field(or linked property) is private?
                var newExactPos = projectile.ExactPosition;
                if (interceptor.GetType() == typeof(Hediff_Overshield))
                {
                    var result = interceptor.pawn != launcher && (interceptor.pawn.Position == cell || PreventTryColideWithPawn(projectile, interceptor.pawn, newExactPos));
                    if (result)
                    {
                        projectile.ExactPosition = IntersectionPoint(projectile.OriginIV3.ToVector3(), projectile.ExactPosition, interceptor.pawn.DrawPos, interceptor.OverlaySize).OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
                        projectile.landed = true;

                        new Traverse(interceptor).Field("lastInterceptAngle").SetValue(newExactPos.AngleToFlat(interceptor.pawn.TrueCenter()));
                        new Traverse(interceptor).Field("lastInterceptTicks").SetValue(Find.TickManager.TicksGame);
                        new Traverse(interceptor).Field("drawInterceptCone").SetValue(true);

                        Effecter e = new Effecter(EffecterDefOf.Interceptor_BlockedProjectile);
                        e.Trigger(new TargetInfo(newExactPos.ToIntVec3(), interceptor.pawn.Map, false), TargetInfo.Invalid);
                        e.Cleanup();
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
                var exactPosition = //BlockerRegistry.GetExactPosition(projectile.OriginIV3.ToVector3(), projectile.ExactPosition, interceptorThing.Position.ToVector3(), (radius ) * (radius));
                    IntersectionPoint(projectile.OriginIV3.ToVector3(), projectile.ExactPosition, interceptor.pawn.Position.ToVector3(), (radius)).OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
                projectile.ExactPosition = exactPosition;
                projectile.landed = true;

                new Traverse(interceptor).Field("lastInterceptAngle").SetValue(newExactPos.AngleToFlat(interceptor.pawn.TrueCenter()));
                new Traverse(interceptor).Field("lastInterceptTicks").SetValue(Find.TickManager.TicksGame);
                new Traverse(interceptor).Field("drawInterceptCone").SetValue(true);

                Effecter eff = new Effecter(EffecterDefOf.Interceptor_BlockedProjectile);
                eff.Trigger(new TargetInfo(newExactPos.ToIntVec3(), interceptor.pawn.Map, false), TargetInfo.Invalid);
                eff.Cleanup();
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

        public static Vector3[] IntersectionPoint(Vector3 p1, Vector3 p2, Vector3 center, float radius)
        {
            Vector3 dp = new Vector3();
            Vector3[] sect;
            float a, b, c;
            float bb4ac;
            float mu1;
            float mu2;

            //  get the distance between X and Z on the segment
            dp.x = p2.x - p1.x;
            dp.z = p2.z - p1.z;
            //   I don't get the math here
            a = dp.x * dp.x + dp.z * dp.z;
            b = 2 * (dp.x * (p1.x - center.x) + dp.z * (p1.z - center.z));
            c = center.x * center.x + center.z * center.z;
            c += p1.x * p1.x + p1.z * p1.z;
            c -= 2 * (center.x * p1.x + center.z * p1.z);
            c -= radius * radius;
            bb4ac = b * b - 4 * a * c;
            if (Mathf.Abs(a) < float.Epsilon || bb4ac < 0)
            {
                //  line does not intersect
                return new Vector3[] { Vector3.zero, Vector3.zero };
            }
            mu1 = (-b + Mathf.Sqrt(bb4ac)) / (2 * a);
            mu2 = (-b - Mathf.Sqrt(bb4ac)) / (2 * a);
            sect = new Vector3[2];
            sect[0] = new Vector3(p1.x + mu1 * (p2.x - p1.x), 0, p1.z + mu1 * (p2.z - p1.z));
            sect[1] = new Vector3(p1.x + mu2 * (p2.x - p1.x), 0, p1.z + mu2 * (p2.z - p1.z));

            return sect;
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
