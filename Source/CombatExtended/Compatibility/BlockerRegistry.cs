using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatExtended.Compatibility
{
    public static class BlockerRegistry
    {
        private static bool enabledCB = false;
        private static bool enabledIS = false;
        private static bool enabledBCW = false;
        private static bool enabledPUF = false;
        private static bool enabledSZ = false;
        private static List<Func<ProjectileCE, Vector3, Vector3, IEnumerable<(Vector3 IntersectionPos, Action OnIntersection)>>> checkForCollisionBetweenCallbacks;
        private static List<Func<ProjectileCE, Thing, bool>> impactSomethingCallbacks;
        private static List<Func<ProjectileCE, Thing, bool>> beforeCollideWithCallbacks;
        private static List<Func<Pawn, IntVec3, bool>> pawnUnsuppresableFromCallback;
        private static List<Func<Thing, IEnumerable<IEnumerable<IntVec3>>>> shieldZonesCallback;

        private static void EnableCB()
        {
            enabledCB = true;
            checkForCollisionBetweenCallbacks = new List<Func<ProjectileCE, Vector3, Vector3, IEnumerable<(Vector3, Action)>>>();
        }
        private static void EnableIS()
        {
            enabledIS = true;
            impactSomethingCallbacks = new List<Func<ProjectileCE, Thing, bool>>();
        }
        private static void EnableSZ()
        {
            enabledSZ = true;
            shieldZonesCallback = new List<Func<Thing, IEnumerable<IEnumerable<IntVec3>>>>();
        }
        private static void EnablePUF()
        {
            enabledPUF = true;
            pawnUnsuppresableFromCallback = new List<Func<Pawn, IntVec3, bool>>();
        }
        private static void EnableBCW()
        {
            enabledBCW = true;
            beforeCollideWithCallbacks = new List<Func<ProjectileCE, Thing, bool>>();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="f">Func that returns potential intersection pos and action </param>
        public static void RegisterCheckForCollisionBetweenCallback(Func<ProjectileCE, Vector3, Vector3, IEnumerable<(Vector3 IntersectionPos, Action OnIntersection)>> f)
        {
            if (!enabledCB)
            {
                EnableCB();
            }
            checkForCollisionBetweenCallbacks.Add(f);
        }


        public static void RegisterImpactSomethingCallback(Func<ProjectileCE, Thing, bool> f)
        {
            if (!enabledIS)
            {
                EnableIS();
            }
            impactSomethingCallbacks.Add(f);
        }
        public static void RegisterBeforeCollideWithCallback(Func<ProjectileCE, Thing, bool> f)
        {
            if (!enabledBCW)
            {
                EnableBCW();
            }
            beforeCollideWithCallbacks.Add(f);
        }
        public static (Vector3 IntersectionPos, Action OnInterception)? CheckForCollisionBetweenCallback(ProjectileCE projectile, Vector3 from, Vector3 to)
        {
            if (!enabledCB)
            {
                return null;
            }
            float max = float.MaxValue;
            (Vector3 IntersectionPos, Action OnInterception)? ms = null;
            foreach (var cb in checkForCollisionBetweenCallbacks)
            {
                foreach (var possibleIntersection in cb(projectile, from, to))
                {
                    var dist = (from - possibleIntersection.IntersectionPos).sqrMagnitude;
                    if (dist < max)
                    {

                        ms = (possibleIntersection.IntersectionPos, possibleIntersection.OnIntersection);
                        max = dist;
                    }
                }
            }
            return ms;
        }

        public static void RegisterShieldZonesCallback(Func<Thing, IEnumerable<IEnumerable<IntVec3>>> f)
        {
            if (!enabledSZ)
            {
                EnableSZ();
            }
            shieldZonesCallback.Add(f);
        }
        public static void RegisterUnsuppresableFromCallback(Func<Pawn, IntVec3, bool> f)
        {
            if (!enabledPUF)
            {
                EnablePUF();
            }
            pawnUnsuppresableFromCallback.Add(f);
        }

        public static bool ImpactSomethingCallback(ProjectileCE projectile, Thing launcher)
        {
            if (!enabledIS)
            {
                return false;
            }
            foreach (var cb in impactSomethingCallbacks)
            {
                if (cb(projectile, launcher))
                {
                    return true;
                }
            }
            return false;
        }
        public static IEnumerable<IEnumerable<IntVec3>> ShieldZonesCallback(Thing thing)
        {
            if (!enabledSZ)
            {
                return null;
            }
            return shieldZonesCallback.SelectMany(cb => cb(thing));
        }
        public static bool PawnUnsuppresableFromCallback(Pawn pawn, IntVec3 origin)
        {
            if (!enabledPUF)
            {
                return false;
            }
            foreach (var cb in pawnUnsuppresableFromCallback)
            {
                if (cb(pawn, origin))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool BeforeCollideWithCallback(ProjectileCE projectile, Thing collideWith)
        {
            if (!enabledBCW)
            {
                return false;
            }
            foreach (var cb in beforeCollideWithCallbacks)
            {
                if (cb(projectile, collideWith))
                {
                    return true;
                }
            }
            return false;
        }
        public static Vector3 GetExactPosition(Vector3 origin, Vector3 curPosition, Vector3 shieldPosition, float radiusSq)
        {
            Vector3 velocity = curPosition - origin;

            double a = velocity.sqrMagnitude;
            double b = 2 * (velocity.x * (origin.x - shieldPosition.x) + velocity.z * (origin.z - shieldPosition.z));
            double c = (shieldPosition - origin).sqrMagnitude - radiusSq;
            double det = b * b - 4 * a * c;
            if (det < 0)
            {
                return curPosition;
            }
            float scalar = (float)(2 * c / (-b + Math.Sqrt(det)));
            return velocity * scalar + origin;
        }

    }
}
