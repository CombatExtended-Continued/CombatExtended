using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using VFECore;
namespace CombatExtended.Compatibility
{
    public class VanillaExpandedFramework : IPatch
    {
        const string ModName = "Vanilla Expanded Framework";
        bool IPatch.CanInstall()
        {
            return ModLister.HasActiveModWithName(ModName);
        }

        IEnumerable<string> IPatch.GetCompatList()
        {
            yield break;
        }

        void IPatch.Install()
        {
            BlockerRegistry.RegisterCheckForCollisionCallback(CheckIntercept);
        }
        private static bool CheckIntercept(ProjectileCE projectile, IntVec3 cell, Thing launcher)
        {
            List<CompShieldField> interceptors;
            if (!CompShieldField.listerShieldGensByMaps.TryGetValue(projectile.Map, out interceptors))
            {
                return false;
            }
            if (!interceptors.Any())
            {
                return false;
            }
            var def = projectile.def;
            Vector3 lastExactPos = (Vector3)new Traverse(projectile).Field("lastExactPos").GetValue();//Why this field(or linked property) is private?
            var newExactPos = projectile.ExactPosition;
            foreach (var interceptor in interceptors)
            {
                var interceptorComp = interceptor;
                if (!interceptorComp.active)
                {
                    continue;
                }
                if (interceptorComp.ThingsWithinRadius.Contains(launcher))
                {
                    continue;
                }
                Vector3 shieldPosition = interceptor.parent.Position.ToVector3ShiftedWithAltitude(0.5f);
                float radius = interceptorComp.ShieldRadius;
                float blockRadius = radius + def.projectile.SpeedTilesPerTick + 0.1f;
                if ((lastExactPos - shieldPosition).sqrMagnitude < radius * radius)
                {
                    continue;
                }
                if ((newExactPos - shieldPosition).sqrMagnitude > Mathf.Pow(blockRadius, 2))
                {
                    continue;
                }

                if (projectile.def.projectile.flyOverhead)
                {
                    continue;
                }

                if ((shieldPosition - lastExactPos).sqrMagnitude <= Mathf.Pow((float)radius, 2))
                {
                    continue;
                }
                if (!IntersectLineSphericalOutline(shieldPosition, radius, lastExactPos, newExactPos))
                {
                    continue;
                }
                var intersectionPoints = //BlockerRegistry.GetExactPosition(projectile.OriginIV3.ToVector3(), projectile.ExactPosition, interceptorThing.Position.ToVector3(), (radius ) * (radius));
                    VanillaPsycastExpanded.IntersectionPoint(projectile.OriginIV3.ToVector3(), projectile.ExactPosition, interceptor.parent.Position.ToVector3(), (radius));
                if (intersectionPoints == new Vector3[] { Vector3.zero, Vector3.zero })
                {
                    continue;
                }
                projectile.ExactPosition = intersectionPoints.OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
                projectile.landed = true;
                interceptorComp.AbsorbDamage(projectile.DamageAmount, projectile.def.projectile.damageDef, launcher);
                return true;
            }
            return false;
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
