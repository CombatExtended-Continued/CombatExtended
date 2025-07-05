using Verse;

namespace CombatExtended
{
    public class GameSetupStep_DarkShootingCurve : GameSetupStep
    {
        public SimpleCurve lightingShiftCurve;

        public override int SeedPart => 58224853; // unused, but required

        public override void GenerateFresh()
        {
            ShiftVecReport.LightingShiftCurve = lightingShiftCurve;
        }

        public override void GenerateFromScribe()
        {
            ShiftVecReport.LightingShiftCurve = lightingShiftCurve;
        }
    }
}
