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

        void IPatch.Install()
        {
            BlockerRegistry.RegisterCheckForCollisionCallback(CheckIntercept);
            BlockerRegistry.RegisterShieldZonesCallback(ShieldZonesCallback);
        }

        private IEnumerable<IEnumerable<IntVec3>> ShieldZonesCallback(Thing pawnToSuppress)
        {
            IEnumerable<CompShieldField> interceptors = CompShieldField.ListerShieldGensActiveIn(pawnToSuppress.Map).ToList();
            List<IEnumerable<IntVec3>> result = new List<IEnumerable<IntVec3>>();
            if (!interceptors.Any())
            {
                return result;
            }
            foreach (var interceptor in interceptors)
            {
                if (!interceptor.CanFunction)
                {
                    continue;
                }
                result.Add(GenRadial.RadialCellsAround(interceptor.HostThing.Position, interceptor.ShieldRadius, true));
            }
            return result;
        }

        private static bool CheckIntercept(ProjectileCE projectile, IntVec3 cell, Thing launcher)
        {
            if (projectile.def.projectile.flyOverhead)
            {
                return false;
            }
            IEnumerable<CompShieldField> interceptors = CompShieldField.ListerShieldGensActiveIn(projectile.Map).ToList();
            if (!interceptors.Any())
            {
                return false;
            }
            var def = projectile.def;
            Vector3 lastExactPos = projectile.LastPos.Yto0();
            var newExactPos = projectile.ExactPosition.Yto0();
            foreach (var interceptor in interceptors)
            {

                if (!interceptor.CanFunction)
                {
                    continue;
                }
                Vector3 shieldPosition = interceptor.HostThing.Position.ToVector3Shifted().Yto0();
                float radius = interceptor.ShieldRadius;

                Vector3[] intersectionPoints;
                if (!CE_Utility.IntersectionPoint(lastExactPos, newExactPos, shieldPosition, radius, out intersectionPoints, false))
                {
                    continue;
                }

                projectile.ExactPosition = intersectionPoints.OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
                projectile.landed = true;
                projectile.InterceptProjectile(interceptor.HostThing, projectile.ExactPosition, true);
                interceptor.AbsorbDamage(projectile.DamageAmount, projectile.def.projectile.damageDef, launcher);
                return true;
            }
            return false;
        }

    }

}
