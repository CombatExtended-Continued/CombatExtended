using RimWorld;
using RimWorld.Planet;
using VanillaGravshipExpanded;
using Verse;

namespace CombatExtended.Compatibility.VGECompat;

public class Verb_ShootWithVGETargeting : Verb_ShootMortarCE
{
    public override bool TryCastShot()
    {
        if (caster is Building_GravshipTurretCE Turret)
        {
            // Reset targets if turret there is no manning pawn at terminal
            if (!Turret.Active || Turret.ManningPawn == null || Turret.linkedTerminal == null)
            {
                Turret.ResetForcedTarget();
                Turret.ResetCurrentTarget();
                return false;
            }
        } 
        return base.TryCastShot();
    }
}

