using RimWorld;
using RimWorld.Planet;
using VanillaGravshipExpanded;
using Verse;

namespace CombatExtended.Compatibility.VGECompat;

#region License
// Any VGE Code used for compatibility has been taken from the following source
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Verbs/Verb_ShootWithWorldTargeting.cs
#endregion

public class Verb_ShootWithWorldTargetingCE : Verb_ShootMortarCE
{
    public Building_GravshipTurretCE Turret => (Building_GravshipTurretCE)caster;
  
    public override bool TryCastShot()
    {
        if (Turret == null || !Turret.Active || Turret.ManningPawn == null)
        {
            // Reset targets if turret there is no manning pawn at terminal
            Turret.ResetForcedTarget();
            Turret.ResetCurrentTarget();
            return false;
        }
        return base.TryCastShot();
    }
}

