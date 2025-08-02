using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace CombatExtended.HarmonyCE;
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
            Verb_MeleeAttackCE meleeVerbCE = null;
            Pawn pawn = selectedThing as Pawn;
            if (pawn != null && pawn != target && pawn.equipment != null &&
                    pawn.equipment.Primary != null)
            {
                if (pawn.equipment.PrimaryEq.PrimaryVerb is Verb_LaunchProjectileCE)
                {
                    verbCE = pawn.equipment.PrimaryEq.PrimaryVerb as Verb_LaunchProjectileCE;
                }
                if (pawn.equipment.PrimaryEq.PrimaryVerb is Verb_MeleeAttackCE)
                {
                    meleeVerbCE = pawn.equipment.PrimaryEq.PrimaryVerb as Verb_MeleeAttackCE;
                }

            }
            Building_TurretGunCE turret = selectedThing as Building_TurretGunCE;
            if (turret != null && turret != target)
            {
                verbCE = turret.AttackVerb as Verb_LaunchProjectileCE;
            }
            Pawn pawn2 = target as Pawn;
            //ranged tooltip
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
                    if (!obstructReport.NullOrEmpty())
                    {
                        stringBuilder.Append(" " + obstructReport + ".");
                    }
                }

                if (pawn2 != null && pawn2.Faction == null && !pawn2.InAggroMentalState && pawn2.AnimalOrWildMan())
                {
                    float manhunterOnDamageChance = PawnUtility.GetManhunterOnDamageChance(pawn2, selectedThing);

                    if (manhunterOnDamageChance > 0f)
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(string.Format("{0}: {1}", "ManhunterPerHit".Translate(), manhunterOnDamageChance.ToStringPercent()));
                    }
                }
            }
            //melee tooltip
            if (pawn != null && verbCE == null && pawn2 != null
                && (pawn.Drafted || pawn.Faction != Faction.OfPlayer))
            {
                //if pawn doesn't have a melee weapon equipped, find another source of melee verb
                if (meleeVerbCE == null)
                {
                    List<VerbEntry> verbs = pawn.meleeVerbs.GetUpdatedAvailableVerbsList(false);
                    foreach (var verbEntry in verbs)
                    {
                        if (verbEntry.verb.IsMeleeAttack || verbEntry.GetSelectionWeight(pawn2) > 0 && verbEntry.verb is Verb_MeleeAttackCE)
                        {
                            meleeVerbCE = verbEntry.verb as Verb_MeleeAttackCE;
                            break;
                        }
                    }
                }

                if (meleeVerbCE != null)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append("CE_AttackedBy".Translate(selectedThing.LabelShort, selectedThing) + ":\n");

                    stringBuilder.Append(meleeVerbCE.GetTextReadout(pawn, pawn2, pawn.equipment?.PrimaryEq));

                    if (pawn2.Faction == null && !pawn2.InAggroMentalState && pawn2.AnimalOrWildMan())
                    {
                        float manhunterOnDamageChance = PawnUtility.GetManhunterOnDamageChance(pawn2, selectedThing, 0f);

                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(string.Format("{0}: {1}", "ManhunterPerHit".Translate(),
                            manhunterOnDamageChance.ToStringPercent()));
                    }
                }
            }
            __result = stringBuilder.ToString();
        }
    }
}
