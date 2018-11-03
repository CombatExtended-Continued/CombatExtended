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

        bool isCrit;

        DamageDef CritDamageDef
        {
            get
            {
                if (CasterPawn.def.race.Animal)
                    return verbProps.meleeDamageDef;
                if (verbProps.meleeDamageDef.armorCategory == CE_DamageArmorCategoryDefOf.Blunt)
                {
                    return DamageDefOf.Stun;
                }
                return DefDatabase<DamageDef>.GetNamed(verbProps.meleeDamageDef.defName + "_Critical");
            }
        }

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
                casterPawn.skills.Learn(SkillDefOf.Melee, HitXP, false);
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
                    defender.skills?.Learn(SkillDefOf.Melee, DodgeXP, false);
                }
                else
                {
                    // Attack connects, calculate resolution
                    //var resultRoll = Rand.Value;
                    var parryBonus = 1 / EquipmentSource?.GetStatValue(CE_StatDefOf.MeleeCounterParryBonus) ?? 1;
                    var parryChance = GetComparativeChanceAgainst(defender, casterPawn, CE_StatDefOf.MeleeParryChance, BaseParryChance, parryBonus);
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

                            defender.skills?.Learn(SkillDefOf.Melee, CritXP + ParryXP, false);
                        }
                        else
                        {
                            // Do a parry
                            DoParry(defender, parryThing);
                            moteText = "CE_TextMote_Parried".Translate();
                            CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesMiss, false); //placeholder

                            defender.skills?.Learn(SkillDefOf.Melee, ParryXP, false);
                        }

                        result = false;
                        soundDef = SoundMiss(); // TODO Set hit sound to something more appropriate
                    }
                    else
                    {
                        BattleLogEntry_MeleeCombat log = this.CreateCombatLog((ManeuverDef maneuver) => maneuver.combatLogRulesHit, false);

                        // Attack connects
                        if (surpriseAttack || Rand.Chance(GetComparativeChanceAgainst(casterPawn, defender, CE_StatDefOf.MeleeCritChance, BaseCritChance)))
                        {
                            // Do a critical hit
                            isCrit = true;
                            ApplyMeleeDamageToTarget(currentTarget).AssociateWithLog(log);
                            moteText = casterPawn.def.race.Animal ? "CE_TextMote_Knockdown".Translate() : "CE_TextMote_CriticalHit".Translate();
                            casterPawn.skills?.Learn(SkillDefOf.Melee, CritXP, false);
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
            var critDamDef = CritDamageDef;
            //START 1:1 COPY Verb_MeleeAttack.DamageInfosToApply
            float damAmount = this.verbProps.AdjustedMeleeDamageAmount(this, base.CasterPawn);
            float armorPenetration = this.verbProps.AdjustedArmorPenetration(this, base.CasterPawn);
            DamageDef damDef = isCrit && critDamDef != DamageDefOf.Stun ? critDamDef : verbProps.meleeDamageDef; //Alteration	//Added isCrit check
            BodyPartGroupDef bodyPartGroupDef = null;
            HediffDef hediffDef = null;
            damAmount = Rand.Range(damAmount * 0.8f, damAmount * 1.2f);
            if (base.CasterIsPawn)
            {
                bodyPartGroupDef = this.verbProps.AdjustedLinkedBodyPartsGroup(this.tool);
                if (damAmount >= 1f)
                {
                    if (base.HediffCompSource != null)
                    {
                        hediffDef = base.HediffCompSource.Def;
                    }
                }
                else
                {
                    damAmount = 1f;
                    damDef = DamageDefOf.Blunt;
                }
            }
            ThingDef source;
            if (base.EquipmentSource != null)
            {
                source = base.EquipmentSource.def;
            }
            else
            {
                source = base.CasterPawn.def;
            }
            Vector3 direction = (target.Thing.Position - base.CasterPawn.Position).ToVector3();
            DamageDef def = damDef;
            //END 1:1 COPY
            BodyPartHeight bodyRegion = GetBodyPartHeightFor(target);   //Custom // Add check for body height
            //START 1:1 COPY
            Thing caster = this.caster;
            DamageInfo mainDinfo = new DamageInfo(def, damAmount, armorPenetration, -1f, caster, null, source, DamageInfo.SourceCategory.ThingOrUnknown, null); //Alteration
            mainDinfo.SetBodyRegion(bodyRegion, BodyPartDepth.Outside); //Alteration
            mainDinfo.SetWeaponBodyPartGroup(bodyPartGroupDef);
            mainDinfo.SetWeaponHediff(hediffDef);
            mainDinfo.SetAngle(direction);
            yield return mainDinfo;

            // Apply secondary damage on surprise attack
            /*
            if (this.surpriseAttack && ((this.verbProps.surpriseAttack != null && !this.verbProps.surpriseAttack.extraMeleeDamages.NullOrEmpty<ExtraMeleeDamage>()) || this.tool == null || this.tool.surpriseAttack == null || this.tool.surpriseAttack.extraMeleeDamages.NullOrEmpty<ExtraMeleeDamage>()))
			{
				IEnumerable<ExtraMeleeDamage> extraDamages = Enumerable.Empty<ExtraMeleeDamage>();
				if (this.verbProps.surpriseAttack != null && this.verbProps.surpriseAttack.extraMeleeDamages != null)
				{
					extraDamages = extraDamages.Concat(this.verbProps.surpriseAttack.extraMeleeDamages);
				}
				if (this.tool != null && this.tool.surpriseAttack != null && !this.tool.surpriseAttack.extraMeleeDamages.NullOrEmpty<ExtraMeleeDamage>())
				{
					extraDamages = extraDamages.Concat(this.tool.surpriseAttack.extraMeleeDamages);
				}
				foreach (ExtraMeleeDamage extraDamage in extraDamages)
				{
					int extraDamageAmount = GenMath.RoundRandom(extraDamage.AdjustedDamageAmount(this, base.CasterPawn));
					float extraDamageArmorPenetration = extraDamage.AdjustedArmorPenetration(this, base.CasterPawn);
					def = extraDamage.def;
					num2 = (float)extraDamageAmount;
					num = extraDamageArmorPenetration;
					caster = this.caster;
					DamageInfo extraDinfo = new DamageInfo(def, num2, num, -1f, caster, null, source, DamageInfo.SourceCategory.ThingOrUnknown, null);
					extraDinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
					extraDinfo.SetWeaponBodyPartGroup(bodyPartGroupDef);
					extraDinfo.SetWeaponHediff(hediffDef);
					extraDinfo.SetAngle(direction);
					yield return extraDinfo;
				}
			}
            */
            //END 1:1 COPY
            // Apply critical damage
            if (isCrit && critDamDef == DamageDefOf.Stun)
            {
                var critAmount = GenMath.RoundRandom(mainDinfo.Amount * 0.25f);
                var critDinfo = new DamageInfo(critDamDef, critAmount, float.MaxValue, //Ignore armor //armorPenetration, //Armor Penetration
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
            if (this.surpriseAttack)
            {
                return 1f;
            }
            if (this.IsTargetImmobile(target))
            {
                return 1f;
            }
            if (base.CasterPawn.skills != null)
            {
                return base.CasterPawn.GetStatValue(StatDefOf.MeleeHitChance, true);
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
            foreach (DamageInfo current in DamageInfosToApply(target, isCrit))
            {
                if (target.ThingDestroyed)
                {
                    break;
                }
                result = target.Thing.TakeDamage(current);
            }
            // Apply animal knockdown
            if (isCrit && CasterPawn.def.race.Animal)
            {
                var pawn = target.Thing as Pawn;
                if (pawn != null && !pawn.Dead)
                {
                    //pawn.stances?.stunner.StunFor(KnockdownDuration);
                    pawn.stances?.SetStance(new Stance_Cooldown(KnockdownDuration, pawn, null));
                    pawn.jobs?.StartJob(new Job(CE_JobDefOf.WaitKnockdown) { expiryInterval = KnockdownDuration }, JobCondition.InterruptForced, null, false, false);
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
                || pawn.story.WorkTagIsDisabled(WorkTags.Violent)
                || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
                || IsTargetImmobile(pawn))
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
                    ArmorUtilityCE.ApplyParryDamage(dinfo, parryThing);
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
                sound?.PlayOneShot(new TargetInfo(caster.Position, caster.Map));
            }
            // Register with parry tracker
            ParryTracker tracker = defender.Map.GetComponent<ParryTracker>();
            if (tracker == null)
            {
                Log.Error("CE failed to find ParryTracker to register pawn " + defender.ToString());
            }
            else
            {
                tracker.RegisterParryFor(defender, verbProps.AdjustedCooldownTicks(this, defender));
            }
        }

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
        }

        // unmodified
        private SoundDef SoundHitPawn()
        {
            if (this.EquipmentSource != null && this.EquipmentSource.Stuff != null)
            {
                if (this.verbProps.meleeDamageDef.armorCategory == DamageArmorCategoryDefOf.Sharp)
                {
                    if (!this.EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp.NullOrUndefined())
                    {
                        return this.EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp;
                    }
                }
                else if (!this.EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt.NullOrUndefined())
                {
                    return this.EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt;
                }
            }
            if (base.CasterPawn != null && !base.CasterPawn.def.race.soundMeleeHitPawn.NullOrUndefined())
            {
                return base.CasterPawn.def.race.soundMeleeHitPawn;
            }
            return SoundDefOf.Pawn_Melee_Punch_HitPawn;
        }

        // unmodified
        private SoundDef SoundHitBuilding()
        {
            if (this.EquipmentSource != null && this.EquipmentSource.Stuff != null)
            {
                if (this.verbProps.meleeDamageDef.armorCategory == DamageArmorCategoryDefOf.Sharp)
                {
                    if (!this.EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp.NullOrUndefined())
                    {
                        return this.EquipmentSource.Stuff.stuffProps.soundMeleeHitSharp;
                    }
                }
                else if (!this.EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt.NullOrUndefined())
                {
                    return this.EquipmentSource.Stuff.stuffProps.soundMeleeHitBlunt;
                }
            }
            if (base.CasterPawn != null && !base.CasterPawn.def.race.soundMeleeHitBuilding.NullOrUndefined())
            {
                return base.CasterPawn.def.race.soundMeleeHitBuilding;
            }
            return SoundDefOf.Pawn_Melee_Punch_HitBuilding;
        }

        // unmodified
        private SoundDef SoundMiss()
        {
            if (base.CasterPawn != null && !base.CasterPawn.def.race.soundMeleeMiss.NullOrUndefined())
            {
                return base.CasterPawn.def.race.soundMeleeMiss;
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
