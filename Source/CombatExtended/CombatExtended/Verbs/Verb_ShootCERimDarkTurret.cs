using Verse;

namespace CombatExtended;

public class Verb_ShootCERimDarkTurret : Verb_ShootCE
{
    private CompRimDarkSummonedTurret TurretComp => caster?.TryGetComp<CompRimDarkSummonedTurret>();

    public override bool Available()
    {
        TurretComp?.EnsureAmmoInitialized();
        return !(TurretComp?.Reloading ?? false) && (CompAmmo?.CanBeFiredNow ?? true) && base.Available();
    }

    public override bool TryCastShot()
    {
        TurretComp?.EnsureAmmoInitialized();
        return !(TurretComp?.Reloading ?? false) && base.TryCastShot();
    }
}
