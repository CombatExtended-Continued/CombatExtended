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
    static class BlockerRegistry
    {
        private static bool enabled = false;
        private static List<Func<ProjectileCE,IntVec3,Thing,bool>> checkCellForCollisionCallbacks;
        private static List<Func<ProjectileCE,Thing,bool>> impactSomethingCallbacks;

        private static void Enable()
        {
            enabled = true;
            impactSomethingCallbacks = new List<Func<ProjectileCE,Thing,bool>>();
            checkCellForCollisionCallbacks = new List<Func<ProjectileCE,IntVec3,Thing,bool>>();
        }

        public static void RegisterCheckForCollisionCallback(Func<ProjectileCE,IntVec3,Thing,bool> f)
        {
            if (!enabled)
            {
                Enable();
            }
            checkCellForCollisionCallbacks.Add(f);
        }

        public static void RegisterImpactSomethingCallback(Func<ProjectileCE,Thing,bool> f)
        {
            if (!enabled)
            {
                Enable();
            }
            impactSomethingCallbacks.Add(f);
        }

        public static bool CheckCellForCollisionCallback(ProjectileCE projectile, IntVec3 cell, Thing launcher)
        {
            if (!enabled)
            {
                return false;
            }
            foreach (var cb in checkCellForCollisionCallbacks)
            {
                if (cb(projectile, cell, launcher))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool ImpactSomethingCallback(ProjectileCE projectile, Thing launcher)
        {
            if (!enabled)
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
    }
}
