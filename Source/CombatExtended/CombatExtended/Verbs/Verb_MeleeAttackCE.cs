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

        private const int TargetCooldown = 50;
        private const float DefaultHitChance = 0.6f;
        private const float ShieldBlockChance = 0.75f;   // If we have a shield equipped, this is the chance a parry will be a shield block
        private const int KnockdownDuration = 120;   // Animal knockdown lasts for this long

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

        public static Verb_MeleeAttackCE LastAttackVerb { get; private set; }   // Hack to get around DamageInfo not passing the tool to ArmorUtilityCE

        public float ArmorPenetrationSharp => (tool as ToolCE)?.armorPenetrationSharp * (EquipmentSource?.GetStatValue(CE_StatDefOf.MeleePenetrationFactor) ?? 1) ?? 0;
        public float ArmorPenetrationBlunt => (tool as ToolCE)?.armorPenetrationBlunt * (EquipmentSource?.GetStatValue(CE_StatDefOf.MeleePenetrationFactor) ?? 1) ?? 0;

        bool isCrit;

        #endregion

        #region Methods

        /// <summary>
        /// Performs the actual melee attack part. Awards XP, calculates and applies whether an attack connected and the outcome.
        /// </summary>
        /// <returns>True if the attack connected, false otherwise</returns>
        protected override bool TryCastShot()
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
                        // Attack is parried
                        Apparel shield = defender.apparel.WornApparel.FirstOrDefault(x => x is Apparel_Shield);
                        bool isShieldBlock = shield != null && Rand.Chance(ShieldBlockChance);
                        Thing parryThing = isShieldBlock ? shield
                            : defender.equipment?.Primary ?? defender;

                        if (Rand.Chance(GetComparativeChanceAgainst(defender, casterPawn, CE_StatDefOf.MeleeCritChance, BaseCritChance)))
                        {
                            // Do a riposte
                            DoParry(defender, parryThing, true);
                            moteText = "CE_TextMote_Riposted".Translate();
                            CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesDeflect, false); //placeholder

                            defender.skills?.Learn(SkillDefOf.Melee, (CritXP + ParryXP) * verbProps.AdjustedFullCycleTime(this, casterPawn), false);
                        }
                        else
                        {
                            // Do a parry
                            DoParry(defender, parryThing);
                            moteText = "CE_TextMote_Parried".Translate();
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
                        if (surpriseAttack || Rand.Chance(GetComparativeChanceAgainst(casterPawn, defender, CE_StatDefOf.MeleeCritChance, BaseCritChance)))
                        {
                            // Do a critical hit
                            isCrit = true;
                            ApplyMeleeDamageToTarget(currentTarget).AssociateWithLog(log);
                            moteText = casterPawn.def.race.Animal ? "CE_TextMote_Knockdown".Translate() : "CE_TextMote_CriticalHit".Translate();
                            casterPawn.skills?.Learn(SkillDefOf.Melee, CritXP * verbProps.AdjustedFullCycleTime(this, casterPawn), false);
                        }
                        else
                        {
                            // Do a regular hit as per vanilla
                            ApplyMeleeDamageToTarget(currentTarget).AssociateWithLog(log);
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
                MoteMaker.ThrowText(targetThing.PositionHeld.ToVector3Shifted(), targetThing.MapHeld, moteText);
            soundDef.PlayOneShot(new TargetInfo(targetThing.PositionHeld, targetThing.MapHeld));
            casterPawn.Drawer.Notify_MeleeAttackOn(targetThing);
            if (defender != null && !defender.Dead)
            {
                defender.stances.StaggerFor(95);
                if (casterPawn.MentalStateDef != MentalStateDefOf.SocialFighting || defender.MentalStateDef != MentalStateDefOf.SocialFighting)
                {
                    defender.mindState.meleeThreat = casterPawn;
                    defender.mindState.lastMeleeThreatHarmTick = Find.TickManager.TicksGame;
                }
            }
            casterPawn.rotationTracker.FaceCell(targetThing.Position);
            if (casterPawn.caller != null)
            {
                casterPawn.caller.Notify_DidMeleeAttack();
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

            if (EquipmentSource != null)
            {
                //melee weapon damage variation
                damAmount *= Rand.Range(StatWorker_MeleeDamage.GetDamageVariationMin(CasterPawn), StatWorker_MeleeDamage.GetDamageVariationMax(CasterPawn));
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
            BodyPartHeight bodyRegion = BodyPartHeight.Undefined; //Alistaire: The GetBodyPartHeightFor code is completely broken and targets Bottom some 90% of the time! Therefore easiest fix to set to Vanilla (Undefined)
                                                                  //GetBodyPartHeightFor(target);   //Custom // Add check for body height
            //START 1:1 COPY
            DamageInfo damageInfo = new DamageInfo(def, damAmount, armorPenetration, -1f, caster, null, source, DamageInfo.SourceCategory.ThingOrUnknown, null); //Alteration

            // Predators get a neck bite on immobile targets
            if (caster.def.race.predator && IsTargetImmobile(target) && target.Thing is Pawn pawn)
            {
                var neck = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Top, BodyPartDepth.Outside)
                    .FirstOrDefault(r => r.def == BodyPartDefOf.Neck);
                damageInfo.SetHitPart(neck);
            }

            damageInfo.SetBodyRegion(bodyRegion);
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
                        var extraDamageInfo = new DamageInfo(extraDamage.def, damAmount, extraDamage.AdjustedArmorPenetration(this, this.CasterPawn), -1f, this.caster, null, source, DamageInfo.SourceCategory.ThingOrUnknown, null);
                        extraDamageInfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                        extraDamageInfo.SetWeaponBodyPartGroup(bodyPartGroupDef);
                        extraDamageInfo.SetWeaponHediff(hediffDef);
                        extraDamageInfo.SetAngle(direction);
                        yield return extraDamageInfo;
                    }
                }
            }

            // Apply critical damage
            if (isCrit && !CasterPawn.def.race.Animal && verbProps.meleeDamageDef.armorCategory != DamageArmorCategoryDefOf.Sharp && target.Thing.def.race.IsFlesh)
            {
                var critAmount = GenMath.RoundRandom(damageInfo.Amount * 0.25f);
                var critDinfo = new DamageInfo(DamageDefOf.Stun, critAmount, armorPenetration,
                    -1, caster, null, source);
                critDinfo.SetBodyRegion(bodyRegion, BodyPartDepth.Outside);
                critDinfo.SetWeaponBodyPartGroup(bodyPartGroupDef);
                critDinfo.SetWeaponHediff(hediffDef);
                critDinfo.SetAngle(direction);
                yield return critDinfo;
            }
        }

        // unmodified
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
                return CasterPawn.GetStatValue(StatDefOf.MeleeHitChance, true);
            }
            return DefaultHitChance;
        }

        /// <summary>
        /// Checks whether a target can move. Immobile targets include anything that's not a pawn and pawns that are lying down/incapacitated.
        /// Immobile targets don't award XP, nor do they benefit from dodge/parry and attacks against them always crit.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
		private bool IsTargetImmobile(LocalTargetInfo target)
        {
            Thing thing = target.Thing;
            Pawn pawn = thing as Pawn;
            return thing.def.category != ThingCategory.Pawn || pawn.Downed || pawn.GetPosture() != PawnPosture.Standing || pawn.stances.stunner.Stunned;    // Added check for stunned
        }

        /// <summary>
        /// Applies all DamageInfosToApply to the target. Increases damage on critical hits.
        /// </summary>
        /// <param name="target">Target to apply damage to</param>
		protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            DamageWorker.DamageResult result = new DamageWorker.DamageResult();
            IEnumerable<DamageInfo> damageInfosToApply = DamageInfosToApply(target, isCrit);
            foreach (DamageInfo current in damageInfosToApply)
            {
                if (target.ThingDestroyed)
                {
                    break;
                }

                LastAttackVerb = this;
                result = target.Thing.TakeDamage(current);
                LastAttackVerb = null;
            }
            // Apply animal knockdown
            if (isCrit && CasterPawn.def.race.Animal)
            {
                var pawn = target.Thing as Pawn;
                if (pawn != null && !pawn.Dead)
                {
                    //pawn.stances?.stunner.StunFor(KnockdownDuration);
                    pawn.stances?.SetStance(new Stance_Cooldown(KnockdownDuration, pawn, null));
                    Job job = JobMaker.MakeJob(CE_JobDefOf.WaitKnockdown);
                    job.expiryInterval = KnockdownDuration;
                    pawn.jobs?.StartJob(job, JobCondition.InterruptForced, null, false, false);
                }
            }
            isCrit = false;
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
        private void DoParry(Pawn defender, Thing parryThing, bool isRiposte = false)
        {
            if (parryThing != null)
            {
                foreach (var dinfo in DamageInfosToApply(defender))
                {
                    LastAttackVerb = this;
                    ArmorUtilityCE.ApplyParryDamage(dinfo, parryThing);
                    LastAttackVerb = null;
                }
            }
            if (isRiposte)
            {
                SoundDef sound = null;
                if (parryThing is Apparel_Shield)
                {
                    // Shield bash
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, 6, parryThing.GetStatValue(CE_StatDefOf.MeleePenetrationFactor), -1, defender, null, parryThing.def);
                    dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                    dinfo.SetAngle((CasterPawn.Position - defender.Position).ToVector3());
                    caster.TakeDamage(dinfo);
                    if (!parryThing.Stuff.stuffProps.soundMeleeHitBlunt.NullOrUndefined())
                        sound = parryThing.Stuff.stuffProps.soundMeleeHitBlunt;
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

        /*
         *  Alistaire: Code targets Bottom about 90% of the time, therefore commented out
         *  
        /// <summary>
        /// Selects a random BodyPartHeight out of the ones our CasterPawn can hit, depending on our body size vs the target's. So a rabbit can hit top height of another rabbit, but not of a human.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private BodyPartHeight GetBodyPartHeightFor(LocalTargetInfo target)
        {
            Pawn pawn = target.Thing as Pawn;
            if (pawn == null || CasterPawn == null) return BodyPartHeight.Undefined;
            var casterReach = new CollisionVertical(CasterPawn).Max * 1.2f;
            var targetHeight = new CollisionVertical(pawn);
            BodyPartHeight maxHeight = targetHeight.GetRandWeightedBodyHeightBelow(casterReach);
            BodyPartHeight height = (BodyPartHeight)Rand.RangeInclusive(1, (int)maxHeight);
            return height;
        }*/

        // unmodified
        private SoundDef SoundHitPawn()
        {
            if (EquipmentSource != null && EquipmentSource.Stuff != null)
            {
                if (verbProps.meleeDamageDef.armorCategory == DamageArmorCategoryDefOf.Sharp)
                {
                    if (!EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp.NullOrUndefined())
                    {
                        return EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp;
                    }
                }
                else if (!EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt.NullOrUndefined())
                {
                    return EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt;
                }
            }
            if (CasterPawn != null && !CasterPawn.def.race.soundMeleeHitPawn.NullOrUndefined())
            {
                return CasterPawn.def.race.soundMeleeHitPawn;
            }
            return SoundDefOf.Pawn_Melee_Punch_HitPawn;
        }

        // unmodified
        private SoundDef SoundHitBuilding()
        {
            if (EquipmentSource != null && EquipmentSource.Stuff != null)
            {
                if (verbProps.meleeDamageDef.armorCategory == DamageArmorCategoryDefOf.Sharp)
                {
                    if (!EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp.NullOrUndefined())
                    {
                        return EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp;
                    }
                }
                else if (!EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt.NullOrUndefined())
                {
                    return EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt;
                }
            }
            if (CasterPawn != null && !CasterPawn.def.race.soundMeleeHitBuilding.NullOrUndefined())
            {
                return CasterPawn.def.race.soundMeleeHitBuilding;
            }
            return SoundDefOf.Pawn_Melee_Punch_HitBuilding;
        }

        // unmodified
        private SoundDef SoundMiss()
        {
            if (CasterPawn != null && !CasterPawn.def.race.soundMeleeMiss.NullOrUndefined())
            {
                return CasterPawn.def.race.soundMeleeMiss;
            }
            return SoundDefOf.Pawn_Melee_Punch_Miss;
        }

        private static float GetComparativeChanceAgainst(Pawn attacker, Pawn defender, StatDef stat, float baseChance, float defenderSkillMult = 1)
        {
            if (attacker == null || defender == null)
                return 0;
            var offSkill = stat.Worker.IsDisabledFor(attacker) ? 0 : attacker.GetStatValue(stat);
            var defSkill = stat.Worker.IsDisabledFor(defender) ? 0 : defender.GetStatValue(stat) * defenderSkillMult;
            var chance = Mathf.Clamp01(baseChance + offSkill - defSkill);
            return chance;
        }

        #endregion
    }
}
