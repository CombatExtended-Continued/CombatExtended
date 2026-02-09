using CombatExtended.Lasers;
using RimWorld;
using UnityEngine;
using Verse;

#region License
// Any VGE Code used for compatibility has been taken from the following source
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Projectiles/Projectile_ArtilleryBeam.cs
#endregion

namespace CombatExtended.Compatibility.VGECompat;

public class Projectile_ArtilleryBeamCE : LaserBeamCE
{
    public override void SpawnBeam(Vector3 muzzle, Vector3 destination)
    {
        var beamMoteDef = def.projectile?.beamMoteDef;
        if (beamMoteDef == null || Map == null)
        {
            return;
        }

        float beamStartOffset = def.projectile?.beamStartOffset ?? 0f;
        Vector3 dir = (destination - muzzle).Yto0().normalized;
        Vector3 offsetA = dir * beamStartOffset;

        var originTarget = new TargetInfo(muzzle.ToIntVec3(), Map);
        var destTarget = new TargetInfo(destination.ToIntVec3(), Map);

        MoteMaker.MakeInteractionOverlay(beamMoteDef, originTarget, destTarget, offsetA, Vector3.zero);
    }
}
