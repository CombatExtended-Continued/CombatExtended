using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public static class ArmorUtilityCE
    {
        #region Constants

        // Soft armor will at a minimum take damage from sharp attacks' damage
        // multiplied by this factor
        private const float SOFT_ARMOR_MIN_SHARP_DAMAGE_FACTOR = 0.2f;

        // Hard armor will take damage from attacks' damage multiplied by this factor
        // Essentially a health multiplier for hard armor
        private const float HARD_ARMOR_DAMAGE_FACTOR = 0.5f;

        // How much damage of a spike trap should be taken as blunt penetration
        private const float TRAP_BLUNT_PEN_FACTOR = 0.65f;

        // Shields reduce whatever blunt penetration they pass off from projectiles
        // by multiplying it by this factor
        private const float PROJECTILE_SHIELD_BLUNT_PEN_FACTOR = 0.2f;

        // Things taking in damage from a parry will have it multiplied by this factor
        private const float PARRY_THING_DAMAGE_FACTOR = 0.5f;

        #endregion

        #region Properties

        // Used as a constant, defines what stuff makes armor be considered soft
        // Note that only armor which is stuffed is checked
        private static readonly StuffCategoryDef[] SOFT_STUFFS = {
            StuffCategoryDefOf.Fabric, StuffCategoryDefOf.Leathery
        };

        #endregion

        #region Classes

        // Responsible for storing internal information used in armor penetration calculation
        internal class AttackInfo
        {
            // Attack's damage info. Should remain mostly unchanged until the attack is being applied
            public DamageInfo dinfo;
            // How much penetration force does the attack currently have
            public float penAmount = 0f;
            // How much damage is in a unit of penetration
            public float dmgPerPenAmount = 0f;
            // How much of the next attack's penetration is in a unit of this attack's penetration
            // Blocked penetration amount is transfered to the next attack multiplied by this value
            public float penTransferRate = 0f;
            // To which next attack does the current attack decay. Essentially a linked-list
            public AttackInfo next;

            public AttackInfo(DamageInfo info)
            {
                dinfo = info;
                penAmount = dinfo.ArmorPenetrationInt;
                dmgPerPenAmount = penAmount > 0f
                    ? dinfo.Amount / penAmount
                    : 0f;
            }

            public void Append(AttackInfo info)
            {
                var tail = this;
                while (tail.next != null)
                {
                    tail = tail.next;
                }
                tail.next = info;
                tail.penTransferRate = tail.dinfo.ArmorPenetrationInt > 0f
                    ? info.dinfo.ArmorPenetrationInt / tail.dinfo.ArmorPenetrationInt
                    : 0f;
                info.penAmount = 0f;
            }

            public void Append(DamageInfo info)
            {
                Append(new AttackInfo(info));
            }

            // Gets the first attack info that still has some penetration amount,
            // or the last element in the linked list
            public AttackInfo FirstValidOrLast()
            {
                AttackInfo newHead = this;
                while (newHead.next != null && newHead.penAmount <= 0f)
                {
                    newHead = newHead.next;
                }
                return newHead;
            }

            public override string ToString()
            {
                return $"({base.ToString()} dinfo={dinfo} penAmount={penAmount}) "
                    + $"dmgPerPenAmount={dmgPerPenAmount} penTransferRate={penTransferRate}";
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Calculates damage through armor, depending on damage type, target and natural resistance.
        /// Also calculates deflection and adjusts damage type and impacted body part accordingly.
        /// </summary>
        /// <param name="origDinfo">The pre-armor damage info</param>
        /// <param name="pawn">The damaged pawn</param>
        /// <param name="armorDeflected">Whether the attack was completely absorbed by the armor</param>
        /// <param name="armorReduced">Whether sharp damage was deflected by armor</param>
        /// <param name="skipSecondary">Whether secondary damage application should be skipped</param>
        /// <returns>
        /// The same damage info with its damage and armor penetration changed,
        /// or a different damage info if the original damage info was stopped.
        /// </returns>
        public static DamageInfo GetAfterArmorDamage(DamageInfo origDinfo, Pawn pawn,
                out bool armorDeflected, out bool armorReduced, out bool skipSecondary)
        {
            armorDeflected = false;
            armorReduced = false; // Unused
            skipSecondary = false;

            if (origDinfo.Def.armorCategory == null
                    || (!(origDinfo.Weapon?.projectile is ProjectilePropertiesCE)
                        && Verb_MeleeAttackCE.LastAttackVerb == null
                        && origDinfo.Weapon == null
                        && origDinfo.Instigator == null))
            {
                return origDinfo;
            }

            var dinfo = new DamageInfo(origDinfo);

            if (dinfo.IsAmbientDamage())
            {
                dinfo.SetAmount(GetAmbientPostArmorDamage(
                            dinfo.Amount,
                            dinfo.ArmorRatingStat(),
                            pawn,
                            dinfo.HitPart));
                armorDeflected = dinfo.Amount <= 0;
                return dinfo;
            }

            var deflectionComp = pawn.TryGetComp<Comp_BurnDamageCalc>();
            if (deflectionComp != null)
            {
                deflectionComp.deflectedSharp = false;
            }

            // Initialize the AttackInfo linked list
            var ainfo = new AttackInfo(dinfo);
            if (dinfo.ArmorRatingStat() == StatDefOf.ArmorRating_Sharp)
            {
                ainfo.Append(GetDeflectDamageInfo(dinfo));
            }
            // Remember the damage worker for later use
            var dworker = dinfo.Def.Worker;

            var involveArmor = ainfo.dinfo.Def.harmAllLayersUntilOutside
                || ainfo.dinfo.HitPart.depth == BodyPartDepth.Outside;
            if (involveArmor && pawn.apparel != null && !pawn.apparel.WornApparel.NullOrEmpty())
            {
                var apparel = pawn.apparel.WornApparel;
                Apparel app;

                // Check against shield
                app = apparel.FirstOrDefault(x => x is Apparel_Shield);
                if (app != null && (!dinfo.Weapon?.IsMeleeWeapon ?? true)
                        && DoesShieldCover(ainfo.dinfo.HitPart, pawn, app))
                {
                    // Apply attack infos
                    for (var info = ainfo;
                            info != null && app != null && !app.Destroyed;
                            info = info.next)
                    {
                        var blockedPenAmount = PenetrateArmor(info, app);
                        // Blocked penetration amount decays into next attack penetration
                        if (info.next != null)
                        {
                            info.next.penAmount += info.penTransferRate * blockedPenAmount;
                        }
                    }

                    // Primary attack was exhausted
                    if (ainfo.penAmount <= 0f)
                    {
                        // Apply secondary damage to shield
                        if (!app.Destroyed)
                        {
                            skipSecondary = true;
                            if (ainfo.dinfo.Weapon?.projectile is ProjectilePropertiesCE props
                                    && !props.secondaryDamage.NullOrEmpty())
                            {
                                foreach (var sec in props.secondaryDamage)
                                {
                                    if (app.Destroyed)
                                    {
                                        break;
                                    }
                                    if (!Rand.Chance(sec.chance))
                                    {
                                        continue;
                                    }
                                    PenetrateArmor(new AttackInfo(sec.GetDinfo()), app);
                                }
                            }
                        }

                        // Update deflection comp
                        if (ainfo.dinfo.ArmorRatingStat() == StatDefOf.ArmorRating_Sharp
                                && deflectionComp != null)
                        {
                            deflectionComp.deflectedSharp = true;
                        }

                        // Update primary attack
                        ainfo = ainfo.FirstValidOrLast();
                        // All attacks were exhausted; return
                        if (ainfo.penAmount <= 0f)
                        {
                            ainfo.dinfo.SetAmount(0f);
                            return ainfo.dinfo;
                        }

                        // Update target body part for blunt attack infos
                        BodyPartRecord partToHit = pawn.health.hediffSet.GetNotMissingParts(
                                depth: BodyPartDepth.Outside)
                            .FirstOrFallback(x => x.IsInGroup(CE_BodyPartGroupDefOf.LeftArm)
                                    || x.IsInGroup(CE_BodyPartGroupDefOf.RightArm));
                        // Cannot wear shields without a left shoulder
                        if (partToHit == null)
                        {
                            partToHit = pawn.health.hediffSet.GetNotMissingParts(
                                    depth: BodyPartDepth.Outside)
                                .First(x => x.IsInGroup(CE_BodyPartGroupDefOf.LeftShoulder));
                        }

                        for (var info = ainfo; info != null; info = info.next)
                        {
                            if (ainfo.dinfo.Def.armorCategory == CE_DamageArmorCategoryDefOf.Blunt)
                            {
                                ainfo.dinfo.SetHitPart(partToHit);
                            }
                        }
                    }
                }

                // Check against apparel
                // Apparel is arranged in draw order, iterate in reverse to go Shell -> OnSkin
                for (var i = apparel.Count - 1; i >= 0; i--)
                {
                    app = apparel[i];
                    if (app == null
                            || !app.def.apparel.CoversBodyPart(dinfo.HitPart)
                            || app is Apparel_Shield)
                    {
                        continue;
                    }

                    // Apply attack infos
                    for (var info = ainfo;
                            info != null && app != null && !app.Destroyed;
                            info = info.next)
                    {
                        var blockedPenAmount = PenetrateArmor(info, app);
                        // Blocked penetration amount decays into next attack penetration
                        if (info.next != null)
                        {
                            info.next.penAmount += info.penTransferRate * blockedPenAmount;
                        }
                    }

                    // Primary attack was exhausted
                    if (ainfo.penAmount <= 0f)
                    {
                        // Update deflection comp
                        if (ainfo.dinfo.ArmorRatingStat() == StatDefOf.ArmorRating_Sharp
                                && deflectionComp != null)
                        {
                            deflectionComp.deflectedSharp = true;
                        }

                        // Update primary attack
                        ainfo = ainfo.FirstValidOrLast();
                        // All attacks were exhausted; return
                        if (ainfo.penAmount <= 0f)
                        {
                            ainfo.dinfo.SetAmount(0f);
                            return ainfo.dinfo;
                        }
                    }
                }
            }

            // Apply every attack info to every body part it should hit
            var sharpPartDensity = pawn.GetStatValue(CE_StatDefOf.BodyPartSharpArmor);
            var bluntPartDensity = pawn.GetStatValue(CE_StatDefOf.BodyPartBluntArmor);
            for (var info = ainfo; info != null; info = info.next)
            {
                var partsToHit = new List<BodyPartRecord>()
                {
                    info.dinfo.HitPart
                };

                // Whether or not to hit parent parts of attack hitpart
                if (info.dinfo.HitPart.depth == BodyPartDepth.Inside
                        && info.dinfo.Def.harmAllLayersUntilOutside
                        && info.dinfo.AllowDamagePropagation)
                {
                    info.dinfo.SetAllowDamagePropagation(false);
                    var partToHit = info.dinfo.HitPart;
                    while (partToHit.parent != null && partToHit.depth == BodyPartDepth.Inside)
                    {
                        partToHit = partToHit.parent;
                        partsToHit.Add(partToHit);
                    }
                }

                var armorRatingStat = ainfo.dinfo.ArmorRatingStat();
                var partDensity = 0f;
                if (armorRatingStat == StatDefOf.ArmorRating_Sharp)
                {
                    partDensity = sharpPartDensity;
                }
                else if (armorRatingStat == StatDefOf.ArmorRating_Blunt)
                {
                    partDensity = bluntPartDensity;
                }

                // Go over partsToHit from outside to inside
                for (var i = partsToHit.Count - 1; i >= 0 && info.penAmount >= 0f; i--)
                {
                    var partToHit = partsToHit[i];
                    // Don't apply body part density if it's the first part
                    PenetrateBodyPart(info, dworker, pawn, partToHit, partDensity);
                }
            }

            // Primary attack was exhausted
            if (ainfo.penAmount <= 0f)
            {
                // Update deflection comp
                if (ainfo.dinfo.ArmorRatingStat() == StatDefOf.ArmorRating_Sharp
                        && deflectionComp != null)
                {
                    deflectionComp.deflectedSharp = true;
                }

                // Update primary attack
                ainfo = ainfo.FirstValidOrLast();
                // All attacks were exhausted; return
                if (ainfo.penAmount <= 0f)
                {
                    ainfo.dinfo.SetAmount(0f);
                    return ainfo.dinfo;
                }
            }

            // Apply all other attacks that have any penetration amount
            for (var info = ainfo.next; info != null; info = info.next)
            {
                if (info.penAmount <= 0f)
                {
                    continue;
                }

                info.dinfo.SetIgnoreArmor(true); // Prevents recursion
                info.dinfo.weaponInt = null; // Prevents secondary damage
                info.dinfo.SetAmount(info.penAmount * info.dmgPerPenAmount);
                // Setting armor penetration amount just in case
                info.dinfo.armorPenetrationInt = info.penAmount;
                // Apply damage using the damage worker we remembered at the start
                dworker.Apply(info.dinfo, pawn);
            }

            ainfo.dinfo.SetAmount(ainfo.penAmount * ainfo.dmgPerPenAmount);
            // Setting armor penetration amount just in case
            ainfo.dinfo.armorPenetrationInt = ainfo.penAmount;
            return ainfo.dinfo;
        }

        /// <summary>
        /// Bridge function to pass the apparel armor rating to the full function
        /// </summary>
        private static float PenetrateArmor(AttackInfo ainfo, Apparel armor)
        {
            if (ainfo.penAmount <= 0f)
            {
                return 0f;
            }

            var armorAmount = armor.PartialStat(ainfo.dinfo.ArmorRatingStat(), ainfo.dinfo.HitPart);
            return PenetrateArmor(ainfo, armor, armorAmount);
        }

        /// <summary>
        /// Bridge function to pass the weapon toughness rating to the full function
        /// </summary>
        private static float PenetrateWeapon(AttackInfo ainfo, Thing weapon)
        {
            if (ainfo.penAmount <= 0f)
            {
                return 0f;
            }

            var armorRatingStat = ainfo.dinfo.ArmorRatingStat();
            float armorAmount = 0f;
            if (armorRatingStat == StatDefOf.ArmorRating_Sharp)
            {
                armorAmount = weapon.GetStatValue(CE_StatDefOf.ToughnessRating);
            }
            else if (armorRatingStat == StatDefOf.ArmorRating_Blunt)
            {
                armorAmount = weapon.GetStatValue(CE_StatDefOf.ToughnessRating) * 1.5f;
            }
            return PenetrateArmor(ainfo, weapon, armorAmount);
        }

        /// <summary>
        /// Reduces the penetration amount of an attack info by the provided armor amount.
        /// Damages the armor that's provided.
        /// </summary>
        /// <param name="ainfo">Attack info to use to penetrate armor</param>
        /// <param name="armor">Armor thing to damage</param>
        /// <param name="armorAmount">The armor amount of the armor</param>
        /// <returns>The amount of penetration amount that was blocked</returns>
        private static float PenetrateArmor(AttackInfo ainfo, Thing armor, float armorAmount)
        {
            if (ainfo.penAmount <= 0f)
            {
                return 0f;
            }

            var penAmount = ainfo.penAmount;
            var blockedPenAmount = penAmount < armorAmount ? penAmount : armorAmount;
            var newPenAmount = penAmount - blockedPenAmount;


            var isSoftArmor = armor.Stuff?.stuffProps.categories.Any(s => SOFT_STUFFS.Contains(s))
                ?? false;
            var dmgAmount = penAmount * ainfo.dmgPerPenAmount;
            var blockedDmgAmount = blockedPenAmount * ainfo.dmgPerPenAmount;
            var newDmgAmount = newPenAmount * ainfo.dmgPerPenAmount;

            var armorRatingStat = ainfo.dinfo.ArmorRatingStat();
            var armorDamage = 0f;
            if (armorRatingStat == StatDefOf.ArmorRating_Heat)
            {
                armorDamage = armor.GetStatValue(StatDefOf.Flammability, true) * dmgAmount;
            }
            // Soft armor takes only blocked damage from sharp
            else if (isSoftArmor)
            {
                if (armorRatingStat == StatDefOf.ArmorRating_Sharp)
                {
                    armorDamage = Mathf.Max(dmgAmount * SOFT_ARMOR_MIN_SHARP_DAMAGE_FACTOR,
                            blockedDmgAmount);
                }
            }
            // Hard armor takes damage from blocked damage and unblocked damage depending on
            // the ratio of penetration amount to armor amount
            else
            {
                float penArmorRatio = penAmount / armorAmount;
                // Armor takes the most sharp damage when penetration amount and armor amount
                // are one to one. Less penetration than armor means failure to penetrate,
                // higher penetration than armor means over-penetration.
                if (armorRatingStat == StatDefOf.ArmorRating_Sharp)
                {
                    armorDamage = blockedDmgAmount * Mathf.Clamp01(penArmorRatio * penArmorRatio)
                        + newDmgAmount * Mathf.Clamp01(1.0f / penArmorRatio);
                }
                // Blunt damage. It's assumed that elastic deformation occurs when attack is blunt
                // and has penetration less than half of armor amount.
                else if (armorRatingStat == StatDefOf.ArmorRating_Blunt)
                {
                    if (penArmorRatio > 0.5f)
                    {
                        penArmorRatio -= 0.5f;
                        armorDamage = Mathf.Min(dmgAmount, dmgAmount * penArmorRatio * penArmorRatio);
                    }
                }

                armorDamage *= HARD_ARMOR_DAMAGE_FACTOR;
            }
            if (armorDamage > 0f)
            {
                TryDamageArmor(ainfo.dinfo.Def, armorDamage, armor);
            }


            ainfo.penAmount = newPenAmount;
            return blockedPenAmount;
        }

        /// <summary>
        /// Damages armor by the integer part of the damage amount.
        /// The fractional part of the damage amount is a chance to deal one additional damage.
        /// </summary>
        /// <returns>True if the armor takes damage, false if it doesn't.</returns>
        private static bool TryDamageArmor(DamageDef def, float armorDamage, Thing armor)
        {
            // Fractional damage has a chance to round up or round down
            // by the chance of the fractional part.
            float flooredDamage = Mathf.Floor(armorDamage);
            armorDamage = Rand.Chance(armorDamage - flooredDamage)
                ? Mathf.Ceil(armorDamage)
                : flooredDamage;

            // Don't call TakeDamage with zero damage
            if (armorDamage > 0f)
            {
                armor.TakeDamage(new DamageInfo(def, (int)armorDamage));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reduces the penenetration amount of an attack by the armor amount of a pawn's body part
        /// and post armor amount (body part density). Applies additional damage to the body part
        /// using a damage worker if it isn't the target hit part of the attack info's damage info.
        /// </summary>
        /// <param name="ainfo">Attack info to use to penetrate the body part</param>
        /// <param name="dworker">Damage worker to use in case of damage to body part</param>
        /// <param name="pawn">Pawn to which the part belongs to</param>
        /// <param name="part">Part to penetrate</param>
        /// <param name="postArmorAmount">How much armor to apply post penetration</param>
        /// <returns>The amount of penetration that was blocked</returns>
        private static float PenetrateBodyPart(AttackInfo ainfo, DamageWorker dworker,
                Pawn pawn, BodyPartRecord part, float partDensity)
        {
            if (ainfo.penAmount <= 0f)
            {
                return 0f;
            }

            var armorAmount = pawn.PartialStat(ainfo.dinfo.ArmorRatingStat(), part);
            if (part.depth == BodyPartDepth.Inside)
            {
                armorAmount += partDensity;
            }

            var penAmount = ainfo.penAmount;
            var newPenAmount = Mathf.Max(penAmount - armorAmount, 0f);

            if (ainfo.dinfo.HitPart != part)
            {
                // Use a cloned damage info so as not to polute the attack's damage info
                var dinfo = new DamageInfo(ainfo.dinfo);
                dinfo.SetHitPart(part);
                dinfo.SetIgnoreArmor(true); // Prevent recursion
                dinfo.weaponInt = null; // Prevent secondary damage
                dinfo.SetAmount(newPenAmount * ainfo.dmgPerPenAmount);
                dinfo.armorPenetrationInt = newPenAmount;
                dworker.Apply(dinfo, pawn);
            }

            ainfo.penAmount = newPenAmount;
            return penAmount - newPenAmount;
        }

        /// <summary>
        /// Creates a new DamageInfo from a deflected attack. Changes damage type to Blunt,
        /// hit part to the outermost parent of the originally hit part and damage amount.
        /// </summary>
        /// <param name="dinfo">The dinfo that was deflected</param>
        /// <returns>DamageInfo copied from dinfo with Def and forceHitPart adjusted</returns>
        private static DamageInfo GetDeflectDamageInfo(DamageInfo dinfo)
        {
            DamageInfo newDinfo;
            if (dinfo.ArmorRatingStat() != StatDefOf.ArmorRating_Sharp)
            {
                newDinfo = new DamageInfo(dinfo);
                newDinfo.SetAmount(0);
                return newDinfo;
            }

            var penAmount = 0f;
            var dmgMulti = 1f;
            if (dinfo.Weapon?.projectile is ProjectilePropertiesCE projectile)
            {
                penAmount = projectile.armorPenetrationBlunt;
                if (projectile.damageAmountBase != 0)
                {
                    dmgMulti = dinfo.Amount / (float)projectile.damageAmountBase;
                }
            }
            else if (dinfo.Instigator?.def.thingClass == typeof(Building_TrapDamager))
            {
                penAmount = dinfo.Instigator.GetStatValue(StatDefOf.TrapMeleeDamage, true)
                    * TRAP_BLUNT_PEN_FACTOR;
            }
            else if (Verb_MeleeAttackCE.LastAttackVerb != null)
            {
                penAmount = Verb_MeleeAttackCE.LastAttackVerb.ArmorPenetrationBlunt;
            }
            else
            {
                Log.Warning($"[CE] Deflection for Instigator:{dinfo.Instigator} "
                        + $"Target:{dinfo.IntendedTarget} DamageDef:{dinfo.Def} "
                        + $"Weapon:{dinfo.Weapon} has null verb, overriding AP");
                penAmount = 50f;
            }

            float dmgAmount = Mathf.Pow(penAmount * 10000f, 1f / 3f) / 10f * dmgMulti;

            newDinfo = new DamageInfo(DamageDefOf.Blunt,
                    dmgAmount,
                    penAmount,
                    dinfo.Angle,
                    dinfo.Instigator,
                    GetOuterMostParent(dinfo.HitPart),
                    dinfo.Weapon,
                    instigatorGuilty: dinfo.InstigatorGuilty);
            newDinfo.SetWeaponBodyPartGroup(dinfo.WeaponBodyPartGroup);
            newDinfo.SetWeaponHediff(dinfo.WeaponLinkedHediff);
            newDinfo.SetInstantPermanentInjury(dinfo.InstantPermanentInjury);
            newDinfo.SetAllowDamagePropagation(dinfo.AllowDamagePropagation);

            return newDinfo;
        }

        /// <summary>
        /// Checks if a shield covers the hit part from an attack
        /// </summary>
        /// <returns>True if the shield does cover the body part, false otherwise</returns>
        private static bool DoesShieldCover(BodyPartRecord hitPart, Pawn pawn, Apparel shield)
        {
            var shieldDef = shield.def.GetModExtension<ShieldDefExtension>();
            if (shieldDef == null)
            {
                Log.ErrorOnce($"[CE] {shield.def} is Apparel_Shield but lacks ShieldDefExtension",
                        shield.def.GetHashCode() + 12748102);
                return false;
            }
            if (!shieldDef.PartIsCoveredByShield(hitPart, pawn.IsCrouching()))
            {
                return false;
            }
            // Right arm is vulnerable during warmup/attack/cooldown
            return !hitPart.IsInGroup(CE_BodyPartGroupDefOf.RightArm)
                    || (pawn.stances?.curStance as Stance_Busy)?.verb == null;
        }

        /// <summary>
        /// Gets the outer most parent of a supplied body part.
        /// </summary>
        private static BodyPartRecord GetOuterMostParent(BodyPartRecord part)
        {
            var curPart = part;
            if (curPart != null)
            {
                while (curPart.parent != null && curPart.depth != BodyPartDepth.Outside)
                {
                    curPart = curPart.parent;
                }
            }
            return curPart;
        }

        /// <summary>
        /// Helper function to quickly get the armorRatingStat of a damage info
        /// </summary>
        private static StatDef ArmorRatingStat(this ref DamageInfo dinfo)
        {
            return dinfo.Def.armorCategory.armorRatingStat;
        }

        /// <summary>
        /// Applies damage to a parry object based on its armor values. For ambient damage,
        /// percentage reduction is applied, direct damage uses deflection formulas.
        /// </summary>
        /// <param name="dinfo">DamageInfo to apply to parryThing</param>
        /// <param name="parryThing">Thing taking the damage</param>
        public static void ApplyParryDamage(DamageInfo origDinfo, Thing parryThing)
        {
            var dinfo = new DamageInfo(origDinfo);

            if (parryThing is Pawn pawn)
            {
                // Pawns run their own armor calculations
                dinfo.SetAmount(dinfo.Amount * Mathf.Clamp01(Rand.Range(
                                0.5f - pawn.GetStatValue(CE_StatDefOf.MeleeParryChance),
                                1f - pawn.GetStatValue(CE_StatDefOf.MeleeParryChance) * 1.25f)));
                pawn.TakeDamage(dinfo);
                return;
            }

            if (dinfo.IsAmbientDamage())
            {
                dinfo.SetAmount(Mathf.CeilToInt(dinfo.Amount * Mathf.Clamp01(
                            parryThing.GetStatValue(dinfo.ArmorRatingStat()))));
                parryThing.TakeDamage(dinfo);
                return;
            }

            dinfo.SetAmount(dinfo.Amount * PARRY_THING_DAMAGE_FACTOR);
            // Initialize the AttackInfo linked list
            var ainfo = new AttackInfo(dinfo);
            if (dinfo.ArmorRatingStat() == StatDefOf.ArmorRating_Sharp)
            {
                var bluntDinfo = GetDeflectDamageInfo(dinfo);
                bluntDinfo.SetAmount(bluntDinfo.Amount * PARRY_THING_DAMAGE_FACTOR);
                ainfo.Append(bluntDinfo);
            }

            // Shield was used to parry
            if (parryThing is Apparel app)
            {
                for (var info = ainfo;
                        info != null && app != null && !app.Destroyed;
                        info = info.next)
                {
                    var blockedPenAmount = PenetrateArmor(info, app);
                    // Blocked penetration amount decays into next attack penetration
                    if (info.next != null)
                    {
                        info.next.penAmount += info.penTransferRate * blockedPenAmount;
                    }
                }
                return;
            }

            // Weapon was used to parry
            if (parryThing.def.IsWeapon)
            {
                for (var info = ainfo;
                        info != null && parryThing != null && !parryThing.Destroyed;
                        info = info.next)
                {
                    // Blocked penetration amount decays into next attack penetration
                    var blockedPenAmount = PenetrateWeapon(info, parryThing);
                    if (info.next != null)
                    {
                        info.next.penAmount += info.penTransferRate * blockedPenAmount;
                    }
                }
                return;
            }
        }

        /// <summary>
        /// Determines whether a dinfo is of an ambient (i.e. heat, electric) damage type
        /// and should apply percentage reduction, as opposed to deflection-based reduction.
        /// </summary>
        /// <param name="dinfo"></param>
        /// <returns>True if dinfo armor category is Heat or Electric, false otherwise</returns>
        private static bool IsAmbientDamage(this DamageInfo dinfo)
        {
            return (dinfo.Def.GetModExtension<DamageDefExtensionCE>()
                    ?? new DamageDefExtensionCE()).isAmbientDamage;
        }

        /// <summary>
        /// Calculates damage reduction for ambient damage types (fire, electricity) versus natural
        /// and worn armor of a pawn. Adds up the total armor percentage (clamped at 0-100%) and
        /// multiplies damage by that amount.
        /// </summary>
        /// <param name="dmgAmount">The original amount of damage</param>
        /// <param name="armorRatingStat">The armor stat to use for damage reduction</param>
        /// <param name="pawn">The damaged pawn</param>
        /// <param name="part">The body part affected</param>
        /// <returns>The post-armor damage ranging from 0 to the original amount</returns>
        private static float GetAmbientPostArmorDamage(float dmgAmount, StatDef armorRatingStat,
                Pawn pawn, BodyPartRecord part)
        {
            var dmgMult = 1f;
            if (part.IsInGroup(CE_BodyPartGroupDefOf.CoveredByNaturalArmor))
            {
                dmgMult -= pawn.PartialStat(armorRatingStat, part);
            }

            if (dmgMult <= 0)
            {
                return 0;
            }
            if (pawn.apparel != null && !pawn.apparel.WornApparel.NullOrEmpty())
            {
                var apparelList = pawn.apparel.WornApparel;
                foreach (var apparel in apparelList)
                {
                    if (apparel.def.apparel.CoversBodyPart(part))
                    {
                        dmgMult -= apparel.PartialStat(armorRatingStat, part);
                    }
                    if (dmgMult <= 0)
                    {
                        dmgMult = 0;
                        break;
                    }
                }
            }

            var deflectionComp = pawn.TryGetComp<Comp_BurnDamageCalc>();
            if (deflectionComp != null)
            {
                if (armorRatingStat == StatDefOf.ArmorRating_Heat)
                {
                    if (deflectionComp.deflectedSharp)
                    {
                        dmgMult /= 2f;
                    }
                }
                deflectionComp.deflectedSharp = false;
            }

            return (float)Math.Floor(dmgAmount * dmgMult);
        }

        #endregion
    }
}
