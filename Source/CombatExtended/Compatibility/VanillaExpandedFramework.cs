using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using VEF.Apparels;
namespace CombatExtended.Compatibility;

public class VanillaExpandedFramework : IPatch
{
    const string ModName = "Vanilla Expanded Framework";
    bool IPatch.CanInstall()
    {
        return ModLister.HasActiveModWithName(ModName);
    }

    void IPatch.Install()
    {
        BlockerRegistry.RegisterCheckForCollisionBetweenCallback(CheckInterceptBetween);
        BlockerRegistry.RegisterShieldZonesCallback(ShieldZonesCallback);
    }

    // Copy of how CE handles vanilla shields
    private static bool CheckInterceptBetween(ProjectileCE projectile, Vector3 from, Vector3 to)
    {
        return CheckIntercept(projectile);
    }

    private IEnumerable<IEnumerable<IntVec3>> ShieldZonesCallback(Thing pawnToSuppress)
    {
        IEnumerable<CompShieldField> interceptors = CompShieldField.ListerShieldGensActiveIn(pawnToSuppress.Map).ToList();
        if (!interceptors.Any())
        {
            yield break;
        }
        foreach (var interceptor in interceptors)
        {
            if (!interceptor.CanFunction)
            {
                continue;
            }
            yield return GenRadial.RadialCellsAround(interceptor.HostThing.Position, interceptor.ShieldRadius, true);
        }
    }

    private static bool CheckIntercept(ProjectileCE projectile)
    {
        IEnumerable<CompShieldField> interceptors = CompShieldField.ListerShieldGensActiveIn(projectile.Map).ToList();
        if (!interceptors.Any())
        {
            return false;
        }
        Vector3 lastExactPos = projectile.LastPos;
        var newExactPos = projectile.ExactPosition;
        foreach (var interceptor in interceptors)
        {
            if (!interceptor.CanFunction)
            {
                continue;
            }

            Vector3 shieldPosition = interceptor.HostThing.Position.ToVector3ShiftedWithAltitude(0.5f);
            float radius = interceptor.ShieldRadius;
            bool spherical = projectile.def.projectile.flyOverhead;
            if (!CE_Utility.IntersectionPoint(lastExactPos, newExactPos, shieldPosition, radius, out Vector3[] intersectionPoints, spherical: spherical))
            {
                continue;
            }

            projectile.ExactPosition = intersectionPoints.OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
            projectile.landed = true;
            projectile.InterceptProjectile(interceptor.HostThing, projectile.ExactPosition, true);
            float damageAmount = CE_Utility.CalculateAbsorbedDamage(projectile);
            interceptor.AbsorbDamage(damageAmount, projectile.def.projectile.damageDef, projectile.launcher);
            return true;
        }
        return false;
    }

}

