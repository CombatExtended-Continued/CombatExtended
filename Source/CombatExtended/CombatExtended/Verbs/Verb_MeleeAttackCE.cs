using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
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
        private const float CritFactor = 1.5f;  // Criticals will do normal damage times this

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
			casterPawn.Drawer.rotator.Face(targetThing.DrawPos);

            // Award XP as per vanilla
            bool targetImmobile = IsTargetImmobile(currentTarget);
            if (!targetImmobile && casterPawn.skills != null)
            {
                casterPawn.skills.Learn(SkillDefOf.Melee, 250f, false);
            }

            // Hit calculations
            bool result;
            string moteText = "";
            SoundDef soundDef;
            Pawn defender = targetThing as Pawn;
            var hitRoll = Rand.Value;
            if (hitRoll < GetHitChance(targetThing))
            {
                // Check for dodge
                if (!targetImmobile && !surpriseAttack && hitRoll < targetThing.GetStatValue(CE_StatDefOf.MeleeDodgeChance))
                {
                    // Attack is evaded
                    moteText = "Dodged";
                    result = false;
                    soundDef = SoundMiss();
                }
                else
                {
                    // Attack connects, calculate resolution
                    var resultRoll = Rand.Value;
                    var parryChance = targetThing.GetStatValue(CE_StatDefOf.MeleeParryChance);
                    if (!surpriseAttack && defender != null && CanDoParry(defender) && resultRoll < parryChance)
                    {
                        // Attack is parried
                        Apparel shield = defender.apparel.WornApparel.FirstOrDefault(x => x is Apparel_Shield);
                        bool isShieldBlock = shield != null && Rand.Chance(ShieldBlockChance);
                        Thing parryThing = isShieldBlock ? shield
                            : defender.equipment?.Primary != null ? defender.equipment.Primary : defender;

                        if (resultRoll < parryChance * targetThing.GetStatValue(CE_StatDefOf.MeleeCritChance))
                        {
                            // Do a riposte
                            DoParry(defender, parryThing, true);
                            moteText = "Riposted";
                        }
                        else
                        {
                            // Do a parry
                            DoParry(defender, parryThing);
                            moteText = "Parried";
                        }

                        result = false;
                        soundDef = SoundMiss(); // TODO Set hit sound to something more appropriate
                    }
                    else
                    {
                        // Attack connects
                        if (!surpriseAttack && resultRoll < (1 - casterPawn.GetStatValue(CE_StatDefOf.MeleeCritChance)))
                        {
                            // Do a regular hit as per vanilla
                            ApplyMeleeDamageToTarget(currentTarget);
                        }
                        else
                        {
                            // Do a critical hit
                            ApplyMeleeDamageToTarget(currentTarget, true);
                            moteText = "Critical hit";
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
            }
            if (!moteText.NullOrEmpty())
                MoteMaker.ThrowText(targetThing.DrawPos, casterPawn.Map, moteText);
            soundDef.PlayOneShot(new TargetInfo(targetThing.Position, casterPawn.Map, false));
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
			casterPawn.Drawer.rotator.FaceCell(targetThing.Position);
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
		private IEnumerable<DamageInfo> DamageInfosToApply(LocalTargetInfo target)
		{
			float damAmount = (float)this.verbProps.AdjustedMeleeDamageAmount(this, base.CasterPawn, this.ownerEquipment);
			DamageDef damDef = this.verbProps.meleeDamageDef;
			BodyPartGroupDef bodyPartGroupDef = null;
			HediffDef hediffDef = null;
			if (base.CasterIsPawn)
			{
				if (damAmount >= 1f)
				{
					bodyPartGroupDef = this.verbProps.linkedBodyPartsGroup;
					if (this.ownerHediffComp != null)
					{
						hediffDef = this.ownerHediffComp.Def;
					}
				}
				else
				{
					damAmount = 1f;
					damDef = DamageDefOf.Blunt;
				}
			}
			ThingDef source;
			if (this.ownerEquipment != null)
			{
				source = this.ownerEquipment.def;
			}
			else
			{
				source = base.CasterPawn.def;
			}
			Vector3 direction = (target.Thing.Position - base.CasterPawn.Position).ToVector3();
			Thing caster = this.caster;
            BodyPartHeight bodyRegion = GetBodyPartHeightFor(target);   // Add check for body height
			DamageInfo mainDinfo = new DamageInfo(damDef, GenMath.RoundRandom(damAmount), -1f, caster, null, source);
			mainDinfo.SetBodyRegion(bodyRegion, BodyPartDepth.Outside);
			mainDinfo.SetWeaponBodyPartGroup(bodyPartGroupDef);
			mainDinfo.SetWeaponHediff(hediffDef);
			mainDinfo.SetAngle(direction);
            Log.Message("CE returning main dinfo");
			yield return mainDinfo;
			if (this.surpriseAttack && this.verbProps.surpriseAttack != null && this.verbProps.surpriseAttack.extraMeleeDamages != null)
			{
				List<ExtraMeleeDamage> extraDamages = this.verbProps.surpriseAttack.extraMeleeDamages;
				for (int i = 0; i < extraDamages.Count; i++)
				{
					ExtraMeleeDamage extraDamage = extraDamages[i];
					int amount = GenMath.RoundRandom((float)extraDamage.amount * base.GetDamageFactorFor(base.CasterPawn));
					caster = this.caster;
					DamageInfo extraDinfo = new DamageInfo(extraDamage.def, amount, -1f, caster, null, source);
					extraDinfo.SetBodyRegion(bodyRegion, BodyPartDepth.Outside);
					extraDinfo.SetWeaponBodyPartGroup(bodyPartGroupDef);
					extraDinfo.SetWeaponHediff(hediffDef);
					extraDinfo.SetAngle(direction);
					yield return extraDinfo;
				}
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
        /// <param name="isCrit">Whether we should apply critical damage</param>
		private void ApplyMeleeDamageToTarget(LocalTargetInfo target, bool isCrit = false)
		{
			foreach (DamageInfo current in DamageInfosToApply(target))
			{
				if (target.ThingDestroyed)
				{
					break;
				}
                if (isCrit) current.SetAmount(Mathf.CeilToInt(current.Amount * CritFactor));    // Apply crit factor
				target.Thing.TakeDamage(current);
			}
		}

        /// <summary>
        /// Checks ParryTracker for whether the specified pawn can currently perform a parry.
        /// </summary>
        /// <param name="pawn">Pawn to check</param>
        /// <returns>True if pawn still has parries available or no parry tracker could be found, false otherwise</returns>
        private bool CanDoParry(Pawn pawn)
        {
            if (!pawn.RaceProps.Humanlike || pawn.story.WorkTagIsDisabled(WorkTags.Violent))
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
                foreach(var dinfo in DamageInfosToApply(defender))
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
                    Log.Message("CE doing shield bash");
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, 6, -1, defender, null, parryThing.def);
                    caster.TakeDamage(dinfo);
                    if (!parryThing.Stuff.stuffProps.soundMeleeHitBlunt.NullOrUndefined())
                        sound = parryThing.Stuff.stuffProps.soundMeleeHitBlunt;
                }
                else
                {
                    Verb_MeleeAttackCE verb = defender.meleeVerbs.TryGetMeleeVerb() as Verb_MeleeAttackCE;
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
                tracker.RegisterParryFor(defender, verbProps.AdjustedCooldownTicks(ownerEquipment));
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
            BodyPartHeight maxHeight = CE_Utility.GetCollisionBodyHeight(pawn, CE_Utility.GetCollisionVertical(CasterPawn).max * 1.2f);
            BodyPartHeight height = (BodyPartHeight)Rand.RangeInclusive(1, (int)maxHeight);
            return height;
        }

        // unmodified
		private SoundDef SoundHitPawn()
		{
			if (this.ownerEquipment != null && this.ownerEquipment.Stuff != null)
			{
				if (this.verbProps.meleeDamageDef.armorCategory == DamageArmorCategory.Sharp)
				{
					if (!this.ownerEquipment.Stuff.stuffProps.soundMeleeHitSharp.NullOrUndefined())
					{
						return this.ownerEquipment.Stuff.stuffProps.soundMeleeHitSharp;
					}
				}
				else if (!this.ownerEquipment.Stuff.stuffProps.soundMeleeHitBlunt.NullOrUndefined())
				{
					return this.ownerEquipment.Stuff.stuffProps.soundMeleeHitBlunt;
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
			if (this.ownerEquipment != null && this.ownerEquipment.Stuff != null)
			{
				if (this.verbProps.meleeDamageDef.armorCategory == DamageArmorCategory.Sharp)
				{
					if (!this.ownerEquipment.Stuff.stuffProps.soundMeleeHitSharp.NullOrUndefined())
					{
						return this.ownerEquipment.Stuff.stuffProps.soundMeleeHitSharp;
					}
				}
				else if (!this.ownerEquipment.Stuff.stuffProps.soundMeleeHitBlunt.NullOrUndefined())
				{
					return this.ownerEquipment.Stuff.stuffProps.soundMeleeHitBlunt;
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

        #endregion
    }
}
