﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class ShiftVecReport
    {
        public LocalTargetInfo target = null;
        public Pawn targetPawn
        {
            get
            {
                return target.Thing as Pawn;
            }
        }
        public float aimingAccuracy = 1f;
        public float sightsEfficiency = 1f;

        private float accuracyFactorInt = -1f;
        public float accuracyFactor
        {
            get
            {
                if (accuracyFactorInt < 0)
                {
                    accuracyFactorInt = (1.5f - aimingAccuracy) / sightsEfficiency;
                }
                return accuracyFactorInt;
            }
        }

        public float circularMissRadius = 0f;
        public float indirectFireShift = 0f;

        // Visibility variables
        public float lightingShift = 0f;
        public float weatherShift = 0f;

        private float enviromentShiftInt = -1;
        public float enviromentShift
        {
            get
            {
                if (enviromentShiftInt < 0)
                {
                    enviromentShiftInt = ((blindFiring ? 1 : lightingShift) * 3.5f + weatherShift * 1.5f) * CE_Utility.LightingRangeMultiplier(shotDist) + smokeDensity;
                }
                return enviromentShiftInt;
            }
        }


        private float visibilityShiftInt = -1f;
        public float visibilityShift
        {
            get
            {
                if (visibilityShiftInt < 0)
                {
                    float se = sightsEfficiency;
                    if (se < 0.02f)
                    {
                        se = 0.02f;
                    }
                    visibilityShiftInt = enviromentShift * (shotDist / 50 / se) * (2 - aimingAccuracy);
                }
                return visibilityShiftInt;
            }
        }

        // Leading variables
        public float shotSpeed = 0f;
        private bool targetIsMoving
        {
            get
            {
                return targetPawn != null && targetPawn.pather != null && targetPawn.pather.Moving && (targetPawn.stances.stunner == null || !targetPawn.stances.stunner.Stunned);
            }
        }
        private float leadDistInt = -1f;
        public float leadDist
        {
            get
            {
                if (leadDistInt < 0)
                {
                    if (targetIsMoving)
                    {
                        float targetSpeed = CE_Utility.GetMoveSpeed(targetPawn);
                        float timeToTarget = shotDist / shotSpeed;
                        leadDistInt = targetSpeed * timeToTarget;
                    }
                    else
                    {
                        leadDistInt = 0f;
                    }
                }
                return leadDistInt;
            }
        }
        public float leadShift
        {
            get
            {
                return leadDist * Mathf.Min(accuracyFactor * 0.25f, 2.5f)
                       + Mathf.Min((blindFiring ? 1 : lightingShift) * CE_Utility.LightingRangeMultiplier(shotDist) * leadDist * 0.25f, (blindFiring ? 100f : 2.0f))
                       + Mathf.Min((blindFiring ? 0 : smokeDensity) * 0.5f, 2.0f);
            }
        }

        // Range variables
        public float shotDist = 0f;
        public float maxRange;
        public float distShift
        {
            get
            {
                return shotDist * (shotDist / Math.Max(maxRange, 20)) * Mathf.Min(accuracyFactor * 0.5f, 0.8f);
            }
        }

        public bool isAiming = false;
        public float swayDegrees = 0f;
        public float spreadDegrees = 0f;
        public Thing cover = null;
        public float smokeDensity = 0f;
        public bool blindFiring = false;
        public bool roofed = false;

        // Copy-constructor
        public ShiftVecReport(ShiftVecReport report)
        {
            target = report.target;
            sightsEfficiency = report.sightsEfficiency;
            aimingAccuracy = report.aimingAccuracy;
            circularMissRadius = report.circularMissRadius;
            indirectFireShift = report.indirectFireShift;
            lightingShift = report.lightingShift;
            shotSpeed = report.shotSpeed;
            shotDist = report.shotDist;
            maxRange = report.maxRange;
            isAiming = report.isAiming;
            swayDegrees = report.swayDegrees;
            spreadDegrees = report.spreadDegrees;
            cover = report.cover;
            smokeDensity = report.smokeDensity;
            blindFiring = report.blindFiring;
            roofed = report.roofed;
        }

        public ShiftVecReport()
        {
        }

        public Vector2 GetRandCircularVec()
        {
            Vector2 vec = CE_Utility.GenRandInCircle(visibilityShift + circularMissRadius + indirectFireShift);
            return vec;
        }

        public float GetRandDist()
        {
            float dist = shotDist + Rand.Range(-distShift, distShift);
            return dist;
        }

        public Vector2 GetRandLeadVec()
        {
            if (blindFiring)
            {
                return new Vector2(0, 0);
            }
            Vector3 moveVec = new Vector3();
            if (targetIsMoving)
            {
                moveVec = (targetPawn.pather.nextCell - targetPawn.Position).ToVector3() * (leadDist + Rand.Range(-leadShift, leadShift));
            }
            return new Vector2(moveVec.x, moveVec.z);
        }

        /// <returns>Angle Vector2 in degrees</returns>
        public Vector2 GetRandSpreadVec()
        {
            Vector2 vec = Rand.InsideUnitCircle * spreadDegrees;
            return vec;
        }

        public static string AsPercent(float pct)
        {
            return Mathf.RoundToInt(100f * pct) + "%";
        }

        public string GetTextReadout()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("   " + "CE_VisibilityError".Translate() + "\t" + GenText.ToStringByStyle(visibilityShift, ToStringStyle.FloatTwo) + " " + "CE_cells".Translate());

            if (Controller.settings.DebuggingMode)
            {
                stringBuilder.AppendLine("   " + $"DEBUG: visibilityShift\t\t{visibilityShift} ");
                stringBuilder.AppendLine("   " + $"DEBUG: leadDist\t\t{leadDist} ");
                stringBuilder.AppendLine("   " + $"DEBUG: enviromentShift\t{enviromentShift}");
                stringBuilder.AppendLine("   " + $"DEBUG: accuracyFactor\t{accuracyFactor}");
                stringBuilder.AppendLine("   " + $"DEBUG: circularMissRadius\t{circularMissRadius}");
                stringBuilder.AppendLine("   " + $"DEBUG: sightsEfficiency\t{sightsEfficiency}");
                stringBuilder.AppendLine("   " + $"DEBUG: weathershift\t\t{weatherShift}");
                stringBuilder.AppendLine("   " + $"DEBUG: accuracyFactor\t\t{accuracyFactor}");
                stringBuilder.AppendLine("   " + $"DEBUG: lightingShift\t\t{lightingShift}");
            }

            if (lightingShift > 0)
            {
                stringBuilder.AppendLine("      " + "Darkness".Translate() + "\t" + AsPercent(lightingShift));
            }
            if (weatherShift > 0)
            {
                stringBuilder.AppendLine("      " + "Weather".Translate() + "\t" + AsPercent(weatherShift));
            }
            if (smokeDensity > 0)
            {
                stringBuilder.AppendLine("      " + "CE_SmokeDensity".Translate() + "\t" + AsPercent(smokeDensity));
            }
            if (leadShift > 0)
            {
                stringBuilder.AppendLine("   " + "CE_LeadError".Translate() + "\t" + GenText.ToStringByStyle(leadShift, ToStringStyle.FloatTwo) + " " + "CE_cells".Translate());
            }
            if (distShift > 0)
            {
                stringBuilder.AppendLine("   " + "CE_RangeError".Translate() + "\t" + GenText.ToStringByStyle(distShift, ToStringStyle.FloatTwo) + " " + "CE_cells".Translate());
            }
            if (swayDegrees > 0)
            {
                stringBuilder.AppendLine("   " + "CE_Sway".Translate() + "\t\t" + GenText.ToStringByStyle(swayDegrees, ToStringStyle.FloatTwo) + "CE_degrees".Translate());
            }
            if (spreadDegrees > 0)
            {
                stringBuilder.AppendLine("   " + "CE_Spread".Translate() + "\t\t" + GenText.ToStringByStyle(spreadDegrees, ToStringStyle.FloatTwo) + "CE_degrees".Translate());
            }
            // Don't display cover and target size if our weapon has a CEP
            if (circularMissRadius > 0)
            {
                stringBuilder.AppendLine("   " + "CE_MissRadius".Translate() + "\t" + GenText.ToStringByStyle(circularMissRadius, ToStringStyle.FloatTwo) + " " + "CE_cells".Translate());
                if (indirectFireShift > 0)
                {
                    stringBuilder.AppendLine("   " + "CE_IndirectFire".Translate() + "\t" + GenText.ToStringByStyle(indirectFireShift, ToStringStyle.FloatTwo) + " " + "CE_cells".Translate());
                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_MortarDirectFire, KnowledgeAmount.FrameDisplayed); // Show we learned about indirect fire penalties
                }
            }
            else
            {
                if (cover != null)
                {
                    stringBuilder.AppendLine("   " + "CE_CoverHeight".Translate() + "\t" + new CollisionVertical(cover).Max * CollisionVertical.MeterPerCellHeight + " " + "CE_meters".Translate());
                }
                if (target.Thing != null)
                {
                    stringBuilder.AppendLine("   " + "CE_TargetHeight".Translate() + "\t" + GenText.ToStringByStyle(new CollisionVertical(target.Thing).HeightRange.Span * CollisionVertical.MeterPerCellHeight, ToStringStyle.FloatTwo) + " " + "CE_meters".Translate());
                    stringBuilder.AppendLine("   " + "CE_TargetWidth".Translate() + "\t" + GenText.ToStringByStyle(CE_Utility.GetCollisionWidth(target.Thing) * CollisionVertical.MeterPerCellHeight, ToStringStyle.FloatTwo) + " " + "CE_meters".Translate());
                    var pawn = target.Thing as Pawn;
                    if (pawn != null && pawn.IsCrouching())
                    {
                        LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_Crouching, OpportunityType.GoodToKnow);
                    }
                }
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_AimingSystem, KnowledgeAmount.FrameDisplayed); // Show we learned about the aiming system
            }
            return stringBuilder.ToString();
        }
    }
}
