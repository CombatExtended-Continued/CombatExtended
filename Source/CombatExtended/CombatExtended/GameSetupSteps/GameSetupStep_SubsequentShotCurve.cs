using Verse;

namespace CombatExtended;

public class GameSetupStep_SubsequentShotCurve : GameSetupStep
{
    public SimpleCurve subsequentShotRecoilCurve;
    public SimpleCurve subsequentShotMassCurve;

    public override int SeedPart => 58224852; // unused, but required

    public override void GenerateFresh()
    {
        Verb_ShootCE.SubsequentShotRecoilCurve = subsequentShotRecoilCurve;
        Verb_ShootCE.SubsequentShotMassCurve = subsequentShotMassCurve;
    }

    public override void GenerateFromScribe()
    {
        Verb_ShootCE.SubsequentShotRecoilCurve = subsequentShotRecoilCurve;
        Verb_ShootCE.SubsequentShotMassCurve = subsequentShotMassCurve;
    }
}
