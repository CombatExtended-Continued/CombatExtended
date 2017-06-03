using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class DamageWorker_AddInjuryCE : DamageWorker
    {
        private struct LocalInjuryResult
        {
            public bool wounded;

            public bool headshot;

            public bool deflected;

            public BodyPartRecord lastHitPart;

            public float totalDamageDealt;

            public static DamageWorker_AddInjuryCE.LocalInjuryResult MakeNew()
            {
                return new DamageWorker_AddInjuryCE.LocalInjuryResult
                {
                    wounded = false,
                    headshot = false,
                    deflected = false,
                    lastHitPart = null,
                    totalDamageDealt = 0f
                };
            }
        }

        private const float SpreadDamageChance = 0.5f;

        public override float Apply(DamageInfo dinfo, Thing thing)
        {
            Pawn pawn = thing as Pawn;
            if (pawn == null)
            {
                return base.Apply(dinfo, thing);
            }
            return this.ApplyToPawn(dinfo, pawn);
        }

        private float ApplyToPawn(DamageInfo dinfo, Pawn pawn)
        {
            if (dinfo.Amount <= 0)
            {
                return 0f;
            }
            if (!DebugSettings.enablePlayerDamage && pawn.Faction == Faction.OfPlayer)
            {
                return 0f;
            }
            DamageWorker_AddInjuryCE.LocalInjuryResult localInjuryResult = DamageWorker_AddInjuryCE.LocalInjuryResult.MakeNew();
            Map mapHeld = pawn.MapHeld;
            if (dinfo.Def.spreadOut)
            {
                if (pawn.apparel != null)
                {
                    List<Apparel> wornApparel = pawn.apparel.WornApparel;
                    for (int i = wornApparel.Count - 1; i >= 0; i--)
                    {
                        this.CheckApplySpreadDamage(dinfo, wornApparel[i]);
                    }
                }
                if (pawn.equipment != null && pawn.equipment.Primary != null)
                {
                    this.CheckApplySpreadDamage(dinfo, pawn.equipment.Primary);
                }
                if (pawn.inventory != null)
                {
                    ThingOwner innerContainer = pawn.inventory.innerContainer;
                    for (int j = innerContainer.Count - 1; j >= 0; j--)
                    {
                        this.CheckApplySpreadDamage(dinfo, innerContainer[j]);
                    }
                }
            }
            if (!this.FragmentDamageForDamageType(dinfo, pawn, ref localInjuryResult))
            {
                this.ApplyDamageToPart(dinfo, pawn, ref localInjuryResult);
                this.CheckDuplicateSmallPawnDamageToPartParent(dinfo, pawn, ref localInjuryResult);
            }
            if (localInjuryResult.wounded)
            {
                DamageWorker_AddInjuryCE.PlayWoundedVoiceSound(dinfo, pawn);
                pawn.Drawer.Notify_DamageApplied(dinfo);
                DamageWorker_AddInjuryCE.InformPsychology(dinfo, pawn);
            }
            if (localInjuryResult.headshot && pawn.Spawned)
            {
                MoteMaker.ThrowText(new Vector3((float)pawn.Position.x + 1f, (float)pawn.Position.y, (float)pawn.Position.z + 1f), pawn.Map, "Headshot".Translate(), Color.white, -1f);
                if (dinfo.Instigator != null)
                {
                    Pawn pawn2 = dinfo.Instigator as Pawn;
                    if (pawn2 != null)
                    {
                        pawn2.records.Increment(RecordDefOf.Headshots);
                    }
                }
            }
            if (localInjuryResult.deflected)
            {
                if (pawn.health.deflectionEffecter == null)
                {
                    pawn.health.deflectionEffecter = EffecterDefOf.ArmorDeflect.Spawn();
                }
                pawn.health.deflectionEffecter.Trigger(pawn, pawn);
            }
            else if (mapHeld != null)
            {
                ImpactSoundUtility.PlayImpactSound(pawn, dinfo.Def.impactSoundType, mapHeld);
            }
            return localInjuryResult.totalDamageDealt;
        }

        private void CheckApplySpreadDamage(DamageInfo dinfo, Thing t)
        {
            if (dinfo.Def == DamageDefOf.Flame && !t.FlammableNow)
            {
                return;
            }
            if (UnityEngine.Random.value < 0.5f)
            {
                dinfo.SetAmount(Mathf.CeilToInt((float)dinfo.Amount * Rand.Range(0.35f, 0.7f)));
                t.TakeDamage(dinfo);
            }
        }

        private bool FragmentDamageForDamageType(DamageInfo dinfo, Pawn pawn, ref DamageWorker_AddInjuryCE.LocalInjuryResult result)
        {
            return dinfo.AllowDamagePropagation && dinfo.Amount >= 9 && dinfo.Def.spreadOut;
        }

        private void CheckDuplicateSmallPawnDamageToPartParent(DamageInfo dinfo, Pawn pawn, ref DamageWorker_AddInjuryCE.LocalInjuryResult result)
        {
            if (!dinfo.AllowDamagePropagation)
            {
                return;
            }
            if (result.lastHitPart != null && dinfo.Def.harmsHealth && result.lastHitPart != pawn.RaceProps.body.corePart && result.lastHitPart.parent != null && pawn.health.hediffSet.GetPartHealth(result.lastHitPart.parent) > 0f && dinfo.Amount >= 10 && pawn.HealthScale <= 0.5001f)
            {
                DamageInfo dinfo2 = dinfo;
                dinfo2.SetForcedHitPart(result.lastHitPart.parent);
                this.ApplyDamageToPart(dinfo2, pawn, ref result);
            }
        }

        private void ApplyDamageToPart(DamageInfo dinfo, Pawn pawn, ref DamageWorker_AddInjuryCE.LocalInjuryResult result)
        {
            BodyPartRecord exactPartFromDamageInfo = DamageWorker_AddInjuryCE.GetExactPartFromDamageInfo(dinfo, pawn);
            if (exactPartFromDamageInfo == null)
            {
                return;
            }
            bool involveArmor = !dinfo.InstantOldInjury;
            DamageInfo postArmorDinfo = dinfo;
            bool shieldAbsorbed = false;
            if (involveArmor)
            {
                postArmorDinfo = ArmorUtilityCE.GetAfterArmorDamage(dinfo, pawn, exactPartFromDamageInfo, out shieldAbsorbed);
                if (postArmorDinfo.ForceHitPart != null && exactPartFromDamageInfo != postArmorDinfo.ForceHitPart) exactPartFromDamageInfo = postArmorDinfo.ForceHitPart;   // If the shot was deflected, update our body part
            }

            // Vanilla code - apply hediff
            if ((double)postArmorDinfo.Amount < 0.001)
            {
                result.deflected = true;
                return;
            }
            HediffDef hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(postArmorDinfo.Def, pawn, exactPartFromDamageInfo);
            Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(hediffDefFromDamage, pawn, null);
            hediff_Injury.Part = exactPartFromDamageInfo;
            hediff_Injury.source = postArmorDinfo.WeaponGear;
            hediff_Injury.sourceBodyPartGroup = postArmorDinfo.WeaponBodyPartGroup;
            hediff_Injury.sourceHediffDef = postArmorDinfo.WeaponLinkedHediff;
            hediff_Injury.Severity = (float)postArmorDinfo.Amount;
            if (postArmorDinfo.InstantOldInjury)
            {
                HediffComp_GetsOld hediffComp_GetsOld = hediff_Injury.TryGetComp<HediffComp_GetsOld>();
                if (hediffComp_GetsOld != null)
                {
                    hediffComp_GetsOld.IsOld = true;
                }
                else
                {
                    Log.Error(string.Concat(new object[]
                    {
                        "Tried to create instant old injury on Hediff without a GetsOld comp: ",
                        hediffDefFromDamage,
                        " on ",
                        pawn
                    }));
                }
            }
            result.wounded = true;
            result.lastHitPart = hediff_Injury.Part;
            if (DamageWorker_AddInjuryCE.IsHeadshot(postArmorDinfo, hediff_Injury, pawn))
            {
                result.headshot = true;
            }
            if (postArmorDinfo.InstantOldInjury && (hediff_Injury.def.CompPropsFor(typeof(HediffComp_GetsOld)) == null || hediff_Injury.Part.def.oldInjuryBaseChance == 0f || hediff_Injury.Part.def.IsSolid(hediff_Injury.Part, pawn.health.hediffSet.hediffs) || pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(hediff_Injury.Part)))
            {
                return;
            }
            this.FinalizeAndAddInjury(pawn, hediff_Injury, postArmorDinfo, ref result);
            this.CheckPropagateDamageToInnerSolidParts(postArmorDinfo, pawn, hediff_Injury, involveArmor, ref result);
            this.CheckDuplicateDamageToOuterParts(postArmorDinfo, pawn, hediff_Injury, involveArmor, ref result);

            // Apply secondary damage
            if (!shieldAbsorbed)
            {
                var props = dinfo.WeaponGear?.projectile as ProjectilePropertiesCE;
                if (props != null && !props.secondaryDamage.NullOrEmpty() && dinfo.Def == props.damageDef)
                {
                    foreach (SecondaryDamage sec in props.secondaryDamage)
                    {
                        if (pawn.Dead) return;
                        var secDinfo = sec.GetDinfo(postArmorDinfo);
                        secDinfo.SetForcedHitPart(exactPartFromDamageInfo);
                        pawn.TakeDamage(secDinfo);
                    }
                }
            }
        }

        private void FinalizeAndAddInjury(Pawn pawn, Hediff_Injury injury, DamageInfo dinfo, ref DamageWorker_AddInjuryCE.LocalInjuryResult result)
        {
            this.CalculateOldInjuryDamageThreshold(pawn, injury);
            result.totalDamageDealt += Mathf.Min(injury.Severity, pawn.health.hediffSet.GetPartHealth(injury.Part));
            pawn.health.AddHediff(injury, null, new DamageInfo?(dinfo));
        }

        private void CalculateOldInjuryDamageThreshold(Pawn pawn, Hediff_Injury injury)
        {
            HediffComp_GetsOld comp = injury.TryGetComp<HediffComp_GetsOld>();
            if (comp == null)
            {
                return;
            }
            // No permanent injuries on solid parts, prosthetics or injuries that are already old
            if (injury.Part.def.IsSolid(injury.Part, pawn.health.hediffSet.hediffs) || pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(injury.Part) || injury.IsOld()) return;

            // Delicate parts get old instantly
            if (injury.Part.def.IsDelicate)
            {
                comp.oldDamageThreshold = injury.Severity;
                comp.IsOld = true;
            }
            // Check if injury is at least 10% of part health and make a random roll
            else if (injury.Severity >= injury.Part.def.GetMaxHealth(pawn) * 0.1f && injury.Severity > 1)
            {
                float getOldChance = injury.Part.def.oldInjuryBaseChance * comp.Props.becomeOldChance * Mathf.Pow((injury.Severity / injury.Part.def.GetMaxHealth(pawn)), 2);
                if (Rand.Value < getOldChance) comp.oldDamageThreshold = Rand.Range(1, injury.Severity);
            }
        }

        private void CheckPropagateDamageToInnerSolidParts(DamageInfo dinfo, Pawn pawn, Hediff_Injury injury, bool involveArmor, ref DamageWorker_AddInjuryCE.LocalInjuryResult result)
        {
            if (!dinfo.AllowDamagePropagation)
            {
                return;
            }
            if (Rand.Value >= HealthTunings.ChanceToAdditionallyDamageInnerSolidPart)
            {
                return;
            }
            if (dinfo.Def.hasChanceToAdditionallyDamageInnerSolidParts && !injury.Part.def.IsSolid(injury.Part, pawn.health.hediffSet.hediffs) && injury.Part.depth == BodyPartDepth.Outside)
            {
                IEnumerable<BodyPartRecord> source = from x in pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                                                     where x.parent == injury.Part && x.def.IsSolid(x, pawn.health.hediffSet.hediffs) && x.depth == BodyPartDepth.Inside
                                                     select x;
                BodyPartRecord part;
                if (source.TryRandomElementByWeight((BodyPartRecord x) => x.coverageAbs, out part))
                {
                    HediffDef hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, part);
                    Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(hediffDefFromDamage, pawn, null);
                    hediff_Injury.Part = part;
                    hediff_Injury.source = injury.source;
                    hediff_Injury.sourceBodyPartGroup = injury.sourceBodyPartGroup;
                    hediff_Injury.Severity = (float)(dinfo.Amount / 2);
                    /*
                    if (involveArmor)
                    {
                        hediff_Injury.Severity = (float)ArmorUtility.GetAfterArmorDamage(pawn, dinfo.Amount / 2, part, dinfo.Def);
                    }
                    */
                    if (hediff_Injury.Severity <= 0f)
                    {
                        return;
                    }
                    result.lastHitPart = hediff_Injury.Part;
                    this.FinalizeAndAddInjury(pawn, hediff_Injury, dinfo, ref result);
                }
            }
        }

        private void CheckDuplicateDamageToOuterParts(DamageInfo dinfo, Pawn pawn, Hediff_Injury injury, bool involveArmor, ref DamageWorker_AddInjuryCE.LocalInjuryResult result)
        {
            if (!dinfo.AllowDamagePropagation)
            {
                return;
            }
            if (dinfo.Def.harmAllLayersUntilOutside && injury.Part.depth == BodyPartDepth.Inside)
            {
                BodyPartRecord parent = injury.Part.parent;
                do
                {
                    if (pawn.health.hediffSet.GetPartHealth(parent) != 0f)
                    {
                        HediffDef hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, parent);
                        Hediff_Injury hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(hediffDefFromDamage, pawn, null);
                        hediff_Injury.Part = parent;
                        hediff_Injury.source = injury.source;
                        hediff_Injury.sourceBodyPartGroup = injury.sourceBodyPartGroup;
                        hediff_Injury.Severity = (float)dinfo.Amount;
                        /*
                        if (involveArmor)
                        {
                            //hediff_Injury.Severity = (float)ArmorUtility.GetAfterArmorDamage(pawn, dinfo.Amount, parent, dinfo.Def);
                        }
                        */
                        if (hediff_Injury.Severity <= 0f)
                        {
                            hediff_Injury.Severity = 1f;
                        }
                        result.lastHitPart = hediff_Injury.Part;
                        this.FinalizeAndAddInjury(pawn, hediff_Injury, dinfo, ref result);
                    }
                    if (parent.depth == BodyPartDepth.Outside)
                    {
                        break;
                    }
                    parent = parent.parent;
                }
                while (parent != null);
            }
        }

        private static void InformPsychology(DamageInfo dinfo, Pawn pawn)
        {
        }

        private static bool IsHeadshot(DamageInfo dinfo, Hediff_Injury injury, Pawn pawn)
        {
            return !dinfo.InstantOldInjury && injury.Part.groups.Contains(BodyPartGroupDefOf.FullHead) && dinfo.Def == DamageDefOf.Bullet;
        }

        internal static BodyPartRecord GetExactPartFromDamageInfo(DamageInfo dinfo, Pawn pawn)
        {
            if (dinfo.ForceHitPart != null)
            {
                return (from x in pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined)
                        where x == dinfo.ForceHitPart
                        select x).FirstOrDefault<BodyPartRecord>();
            }
            BodyPartRecord randomNotMissingPart = pawn.health.hediffSet.GetRandomNotMissingPart(dinfo.Def, dinfo.Height, dinfo.Depth);
            if (randomNotMissingPart == null)
            {
                Log.Warning("GetRandomNotMissingPart returned null (any part).");
            }
            return randomNotMissingPart;
        }

        private static void PlayWoundedVoiceSound(DamageInfo dinfo, Pawn pawn)
        {
            if (pawn.Dead)
            {
                return;
            }
            if (dinfo.InstantOldInjury)
            {
                return;
            }
            if (pawn.MapHeld == null)
            {
                return;
            }
            if (dinfo.Def.externalViolence)
            {
                LifeStageUtility.PlayNearestLifestageSound(pawn, (LifeStageAge ls) => ls.soundWounded, 1f);
            }
        }
    }
}
