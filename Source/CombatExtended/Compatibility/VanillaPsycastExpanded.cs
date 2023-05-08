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
                PostColide(interceptor, hediff , projectile.ExactPosition);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool CheckIntercept(ProjectileCE projectile, IntVec3 cell, Thing launcher)
        {
            foreach (var interceptor in CustomShieldHediffs(projectile.Map))
            {
                if (CheckIntercept(projectile, interceptor.pawn, interceptor, cell))
                {
                    return true;
                }
            }

            return false;
        }
        public static IEnumerable<Hediff_Overshield> CustomShieldHediffs(Map map)
        {
            //Log.Message(map.listerThings.ThingsInGroup(ThingRequestGroup.Pawn).Count.ToString());
            return map.listerThings.ThingsInGroup(ThingRequestGroup.Pawn).Cast<Pawn>()
                .SelectMany(x => x.health.hediffSet.hediffs)
                .Where(x => typeof(Hediff_Overshield).IsAssignableFrom(x.def.hediffClass)).Cast<Hediff_Overshield>();
        }

        /// <summary>
        /// Just copy of original method, but adapt for VE psycast new abilities
        /// </summary>
        /// <param name="interceptorThing">Pawn</param>
        /// <param name=""></param>
        /// <param name="withDebug"></param>
        /// <returns></returns>
        private static bool CheckIntercept(ProjectileCE projectile, Thing interceptorThing, Hediff_Overshield interceptorHediff, IntVec3 cell, bool withDebug = false)
        {
            var def = projectile.def;
            Vector3 lastExactPos = (Vector3)new Traverse(projectile).Field("lastExactPos").GetValue();//Why this field(or linked property) is private?
            var launcher = projectile.launcher;
            var newExactPos = projectile.ExactPosition;
            if (interceptorHediff.GetType() == typeof(Hediff_Overshield))
            {
                var result = interceptorThing.Position==cell || PreventTryColideWithPawn(projectile, interceptorThing, newExactPos);
                if (result)
                {
                    projectile.ExactPosition = IntersectionPoint(projectile.OriginIV3.ToVector3(), projectile.ExactPosition, interceptorThing.DrawPos, interceptorHediff.OverlaySize).OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
                    projectile.landed = true;
                    PostColide( interceptorThing, interceptorHediff,  projectile.ExactPosition);
                }

                return result;
            }
            #region AOE shields
            Vector3 shieldPosition = interceptorThing.Position.ToVector3ShiftedWithAltitude(0.5f);
            float radius = interceptorHediff.OverlaySize;
            float blockRadius = radius + def.projectile.SpeedTilesPerTick + 0.1f;
            if((lastExactPos-shieldPosition).sqrMagnitude<radius*radius)
            {
                return false;
            }
            if ((newExactPos - shieldPosition).sqrMagnitude > Mathf.Pow(blockRadius, 2))
            {
                return false;
            }
            //No such property
            //if (!interceptorComp.Active)
            //{
            //    return false;
            //}

            if (
                //No such property. Always ground projectiles
                //interceptorComp.Props.interceptGroundProjectiles && 
                projectile.def.projectile.flyOverhead)
            {
                return false;
            }
            //No such property. Always ground projectiles
            //if (interceptorComp.Props.interceptAirProjectiles && !projectile.def.projectile.flyOverhead)
            //{
            //    return false;
            //}

            //if ((launcher == null || !launcher.HostileTo(interceptorThing))
            //    //No such property
            //    //&& !interceptorComp.debugInterceptNonHostileProjectiles
            //    //&& !interceptorComp.Props.interceptNonHostileProjectiles
            //    )
            //{
            //    return false;
            //}
            if (
                //All custom interceptors abilities (not skipshield) allows outgoing. I found no such property, guess it's always true
                //!interceptorComp.Props.interceptOutgoingProjectiles &&
                (shieldPosition - lastExactPos).sqrMagnitude <= Mathf.Pow((float)radius, 2))
            {
                return false;
            }
            if (!IntersectLineSphericalOutline(shieldPosition, radius, lastExactPos, newExactPos))
            {
                return false;
            }
            var exactPosition = //BlockerRegistry.GetExactPosition(projectile.OriginIV3.ToVector3(), projectile.ExactPosition, interceptorThing.Position.ToVector3(), (radius ) * (radius));
                IntersectionPoint(projectile.OriginIV3.ToVector3(), projectile.ExactPosition, interceptorThing.Position.ToVector3(), (radius)).OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
            projectile.ExactPosition = exactPosition;
            projectile.landed = true;
            PostColide(interceptorThing, interceptorHediff, exactPosition);
            return true;
            #endregion
        }
        private static void PostColide(Thing interceptorThing, Hediff_Overshield interceptorHediff, Vector3 newExactPos)
        {
            //FIX ME(?): guess, thats not best practics to do it
            new Traverse(interceptorHediff).Field("lastInterceptAngle").SetValue(newExactPos.AngleToFlat(interceptorThing.TrueCenter()));
            new Traverse(interceptorHediff).Field("lastInterceptTicks").SetValue(Find.TickManager.TicksGame);
            new Traverse(interceptorHediff).Field("drawInterceptCone").SetValue(true);

            Effecter eff = new Effecter(EffecterDefOf.Interceptor_BlockedProjectile);
            eff.Trigger(new TargetInfo(newExactPos.ToIntVec3(), interceptorThing.Map, false), TargetInfo.Invalid);
            eff.Cleanup();
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
            if (dist * dist > projectile.ExactMinusLastPos.sqrMagnitude+(projectile.minCollisionDistance*2))
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// seems BlockerRegistry.GetExactPosition works a bit incorrect (at least for overshield with 1 radius: position can be behind of target - shoot from west, but impact on east side of pawn/shield)
        /// Copied from https://answers.unity.com/questions/1658184/circle-line-intersection-points.html
        /// </summary>
        /// <param name="p1">origin</param>
        /// <param name="p2">destination/Exact position</param>
        /// <param name="center">shield pos</param>
        /// <param name="radius">shield radius</param>

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
