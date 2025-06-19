using Verse;

namespace CombatExtended;

public class GameSetupStep_ExplosionCurve : GameSetupStep
{
    public SimpleCurve defaultExplosionFalloffCurve;
    
    public override int SeedPart => 58224852; // unused, but required

    public override void GenerateFresh()
    {
        ExplosionCE.defaultCurve = defaultExplosionFalloffCurve;
    }

    public override void GenerateFromScribe()
    {
        ExplosionCE.defaultCurve = defaultExplosionFalloffCurve;
    }
}
