namespace CombatExtended;

public class Verb_DryadTurretCE : Verb_ShootCE
{
    // Dryads Reworked uses Building_TurretGun's warmup. CE's repeat-shot code
    // expects a Building_TurretGunCE or a pawn's warmup stance.
    public override void RecalculateWarmupTicks() { }
}
