using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CombatExtended
{
    /* Copied from Verb_MeleeAttack
     *
     * Added dodge/parry mechanics, crits, height check.
     *
     * Unmodified methods should be kept up-to-date with vanilla between Alphas (at least as far as logic is concerned) so long as they don't interfere with our own.
     * Please tag changes you're making from vanilla.
     *
     * -NIA
     */
    public class Verb_MeleeAttackCE : Verb_MeleeAttack
    {
        #region Constants

        private const float DefaultHitChance = 0.6f;
        private const float ShieldBlockChance = 0.75f;   // If we have a shield equipped, this is the chance a parry will be a shield block
        private const int KnockdownDuration = 120;   // Animal knockdown lasts for this long

        private const float KnockdownMassRequirement = 5f;

        // XP variables
        private const float HitXP = 200;    // Vanilla is 250
        private const float DodgeXP = 50;
        private const float ParryXP = 50;
        private const float CritXP = 100;

        /* Base stats
         *
         * These are the baseline stats we want for crit/dodge/parry for pawns of equal skill. These need to be the same as set in the base factors set in the
         * stat defs. Ideally we would access them from the defs but the relevant values are all set to private and there is no real way to get at them that I
         * can see.
         *
         * -NIA
         */

        private const float BaseCritChance = 0.1f;
        private const float BaseDodgeChance = 0.1f;
        private const float BaseParryChance = 0.2f;

        #endregion

        #region Properties

        public ToolCE ToolCE => (tool as ToolCE);

        public static Verb_MeleeAttackCE LastAttackVerb
        {
            get;    // Hack to get around DamageInfo not passing the tool to ArmorUtilityCE
            private set;
        }

        protected virtual float PenetrationSkillMultiplier => 1f + (CasterPawn?.skills?.GetSkill(SkillDefOf.Melee).Level ?? 1 - 1) * StatWorker_MeleeArmorPenetration.skillFactorPerLevel;
        protected virtual float PenetrationOtherMultipliers => CasterIsPawn ? Mathf.Pow(this.verbProps.GetDamageFactorFor(this, CasterPawn), StatWorker_MeleeArmorPenetration.powerForOtherFactors) : 1f;
        public float ArmorPenetrationSharp => (tool as ToolCE)?.armorPenetrationSharp * PenetrationOtherMultipliers * PenetrationSkillMultiplier * (EquipmentSource?.GetStatValue(CE_StatDefOf.MeleePenetrationFactor) ?? 1) ?? 0;
        public float ArmorPenetrationBlunt => (tool as ToolCE)?.armorPenetrationBlunt * PenetrationOtherMultipliers * PenetrationSkillMultiplier * (EquipmentSource?.GetStatValue(CE_StatDefOf.MeleePenetrationFactor) ?? 1) ?? 0;

        public bool Enabled
        {
            get
            {
                /*
                 * Required in order to disable verbs that are dependent on attachments.
                 */
                if (ToolCE?.requiredAttachment != null && CasterIsPawn && CasterPawn?.equipment?.Primary is WeaponPlatform weapon)
                {
                    AttachmentLink[] links = weapon.CurLinks;
                    for (int i = 0; i < links.Length; i++)
                    {
                        if (links[i].attachment == ToolCE.requiredAttachment)
                        {
                            return true;
                        }
                    }
                    return false;
                }
                return true;
            }
        }

        bool isCrit;

        /// <summary>
        /// Backing field for <see cref="CompMeleeTargettingGizmo"/>.
        /// </summary>
        private CompMeleeTargettingGizmo compMeleeTargettingGizmo;
        /// <summary>
        /// Whether <see cref="compMeleeTargettingGizmo"/> was initialized and is up to date for the current tick.
        /// </summary>
        private bool meleeTargettingInitialized;

        #endregion

        #region Methods

        public override bool Available()
        {
            return Enabled && base.Available();
        }

        public override bool IsUsableOn(Thing target)
        {
            return Enabled && base.IsUsableOn(target);
        }

        /// <summary>
        /// Obtain the melee targetting gizmo for the current caster, if applicable.
        /// </summary>
        public CompMeleeTargettingGizmo CompMeleeTargettingGizmo
        {
            get
            {
                if (!meleeTargettingInitialized)
                {
                    meleeTargettingInitialized = true;
                    // Disable melee targeting in social fights to reduce the chance of a lethal outcome
                    if (CasterIsPawn && CasterPawn.MentalStateDef != MentalStateDefOf.SocialFighting)
                    {
                        compMeleeTargettingGizmo = CasterPawn.TryGetComp<CompMeleeTargettingGizmo>();
                    }
                }

                return compMeleeTargettingGizmo;
            }
        }

        /// <summary>
        /// Clear out the cached <see cref="CompMeleeTargettingGizmo"/> for the caster.
        /// </summary>
        public override void WarmupComplete()
        {
            meleeTargettingInitialized = false;
            base.WarmupComplete();
        }

        /// <summary>
        /// Performs the actual melee attack part. Awards XP, calculates and applies whether an attack connected and the outcome.
        /// </summary>
        /// <returns>True if the attack connected, false otherwise</returns>
        public override bool TryCastShot()
        {
            Pawn casterPawn = CasterPawn;
            if (casterPawn.stances.FullBodyBusy)
            {
                return false;
            }
            Thing targetThing = currentTarget.Thing;
            if (!CanHitTarget(targetThing))
            {
                Log.Warning(string.Concat(new object[]
                {
                    casterPawn,
                    " meleed ",
                    targetThing,
                    " from out of melee position."
                }));
            }
            casterPawn.rotationTracker.Face(targetThing.DrawPos);

            // Award XP as per vanilla
            bool targetImmobile = IsTargetImmobile(currentTarget);
            if (!targetImmobile && casterPawn.skills != null)
            {
                casterPawn.skills.Learn(SkillDefOf.Melee, HitXP * verbProps.AdjustedFullCycleTime(this, casterPawn), false);
            }

            //init target info for sfx here, where we're sure the target still exists.
            TargetInfo targInfo = new TargetInfo(targetThing.PositionHeld, targetThing.MapHeld);

            // Hit calculations
            bool result;
            string moteText = "";
            SoundDef soundDef;
            Pawn defender = targetThing as Pawn;
            //var hitRoll = Rand.Value;
            if (Rand.Chance(GetHitChance(targetThing)))
            {
                // Check for dodge
                if (!targetImmobile && !surpriseAttack && Rand.Chance(defender.GetStatValue(StatDefOf.MeleeDodgeChance)))
                {
                    // Attack is evaded
                    result = false;
                    soundDef = SoundMiss();
                    CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesDodge, false);

                    moteText = "TextMote_Dodge".Translate();
                    defender.skills?.Learn(SkillDefOf.Melee, DodgeXP * verbProps.AdjustedFullCycleTime(this, casterPawn), false);
                }
                else
                {
                    // Attack connects, calculate resolution
                    //var resultRoll = Rand.Value;
                    var counterParryBonus = 1 + (EquipmentSource?.GetStatValue(CE_StatDefOf.MeleeCounterParryBonus) ?? 0);
                    var parryChance = GetComparativeChanceAgainst(defender, casterPawn, CE_StatDefOf.MeleeParryChance, BaseParryChance, counterParryBonus);
                    if (!surpriseAttack && defender != null && CanDoParry(defender) && Rand.Chance(parryChance))
                    {
                        var deflectChance = GetComparativeChanceAgainst(defender, casterPawn, CE_StatDefOf.MeleeCritChance, BaseCritChance);
                        // Attack is parried
                        Apparel shield = defender.apparel.WornApparel.FirstOrDefault(x => x is Apparel_Shield);
                        bool isShieldBlock = shield != null && Rand.Chance(ShieldBlockChance);
                        Thing parryThing = isShieldBlock ? shield
                                           : defender.equipment?.Primary ?? defender;

                        bool deflected = Rand.Chance(deflectChance);
                        if (Rand.Chance(deflectChance))
                        {
                            // Do a riposte
                            DoParry(defender, parryThing, true, deflected);
                            moteText = "CE_TextMote_Riposted".Translate();
                            CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesDeflect, false); //placeholder

                            defender.skills?.Learn(SkillDefOf.Melee, (CritXP + ParryXP) * verbProps.AdjustedFullCycleTime(this, casterPawn), false);
                        }
                        else
                        {
                            // Do a parry
                            DoParry(defender, parryThing, false, deflected);
                            moteText = "CE_TextMote_Blocked".Translate();
                            if (deflected)
                            {
                                moteText = "CE_TextMote_Parried".Translate();
                            }
                            CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesMiss, false); //placeholder

                            defender.skills?.Learn(SkillDefOf.Melee, ParryXP * verbProps.AdjustedFullCycleTime(this, casterPawn), false);
                        }

                        result = false;
                        soundDef = SoundMiss(); // TODO Set hit sound to something more appropriate
                    }
                    else
                    {
                        BattleLogEntry_MeleeCombat log = CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesHit, false);

                        // Attack connects
                        DamageWorker.DamageResult damageResult;
                        if (surpriseAttack || Rand.Chance(GetComparativeChanceAgainst(casterPawn, defender, CE_StatDefOf.MeleeCritChance, BaseCritChance)))
                        {
                            // Do a critical hit
                            isCrit = true;
                            damageResult = ApplyMeleeDamageToTarget(currentTarget);
                            moteText = casterPawn.def.race.Animal ? null : "CE_TextMote_CriticalHit".Translate();
                            casterPawn.skills?.Learn(SkillDefOf.Melee, CritXP * verbProps.AdjustedFullCycleTime(this, casterPawn), false);
                        }
                        else
                        {
                            // Do a regular hit as per vanilla
                            damageResult = ApplyMeleeDamageToTarget(currentTarget);
                        }

                        damageResult.AssociateWithLog(log);
                        if (defender != null && damageResult.totalDamageDealt > 0f)
                        {
                            this.ApplyMeleeSlaveSuppression(defender, damageResult.totalDamageDealt);
                        }

                        result = true;
                        soundDef = targetThing.def.category == ThingCategory.Building ? SoundHitBuilding() : SoundHitPawn();
                    }
                }
            }
            else
            {
                // Attack missed
                result = false;
                soundDef = SoundMiss();
                CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesMiss, false);
            }

            if (!moteText.NullOrEmpty())
            {
                MoteMakerCE.ThrowText(targetThing.PositionHeld.ToVector3Shifted(), targetThing.MapHeld, moteText);
            }

            if (targInfo.Map != null)
            {
                soundDef.PlayOneShot(targInfo);
            }

            // The caster could be dead at this point due to a successful riposte from the defender,
            // so check for that as appropriate.
            if (casterPawn.Spawned)
            {
                casterPawn.Drawer.Notify_MeleeAttackOn(targetThing);
            }

            // The defender may still be alive but not spawned at this point due to a side-effect of the melee attack,
            // such as the cocoon bite of giant spiders from VAE: Caves
            if (defender != null && !defender.Dead && defender.Spawned)
            {
                defender.stances.stagger.StaggerFor(95);
                if (casterPawn.MentalStateDef != MentalStateDefOf.SocialFighting || defender.MentalStateDef != MentalStateDefOf.SocialFighting)
                {
                    defender.mindState.meleeThreat = casterPawn;
                    defender.mindState.lastMeleeThreatHarmTick = Find.TickManager.TicksGame;
                }
            }

            casterPawn.rotationTracker?.FaceCell(targetThing.Position);
            casterPawn.caller?.Notify_DidMeleeAttack();

            return result;
        }
        /// <summary>
        /// Gets attacked body part height
        /// </summary>
        /// <returns></returns>
        public BodyPartHeight GetAttackedPartHeightCE()
        {
            var result = BodyPartHeight.Undefined;

            if (CompMeleeTargettingGizmo != null && this.CurrentTarget.Thing is Pawn pawn)
            {
                return CompMeleeTargettingGizmo.finalHeight(pawn);
            }

            return result;
        }

        /// <summary>
        /// Calculates primary DamageInfo from verb, as well as secondary DamageInfos to apply (i.e. surprise attack stun damage).
        /// Also calculates the maximum body height an attack can target, so we don't get rabbits biting out a colonist's eye or something.
        /// </summary>
        /// <param name="target">The target damage is to be applied to</param>
        /// <returns>Collection with primary DamageInfo, followed by secondary types</returns>
        private IEnumerable<DamageInfo> DamageInfosToApply(LocalTargetInfo target, bool isCrit = false)
        {
            //START 1:1 COPY Verb_MeleeAttack.DamageInfosToApply
            float damAmount = verbProps.AdjustedMeleeDamageAmount(this, CasterPawn);
            var critModifier = isCrit && verbProps.meleeDamageDef.armorCategory == DamageArmorCategoryDefOf.Sharp &&
                               !CasterPawn.def.race.Animal
                               ? 2
                               : 1;
            var armorPenetration = (verbProps.meleeDamageDef.armorCategory == DamageArmorCategoryDefOf.Sharp ? ArmorPenetrationSharp : ArmorPenetrationBlunt) * critModifier;
            DamageDef damDef = verbProps.meleeDamageDef;
            BodyPartGroupDef bodyPartGroupDef = null;
            HediffDef hediffDef = null;

            if (EquipmentSource != null && EquipmentSource != CasterPawn)
            {
                //crits force a max damage variation roll
                if (isCrit)
                {
                    damAmount *= StatWorker_MeleeDamage.GetDamageVariationMax(CasterPawn);
                }
                //melee weapon damage variation
                else
                {
                    damAmount *= Rand.Range(StatWorker_MeleeDamage.GetDamageVariationMin(CasterPawn), StatWorker_MeleeDamage.GetDamageVariationMax(CasterPawn));
                }
            }
            else if (!CE_StatDefOf.UnarmedDamage.Worker.IsDisabledFor(CasterPawn))  //ancient soldiers can punch even if non-violent, this prevents the disabled stat from being used
            {
                //unarmed damage bonus offset
                damAmount += CasterPawn.GetStatValue(CE_StatDefOf.UnarmedDamage);
            }

            if (CasterIsPawn)
            {
                bodyPartGroupDef = verbProps.AdjustedLinkedBodyPartsGroup(tool);
                if (damAmount >= 1f)
                {
                    if (HediffCompSource != null)
                    {
                        hediffDef = HediffCompSource.Def;
                    }
                }
                else
                {
                    damAmount = 1f;
                    damDef = DamageDefOf.Blunt;
                }
            }

            var source = EquipmentSource != null ? EquipmentSource.def : CasterPawn.def;
            Vector3 direction = (target.Thing.Position - CasterPawn.Position).ToVector3();
            DamageDef def = damDef;
            //END 1:1 COPY
            BodyPartHeight bodyRegion = GetAttackedPartHeightCE(); //Caula: Changed to the comp selector
            //GetBodyPartHeightFor(target);   //Custom // Add check for body height
            //START 1:1 COPY

            // Don't let players draft people and order them to punch/assault another pawn
            // as a gamey way of making them guilty for purposes of banishment and the like.
            var instigatorGuilty = !CasterPawn?.Drafted ?? true;
            DamageInfo damageInfo = new DamageInfo(def, damAmount, armorPenetration, -1f, caster, null, source, DamageInfo.SourceCategory.ThingOrUnknown, null, instigatorGuilty); //Alteration

            if (target.Thing is Pawn pawn)
            {
                // Predators get a neck bite on immobile targets
                if (caster.def.race.predator && IsTargetImmobile(target))
                {
                    //TODO: 1.5 Should be neck?
                    var hp = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Top, BodyPartDepth.Outside)
                               .FirstOrDefault(r => r.def == CE_BodyPartDefOf.Neck);
                    if (hp == null)
                    {
                        hp = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Top, BodyPartDepth.Outside)
                               .FirstOrDefault(r => r.def == BodyPartDefOf.Head);
                    }
                    damageInfo.SetHitPart(hp);
                }
                //for some reason, when all parts of height are missing their incode count is 3
                if (pawn.health.hediffSet.GetNotMissingParts(bodyRegion).Count() <= 3)
                {
                    bodyRegion = BodyPartHeight.Middle;
                }
                //specific part hits
                if ((CompMeleeTargettingGizmo?.SkillReqBP ?? false) && CompMeleeTargettingGizmo.targetBodyPart != null)
                {

                    // 50f might be too little, since it'd mean no hits are possible for some bodyparts at certain pawn level
                    float targetSkillDecrease = (pawn.skills?.GetSkill(SkillDefOf.Melee).Level ?? 0f) / 50f;

                    if (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) > 0f)
                    {
                        targetSkillDecrease *= pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving);
                    }
                    else
                    {
                        targetSkillDecrease = 0f;
                    }

                    var partToHit = pawn.health.hediffSet.GetNotMissingParts().Where(x => x.def == CompMeleeTargettingGizmo.targetBodyPart).FirstOrFallback();

                    if (Rand.Chance(CompMeleeTargettingGizmo.SkillBodyPartAttackChance(partToHit) - targetSkillDecrease))
                    {
                        damageInfo.SetHitPart(partToHit);
                    }
                }
            }



            //everything related to internal organ penetration
            BodyPartDepth finalDepth = BodyPartDepth.Outside;
            if (target.Thing is Pawn p)
            {

                if (damageInfo.Def.armorCategory == DamageArmorCategoryDefOf.Sharp && this.ToolCE.capacities.Any(y => y.GetModExtension<ModExtensionMeleeToolPenetration>()?.canHitInternal ?? false))
                {
                    if (Rand.Chance(damageInfo.Def.stabChanceOfForcedInternal))
                    {
                        if (ToolCE.armorPenetrationSharp > p.GetStatValueForPawn(StatDefOf.ArmorRating_Sharp, p))
                        {
                            finalDepth = BodyPartDepth.Inside;

                            if (damageInfo.HitPart != null)
                            {
                                var children = damageInfo.HitPart.GetDirectChildParts();
                                if (children.Count() > 0)
                                {
                                    damageInfo.SetHitPart(children.RandomElementByWeight(x => x.coverage));
                                }
                            }
                        }
                    }
                }
            }

            damageInfo.SetBodyRegion(bodyRegion, finalDepth);
            damageInfo.SetWeaponBodyPartGroup(bodyPartGroupDef);
            damageInfo.SetWeaponHediff(hediffDef);
            damageInfo.SetAngle(direction);
            yield return damageInfo;
            if (this.tool != null && this.tool.extraMeleeDamages != null)
            {
                foreach (ExtraDamage extraDamage in this.tool.extraMeleeDamages)
                {
                    if (Rand.Chance(extraDamage.chance))
                    {
                        damAmount = extraDamage.amount;
                        damAmount = Rand.Range(damAmount * 0.8f, damAmount * 1.2f);
                        var extraDamageInfo = new DamageInfo(extraDamage.def, damAmount, extraDamage.AdjustedArmorPenetration(this, this.CasterPawn), -1f, this.caster, null, source, DamageInfo.SourceCategory.ThingOrUnknown, null, instigatorGuilty);
                        extraDamageInfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                        extraDamageInfo.SetWeaponBodyPartGroup(bodyPartGroupDef);
                        extraDamageInfo.SetWeaponHediff(hediffDef);
                        extraDamageInfo.SetAngle(direction);

                        if (damageInfo.HitPart != null)
                        {
                            extraDamageInfo.SetHitPart(damageInfo.HitPart);
                        }

                        yield return extraDamageInfo;
                    }
                }
            }




            // Apply critical damage
            if (isCrit && !CasterPawn.def.race.Animal && verbProps.meleeDamageDef.armorCategory != DamageArmorCategoryDefOf.Sharp && target.Thing.def.race.IsFlesh)
            {
                var critAmount = GenMath.RoundRandom(damageInfo.Amount * 0.25f);
                var critDinfo = new DamageInfo(DamageDefOf.Stun, critAmount, armorPenetration,
                                               -1, caster, null, source, instigatorGuilty: instigatorGuilty);
                critDinfo.SetBodyRegion(bodyRegion, BodyPartDepth.Outside);
                critDinfo.SetWeaponBodyPartGroup(bodyPartGroupDef);
                critDinfo.SetWeaponHediff(hediffDef);
                critDinfo.SetAngle(direction);
                yield return critDinfo;
            }
        }

        // center mass has the standard change to hit, rest is much lowered
        private float GetHitChance(LocalTargetInfo target)
        {
            if (surpriseAttack)
            {
                return 1f;
            }
            if (IsTargetImmobile(target))
            {
                return 1f;
            }
            if (CasterPawn.skills != null)
            {
                float chance = CasterPawn.GetStatValue(StatDefOf.MeleeHitChance, true);

                switch (GetAttackedPartHeightCE())
                {
                    case BodyPartHeight.Bottom:
                        chance *= 0.8f;
                        break;
                    case BodyPartHeight.Middle:
                        break;
                    case BodyPartHeight.Undefined:
                        break;
                    case BodyPartHeight.Top:
                        chance *= 0.7f;
                        break;
                }

                return chance;
            }
            return DefaultHitChance;
        }

        /// <summary>
        /// Applies all DamageInfosToApply to the target. Increases damage on critical hits.
        /// </summary>
        /// <param name="target">Target to apply damage to</param>
        public override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            DamageWorker.DamageResult result = new DamageWorker.DamageResult();
            IEnumerable<DamageInfo> damageInfosToApply = DamageInfosToApply(target, isCrit);
            bool isHeadHit = false;
            float totalDmg = 0;
            foreach (DamageInfo current in damageInfosToApply)
            {
                if (target.ThingDestroyed)
                {
                    break;
                }

                if (current.Height == BodyPartHeight.Top)
                {
                    isHeadHit = true;
                }

                LastAttackVerb = this;
                result = target.Thing.TakeDamage(current);
                totalDmg += result.totalDamageDealt;
                LastAttackVerb = null;
            }
            // Apply animal knockdown
            if (isCrit && CasterPawn.def.race.Animal)
            {
                var pawn = target.Thing as Pawn;

                float equivalentTargetWeight = pawn.GetStatValue(StatDefOf.Mass);
                RacePropertiesExtensionCE bodyShape = pawn.def.GetModExtension<RacePropertiesExtensionCE>();
                if (bodyShape != null)
                {
                    equivalentTargetWeight *= (bodyShape.bodyShape.width / bodyShape.bodyShape.height);
                }
                if (isHeadHit)
                {
                    equivalentTargetWeight *= 0.5f;
                }

                // an attacker have to be heavier that 1/mass requirement of target equivalent mass to have a chance to knock target down, and as attacker mass approaches equivalent mass, knock down chance increases
                if (pawn != null && !pawn.Dead && Rand.Chance((CasterPawn.GetStatValue(StatDefOf.Mass) / equivalentTargetWeight) - (1 / (KnockdownMassRequirement - 1))))
                {
                    MoteMakerCE.ThrowText(pawn.PositionHeld.ToVector3Shifted(), pawn.MapHeld, "CE_TextMote_Knockdown".Translate());

                    //pawn.stances?.stunner.StunFor(KnockdownDuration);
                    pawn.stances?.SetStance(new Stance_Cooldown(KnockdownDuration, pawn, null));
                    Job job = JobMaker.MakeJob(CE_JobDefOf.WaitKnockdown);
                    job.expiryInterval = KnockdownDuration;
                    pawn.jobs?.StartJob(job, JobCondition.InterruptForced, null, false, false);
                }
            }
            isCrit = false;
            result.totalDamageDealt = totalDmg;
            return result;
        }

        /// <summary>
        /// Checks ParryTracker for whether the specified pawn can currently perform a parry.
        /// </summary>
        /// <param name="pawn">Pawn to check</param>
        /// <returns>True if pawn still has parries available or no parry tracker could be found, false otherwise</returns>
        private bool CanDoParry(Pawn pawn)
        {
            if (pawn == null
                    || pawn.Dead
                    || !pawn.RaceProps.Humanlike
                    || pawn.WorkTagIsDisabled(WorkTags.Violent)
                    || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
                    || IsTargetImmobile(pawn)
                    || pawn.MentalStateDef == MentalStateDefOf.SocialFighting)
            {
                return false;
            }
            ParryTracker tracker = pawn.Map.GetComponent<ParryTracker>();
            if (tracker == null)
            {
                Log.Error("CE failed to find ParryTracker to check pawn " + pawn.ToString());
                return true;
            }
            return tracker.CheckCanParry(pawn);
        }

        /// <summary>
        /// Performs parry calculations. Deflects damage from defender-pawn onto parryThing, applying armor reduction in the process.
        /// On critical parry does a riposte against this verb's CasterPawn.
        /// </summary>
        /// <param name="defender">Pawn doing the parrying</param>
        /// <param name="parryThing">Thing used to parry the blow (weapon/shield)</param>
        /// <param name="isRiposte">Whether to do a riposte</param>
        /// <param name="deflectChance">Chance of the weapon taking no damage from parrying</param>
        private void DoParry(Pawn defender, Thing parryThing, bool isRiposte = false, bool deflected = false)
        {
            if (parryThing != null)
            {
                foreach (var dinfo in DamageInfosToApply(defender))
                {
                    if (!deflected)
                    {
                        LastAttackVerb = this;
                        ArmorUtilityCE.ApplyParryDamage(dinfo, parryThing);
                        LastAttackVerb = null;
                    }
                }
            }
            if (isRiposte)
            {
                SoundDef sound = null;
                if (parryThing is Apparel_Shield)
                {
                    // Ensure damage from a successful parry won't make the defender guilty if they were drafted by the player
                    bool instigatorGuilty = !defender.Drafted;
                    // Shield bash
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, 6, parryThing.GetStatValue(CE_StatDefOf.MeleePenetrationFactor), -1, defender, null, parryThing.def, instigatorGuilty: instigatorGuilty);
                    dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                    dinfo.SetAngle((CasterPawn.Position - defender.Position).ToVector3());
                    caster.TakeDamage(dinfo);
                    if (!parryThing.Stuff.stuffProps.soundMeleeHitBlunt.NullOrUndefined())
                    {
                        sound = parryThing.Stuff.stuffProps.soundMeleeHitBlunt;
                    }
                }
                else
                {
                    Verb_MeleeAttackCE verb = defender.meleeVerbs.TryGetMeleeVerb(caster) as Verb_MeleeAttackCE;
                    if (verb == null)
                    {
                        Log.Error("CE failed to get attack verb for riposte from Pawn " + defender.ToString());
                    }
                    else
                    {
                        verb.ApplyMeleeDamageToTarget(caster);
                        sound = verb.SoundHitPawn();
                    }
                }
                // Held, because the caster may have died and despawned
                sound?.PlayOneShot(new TargetInfo(caster.PositionHeld, caster.MapHeld));
            }
            // Register with parry tracker
            ParryTracker tracker = defender.MapHeld?.GetComponent<ParryTracker>();
            if (tracker == null)
            {
                Log.Error("CE failed to find ParryTracker to register pawn " + defender.ToString());
            }
            else
            {
                tracker.RegisterParryFor(defender, verbProps.AdjustedCooldownTicks(this, defender));
            }
        }

        private static float GetComparativeChanceAgainst(Pawn attacker, Pawn defender, StatDef stat, float baseChance, float defenderSkillMult = 1)
        {
            if (attacker == null || defender == null)
            {
                return 0;
            }
            var offSkill = stat.Worker.IsDisabledFor(attacker) ? 0 : attacker.GetStatValue(stat);
            var defSkill = stat.Worker.IsDisabledFor(defender) ? 0 : defender.GetStatValue(stat) * defenderSkillMult;
            var chance = Mathf.Clamp01(baseChance + offSkill - defSkill);
            return chance;
        }

        #endregion
    }
}
