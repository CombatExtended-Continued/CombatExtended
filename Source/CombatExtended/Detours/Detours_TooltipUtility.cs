using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.Detours
{
    internal static class Detours_TooltipUtility
    {
        internal static string ShotCalculationTipString(Thing target)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (Find.Selector.SingleSelectedThing != null)
            {
                Verb verb = null;
                Verb_LaunchProjectileCE verbCE = null;
                Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
                if (pawn != null && pawn != target && pawn.equipment != null && pawn.equipment.Primary != null && pawn.equipment.PrimaryEq.PrimaryVerb is Verb_LaunchProjectile)
                {
                    verb = pawn.equipment.PrimaryEq.PrimaryVerb;
                }
                Building_TurretGun building_TurretGun = Find.Selector.SingleSelectedThing as Building_TurretGun;
                if (building_TurretGun != null && building_TurretGun != target)
                {
                    verb = building_TurretGun.AttackVerb;
                }
                if (verb != null)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append("ShotBy".Translate(new object[]
                    {
                        Find.Selector.SingleSelectedThing.LabelShort
                    }) + ": ");
                    if (verb.CanHitTarget(target))
                    {
                        stringBuilder.Append(ShotReport.HitReportFor(verb.caster, verb, target).GetTextReadout());
                    }
                    else
                    {
                        stringBuilder.Append("CannotHit".Translate());
                    }
                }
                // Append CE tooltip
                else
                {

                    if (pawn != null && pawn != target && pawn.equipment != null &&
                        pawn.equipment.Primary != null && pawn.equipment.PrimaryEq.PrimaryVerb is Verb_LaunchProjectileCE)
                    {
                        verbCE = pawn.equipment.PrimaryEq.PrimaryVerb as Verb_LaunchProjectileCE;
                    }
                    Building_TurretGun building_TurretGun2 = Find.Selector.SingleSelectedThing as Building_TurretGun;
                    if (building_TurretGun != null && building_TurretGun != target)
                    {
                        verbCE = building_TurretGun.AttackVerb as Verb_LaunchProjectileCE;
                    }
                    if (verbCE != null)
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.Append("ShotBy".Translate(new object[] { pawn.LabelShort }) + ":\n");
                        if (verbCE.CanHitTarget(target))
                        {
                            ShiftVecReport report = verbCE.ShiftVecReportFor(target);
                            stringBuilder.Append(report.GetTextReadout());
                        }
                        else
                        {
                            stringBuilder.Append("CannotHit".Translate());
                        }
                    }
                }
            }
            return stringBuilder.ToString();
        }
    }
}
