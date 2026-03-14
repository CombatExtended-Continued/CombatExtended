using CombatExtended.Lasers;
using RimWorld;
using UnityEngine;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Projectiles/Projectile_ArtilleryBeam.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
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
