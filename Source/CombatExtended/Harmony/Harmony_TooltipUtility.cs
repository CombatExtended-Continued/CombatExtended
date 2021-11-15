using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(TooltipUtility), "ShotCalculationTipString")]
    public class Harmony_TooltipUtility_ShotCalculationTipString_Patch
    {
        public static void Postfix(ref string __result, Thing target)
        {
            var selectedThing = Find.Selector.SingleSelectedThing;
            if (__result.NullOrEmpty() && selectedThing != null)
            {
                // Create CE Tooltip
                StringBuilder stringBuilder = new StringBuilder();
                Verb_LaunchProjectileCE verbCE = null;
                Pawn pawn = selectedThing as Pawn;
                if (pawn != null && pawn != target && pawn.equipment != null &&
                    pawn.equipment.Primary != null && pawn.equipment.PrimaryEq.PrimaryVerb is Verb_LaunchProjectileCE)
                {
                    verbCE = pawn.equipment.PrimaryEq.PrimaryVerb as Verb_LaunchProjectileCE;
                }
                Building_TurretGunCE turret = selectedThing as Building_TurretGunCE;
                if (turret != null && turret != target)
                {
                    verbCE = turret.AttackVerb as Verb_LaunchProjectileCE;
                }
                if (verbCE != null)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append("ShotBy".Translate(selectedThing.LabelShort, selectedThing) + ":\n");
                    string obstructReport;
                    if (verbCE.CanHitTarget(target, out obstructReport))
                    {
                        ShiftVecReport report = verbCE.ShiftVecReportFor(new LocalTargetInfo(target));
                        stringBuilder.Append(report.GetTextReadout());
                    }
                    else
                    {
                        stringBuilder.Append("CannotHit".Translate());
                        if (!obstructReport.NullOrEmpty()) stringBuilder.Append(" " + obstructReport + ".");
                    }
                    Pawn pawn2 = target as Pawn;
                    if (pawn2 != null && pawn2.Faction == null && !pawn2.InAggroMentalState)
                    {
                        float manhunterOnDamageChance;
                        if (verbCE.IsMeleeAttack)
                        {
                            manhunterOnDamageChance = PawnUtility.GetManhunterOnDamageChance(pawn2, 0f, selectedThing);
                        }
                        else
                        {
                            manhunterOnDamageChance = PawnUtility.GetManhunterOnDamageChance(pawn2, selectedThing);
                        }
                        if (manhunterOnDamageChance > 0f)
                        {
                            stringBuilder.AppendLine();
                            stringBuilder.AppendLine(string.Format("{0}: {1}", "ManhunterPerHit".Translate(), manhunterOnDamageChance.ToStringPercent()));
                        }
                    }
                }
                __result = stringBuilder.ToString();
            }
        }
    }
}
