#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Projectiles/Projectile_AnticraftBeam.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

using CombatExtended.Compatibility.VGECompat;
using UnityEngine;
using VanillaGravshipExpanded2;
using Verse;

namespace CombatExtended.Compatibility.VGE2Compat;

public class ProjectileCE_AnticraftBeam : ProjectileCE_ArtilleryBeam
{
    public AnticraftBeamStrikeCE strike;

    // same code but we use LaserBeamCE.Impact(Thing hitThing, Vector3 muzzle) instead of Projectile.Impact(Thing hitThing, bool blockedByShield = false)
    public override void Impact(Thing hitThing, Vector3 muzzle)
    {
        //if (!blockedByShield)
        //{
        strike = (AnticraftBeamStrikeCE)GenSpawn.Spawn(InternalDefOf.VGE_EnemyAnticraftBeamStrike, ExactPosition.ToIntVec3(), Map);
        strike.duration = 600;
        strike.instigator = launcher;
        strike.weaponDef = equipmentDef;
        strike.StartStrike();
        var emitter = launcher as Building_EnemyAnticraftEmitterCE;
        emitter.currentStrike = strike;
        //}
        base.Impact(hitThing, muzzle);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref strike, "strike");
    }
}
