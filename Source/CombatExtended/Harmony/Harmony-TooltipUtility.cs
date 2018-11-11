using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Harmony;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(TooltipUtility), "ShotCalculationTipString")]
    public class Harmony_TooltipUtility_ShotCalculationTipString_Patch
    {
        public static void Postfix(ref string __result, Thing target)
        {
            if (__result.NullOrEmpty() && Find.Selector.SingleSelectedThing != null)
            {
                // Create CE Tooltip
                StringBuilder stringBuilder = new StringBuilder();
                Verb_LaunchProjectileCE verbCE = null;
                Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
                if (pawn != null && pawn != target && pawn.equipment != null &&
                    pawn.equipment.Primary != null && pawn.equipment.PrimaryEq.PrimaryVerb is Verb_LaunchProjectileCE)
                {
                    verbCE = pawn.equipment.PrimaryEq.PrimaryVerb as Verb_LaunchProjectileCE;
                }
                Building_TurretGunCE turret = Find.Selector.SingleSelectedThing as Building_TurretGunCE;
                if (turret != null && turret != target)
                {
                    verbCE = turret.AttackVerb as Verb_LaunchProjectileCE;
                }
                if (verbCE != null)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append("ShotBy".Translate(Find.Selector.SingleSelectedThing.LabelShort) + ":\n");
                    string obstructReport;
                    if (verbCE.CanHitTarget(target, out obstructReport))
                    {
                        ShiftVecReport report = verbCE.ShiftVecReportFor(target);
                        stringBuilder.Append(report.GetTextReadout());
                    }
                    else
                    {
                        stringBuilder.Append("CannotHit".Translate());
                        if (!obstructReport.NullOrEmpty()) stringBuilder.Append(" " + obstructReport + ".");
                    }
                }
                __result = stringBuilder.ToString();
            }
        }
    }
}
