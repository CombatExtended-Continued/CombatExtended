using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
    public class StatWorker_WeaponToughness : StatWorker
    {
        public const float parryChanceFactor = 5f; // Factor by which the weapon holder's skill affects the final weapon toughness

        public float GetHolderToughnessFactor(Pawn pawn)
        {
            StatDef parryStatDef = CE_StatDefOf.MeleeParryChance;

            float num = pawn.def.statBases.Find(x => x.stat == CE_StatDefOf.MeleeParryChance)?.value ?? parryStatDef.defaultBaseValue;

            // Literally just a decompiled StatWorker.GetValueUnfinalized with no mention of primary or apparel. There are so many factors to worry about that I may as well just copy over the code
            if (pawn.skills != null)
            {
                if (parryStatDef.skillNeedOffsets != null)
                {
                    for (int i = 0; i < parryStatDef.skillNeedOffsets.Count; i++)
                    {
                        num += parryStatDef.skillNeedOffsets[i].ValueFor(pawn);
                    }
                }
            }
            else
            {
                num += parryStatDef.noSkillOffset;
            }

            if (parryStatDef.capacityOffsets != null)
            {
                for (int j = 0; j < parryStatDef.capacityOffsets.Count; j++)
                {
                    PawnCapacityOffset pawnCapacityOffset = parryStatDef.capacityOffsets[j];
                    num += pawnCapacityOffset.GetOffset(pawn.health.capacities.GetLevel(pawnCapacityOffset.capacity));
                }
            }
            if (pawn.story != null)
            {
                for (int k = 0; k < pawn.story.traits.allTraits.Count; k++)
                {
                    num += pawn.story.traits.allTraits[k].OffsetOfStat(parryStatDef);
                }
            }
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int l = 0; l < hediffs.Count; l++)
            {
                HediffStage curStage = hediffs[l].CurStage;
                if (curStage != null)
                {
                    float num2 = curStage.statOffsets.GetStatOffsetFromList(parryStatDef);
                    if (num2 != 0f && curStage.statOffsetEffectMultiplier != null)
                    {
                        num2 *= pawn.GetStatValue(curStage.statOffsetEffectMultiplier);
                    }
                    num += num2;
                }
            }
            if (pawn.Ideo != null)
            {
                List<Precept> preceptsListForReading = pawn.Ideo.PreceptsListForReading;
                for (int m = 0; m < preceptsListForReading.Count; m++)
                {
                    if (preceptsListForReading[m].def.statOffsets != null)
                    {
                        float statOffsetFromList = preceptsListForReading[m].def.statOffsets.GetStatOffsetFromList(parryStatDef);
                        num += statOffsetFromList;
                    }
                }
                Precept_Role role = pawn.Ideo.GetRole(pawn);
                if (role != null && role.def.roleEffects != null)
                {
                    foreach (RoleEffect roleEffect in role.def.roleEffects)
                    {
                        RoleEffect_PawnStatOffset roleEffect_PawnStatOffset;
                        if ((roleEffect_PawnStatOffset = roleEffect as RoleEffect_PawnStatOffset) != null && roleEffect_PawnStatOffset.statDef == parryStatDef)
                        {
                            num += roleEffect_PawnStatOffset.modifier;
                        }
                    }
                }
            }
            if (pawn.story != null)
            {
                for (int num3 = 0; num3 < pawn.story.traits.allTraits.Count; num3++)
                {
                    num *= pawn.story.traits.allTraits[num3].MultiplierOfStat(parryStatDef);
                }
            }
            for (int num4 = 0; num4 < hediffs.Count; num4++)
            {
                HediffStage curStage2 = hediffs[num4].CurStage;
                if (curStage2 != null)
                {
                    float num5 = curStage2.statFactors.GetStatFactorFromList(parryStatDef);
                    if (Math.Abs(num5 - 1f) > float.Epsilon && curStage2.statFactorEffectMultiplier != null)
                    {
                        num5 = ScaleFactor(num5, pawn.GetStatValue(curStage2.statFactorEffectMultiplier));
                    }
                    num *= num5;
                }
            }
            if (pawn.Ideo != null)
            {
                List<Precept> preceptsListForReading2 = pawn.Ideo.PreceptsListForReading;
                for (int num6 = 0; num6 < preceptsListForReading2.Count; num6++)
                {
                    if (preceptsListForReading2[num6].def.statFactors != null)
                    {
                        float statFactorFromList = preceptsListForReading2[num6].def.statFactors.GetStatFactorFromList(parryStatDef);
                        num *= statFactorFromList;
                    }
                }
                Precept_Role role2 = pawn.Ideo.GetRole(pawn);
                if (role2 != null && role2.def.roleEffects != null)
                {
                    foreach (RoleEffect roleEffect2 in role2.def.roleEffects)
                    {
                        RoleEffect_PawnStatFactor roleEffect_PawnStatFactor;
                        if ((roleEffect_PawnStatFactor = roleEffect2 as RoleEffect_PawnStatFactor) != null && roleEffect_PawnStatFactor.statDef == parryStatDef)
                        {
                            num *= roleEffect_PawnStatFactor.modifier;
                        }
                    }
                }
            }
            num *= pawn.ageTracker.CurLifeStage.statFactors.GetStatFactorFromList(parryStatDef);

            CompAffectedByFacilities compAffectedByFacilities = pawn.TryGetComp<CompAffectedByFacilities>();
            if (compAffectedByFacilities != null)
            {
                num += compAffectedByFacilities.GetStatOffset(parryStatDef);
            }
            if (parryStatDef.statFactors != null)
            {
                for (int num9 = 0; num9 < parryStatDef.statFactors.Count; num9++)
                {
                    num *= pawn.GetStatValue(parryStatDef.statFactors[num9]);
                }
            }
            if (pawn.skills != null)
            {
                if (parryStatDef.skillNeedFactors != null)
                {
                    for (int num10 = 0; num10 < parryStatDef.skillNeedFactors.Count; num10++)
                    {
                        num *= parryStatDef.skillNeedFactors[num10].ValueFor(pawn);
                    }
                }
            }
            else
            {
                num *= parryStatDef.noSkillFactor;
            }
            if (parryStatDef.capacityFactors != null)
            {
                for (int num11 = 0; num11 < parryStatDef.capacityFactors.Count; num11++)
                {
                    PawnCapacityFactor pawnCapacityFactor = parryStatDef.capacityFactors[num11];
                    float factor = pawnCapacityFactor.GetFactor(pawn.health.capacities.GetLevel(pawnCapacityFactor.capacity));
                    num = Mathf.Lerp(num, num * factor, pawnCapacityFactor.weight);
                }
            }
            if (pawn.Inspired)
            {
                num += pawn.InspirationDef.statOffsets.GetStatOffsetFromList(parryStatDef);
                num *= pawn.InspirationDef.statFactors.GetStatFactorFromList(parryStatDef);
            }

            if (parryStatDef.parts != null)
            {
                for (int i = 0; i < parryStatDef.parts.Count; i++)
                {
                    parryStatDef.parts[i].TransformValue(StatRequest.For(pawn), ref num);
                }
            }

            if (parryStatDef.postProcessCurve != null)
            {
                num = parryStatDef.postProcessCurve.Evaluate(num);
            }

            if (parryStatDef.postProcessStatFactors != null)
            {
                for (int j = 0; j < parryStatDef.postProcessStatFactors.Count; j++)
                {
                    num *= pawn.GetStatValue(parryStatDef.postProcessStatFactors[j]);
                }
            }
            if (Find.Scenario != null)
            {
                num *= Find.Scenario.GetStatFactor(parryStatDef);
            }
            num = Mathf.Clamp(num, parryStatDef.minValue, parryStatDef.maxValue);

            return num * parryChanceFactor;
        }

        public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess)
        {
            var thing = req.Thing;
            if (thing != null)
            {
                // Since the material factor acts like a StatPart, if the holder's skill was taken into account first, it wouldn't affect stuffable weapons
                if (stat.parts != null)
                {
                    foreach (StatPart part in this.stat.parts)
                    {
                        part.TransformValue(req, ref val);
                    }
                }

                // Additional stuff multiplier
                if (thing.Stuff != null)
                {
                    val *= thing.Stuff.GetModExtension<StuffToughnessMultiplierExtensionCE>()?.toughnessMultiplier ?? 1f;
                }

                // Factors in the holder's skill
                var compEq = thing.TryGetComp<CompEquippable>();
                var holder = compEq?.Holder;
                if (holder?.equipment?.Primary == thing)
                {
                    val *= GetHolderToughnessFactor(holder);
                }
            }
        }

        public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
        {
            string result = "";

            if (this.stat.parts != null)
            {
                foreach (var part in this.stat.parts)
                {
                    if (!part.ExplanationPart(req).NullOrEmpty())
                    {
                        result += "\n" + part.ExplanationPart(req);
                    }
                }
            }

            if (req.Thing.Stuff != null)
            {
                result += "\n" + "CE_StatsReport_WeaponToughness_StuffEffect".Translate() + (req.Thing.Stuff.GetModExtension<StuffToughnessMultiplierExtensionCE>()?.toughnessMultiplier ?? 1f).ToStringPercent();
            }

            if (req.Thing != null)
            {
                if (req.Thing?.TryGetComp<CompEquippable>()?.Holder != null)
                {
                    result += "\n" + "CE_StatsReport_WeaponToughness_HolderEffect".Translate() + GetHolderToughnessFactor(req.Thing.TryGetComp<CompEquippable>().Holder).ToStringPercent();
                }
            }

            result += "\n" + "StatsReport_FinalValue".Translate() + ": " + stat.ValueToString(finalVal, stat.toStringNumberSense);

            return result;
        }

        public override bool ShouldShowFor(StatRequest req)
        {
            return Controller.settings.ShowExtraStats && req.HasThing && req.Thing.def.IsWeapon && base.ShouldShowFor(req);
        }
    }
}
