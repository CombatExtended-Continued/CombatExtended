using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class Recipe_RemoveOldHediff : Recipe_Surgery
    {
        private const float BaseSeverityReduction = 7.5f; // The base value for scar reduction
        private const float reductionVariance = 2.5f;     // Reduction is randomized by +- this value

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients)
        {
            if (CheckSurgeryFail(billDoer, pawn, ingredients, part)) return;
            TaleRecorder.RecordTale(TaleDefOf.DidSurgery, new object[] { billDoer, pawn });

            // Calculate how much damage we reduced
            float amountReduced = Mathf.Max(0, BaseSeverityReduction + Rand.Range(-reductionVariance, reductionVariance)) * billDoer.GetStatValue(StatDefOf.HealingQuality);

            // Find all scars
            List<Hediff> scars = pawn.health.hediffSet.hediffs.FindAll(h => h.Part == part && h.IsOld());
            foreach (Hediff scar in scars)
            {
                // Reduce scars until we run out of reduction
                if (scar.Severity <= amountReduced)
                {
                    amountReduced -= scar.Severity;
                    pawn.health.RemoveHediff(scar);
                    if (amountReduced <= 0) break;
                }
                else
                {
                    scar.Severity -= amountReduced;
                    break;
                }
            }
        }

        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs.FindAll(h => h.IsOld());
            List<BodyPartRecord> parts = new List<BodyPartRecord>();
            foreach(Hediff hediff in hediffs)
            {
                BodyPartRecord part = hediff.Part;
                if (pawn.RaceProps.IsFlesh && part.depth == BodyPartDepth.Outside && !part.def.IsDelicate && !parts.Contains(part)) parts.Add(part);
            }
            return parts;
        }
    }
}
