#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Projectiles/Projectile_AnticraftBeam.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

using CombatExtended.Compatibility.VGECompat;
using VanillaGravshipExpanded2;
using Verse;

namespace CombatExtended.Compatibility.VGE2Compat;

public class Projectile_AnticraftBeamCE : Projectile_ArtilleryBeamCE
{
    public AnticraftBeamStrike strike;

    // same code but we don't need blockedByShield
    public override void Impact(Thing hitThing)
    {
        //if (!blockedByShield)
        //{
            strike = (AnticraftBeamStrike)GenSpawn.Spawn(InternalDefOf.VGE_EnemyAnticraftBeamStrike, intendedTarget.Cell, Map);
            strike.duration = 600;
            strike.instigator = launcher;
            strike.weaponDef = equipmentDef;
            strike.StartStrike();
            var emitter = launcher as Building_EnemyAnticraftEmitter;
            emitter.currentStrike = strike;
        //}
        base.Impact(hitThing);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref strike, "strike");
    }
}
