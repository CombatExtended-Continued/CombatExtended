using RimWorld;
using Verse;

namespace CombatExtended;

public class CompProperties_AbilityExplosionCE : CompProperties_AbilityExplosion
{
    public bool ignoreSelf;
    public SimpleCurve falloffCurveOverride;

    public CompProperties_AbilityExplosionCE()
    {
        compClass = typeof(CompAbilityEffect_ExplosionCE);
    }
}
