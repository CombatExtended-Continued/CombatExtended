using Verse;

namespace CombatExtended;

public class GameSetupStep_SubsequentShotCurve : GameSetupStep
{
    public SimpleCurve defaultSubsequentShotCurve;

    public override int SeedPart => 58224852; // unused, but required

    public override void GenerateFresh()
    {
        Verb_ShootCE.SubsequentShotCurve = defaultSubsequentShotCurve;
    }

    public override void GenerateFromScribe()
    {
        Verb_ShootCE.SubsequentShotCurve = defaultSubsequentShotCurve;
    }
}
