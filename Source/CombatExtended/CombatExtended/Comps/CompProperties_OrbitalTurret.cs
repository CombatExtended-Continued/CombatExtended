
using Verse;

namespace CombatExtended;

public class CompProperties_OrbitalTurret: CompProperties
{
    public float precisionBonusFactor = 20;
    public float rangeBonusFactor = 20;
    public bool isMarkMandatory = false;

    public CompProperties_OrbitalTurret()
    {
        compClass = typeof(CompOrbitalTurret);
    }
}
