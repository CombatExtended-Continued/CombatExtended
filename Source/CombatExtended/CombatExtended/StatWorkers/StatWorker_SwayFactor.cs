using System.Drawing;
using System.Text;
using Verse;
using RimWorld;

namespace CombatExtended;

public class StatWorker_SwayFactor : StatWorker
{
    private float CalculateRequiredSkill(float swayFactor, float benchmarkSwayAmplitude, out float weaponHandling)
    {
        weaponHandling = -((benchmarkSwayAmplitude / swayFactor) - 4.5f);

        float shootingPoints = StatDefOf.ShootingAccuracyPawn.postProcessCurve.EvaluateInverted(weaponHandling);

        if (StatDefOf.ShootingAccuracyPawn.skillNeedOffsets.Find(x => x is SkillNeed_BaseBonus) is SkillNeed_BaseBonus baseStats)
        {
            return (shootingPoints - baseStats.baseValue) / baseStats.bonusPerLevel;
        }

        return 0f;
    }


    public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(base.GetExplanationFinalizePart(req, numberSense, finalVal));

        float benchmarkSwayAmplitude = 3.0f;
        float weaponHandling = 0f;
        float skillLevel = CalculateRequiredSkill(finalVal, benchmarkSwayAmplitude, out weaponHandling);
        stringBuilder.AppendLine("\n\n" + "CE_StatWorker_Sway_Explanation".Translate(finalVal.ToStringByStyle(ToStringStyle.FloatTwo),
                                            benchmarkSwayAmplitude)
                                        + "\n" + skillLevel.ToStringByStyle(ToStringStyle.FloatOne)
                                        + " (" + weaponHandling.ToStringByStyle(ToStringStyle.PercentZero) + " " +
                                        StatDefOf.ShootingAccuracyPawn.label + ")");

        return stringBuilder.ToString().TrimEndNewlines();
    }
}
