using CombatExtended.Lasers;
using RimWorld;
using UnityEngine;
using VanillaGravshipExpanded;
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

// We just need the special VGE mote
public class ProjectileCE_ArtilleryBeam : LaserBeamCE
{
    public override void SpawnBeam(Vector3 muzzle, Vector3 destination)
    {
        Vector3 direction = (ExactPosition - muzzle).Yto0().normalized;
        Vector3 offsetA = direction * def.projectile.beamStartOffset;
        var originMap = equipment?.MapHeld ?? launcher?.MapHeld;
        if (originMap is null)
        {
            return;
        }
        //if (originMap != this.Map)
        //{
        //    float angle = ArtilleryUtility.GetAngle(originMap.Tile, this.Map.Tile);
        //    offsetA -= new Vector3(2, 0, 2).RotatedBy(angle);
        //}
        if (def.projectile.beamMoteDef != null)
        {
            var originTarget = new TargetInfo((muzzle - direction * 0.9f).ToIntVec3(), Map);
            var destTarget = new TargetInfo(destination.ToIntVec3(), Map);

            MoteMaker.MakeInteractionOverlay(def.projectile.beamMoteDef, originTarget, destTarget, offsetA, Vector3.zero);
        }
    }
}
