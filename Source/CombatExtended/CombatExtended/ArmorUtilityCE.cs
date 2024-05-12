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

        private const float PARRY_THING_DAMAGE_FACTOR = 0.5f;

        #endregion

        #region Properties

        // Used as a constant, defines what stuff makes armor be considered soft
        private static readonly StuffCategoryDef[] SOFT_STUFFS = {
            StuffCategoryDefOf.Fabric, StuffCategoryDefOf.Leathery
        };

        #endregion

        #region Methods

        /// <summary>
        /// Calculates damage through armor, depending on damage type, target and natural resistance.
        /// Also calculates deflection and adjusts damage type and impacted body part accordingly.
        /// </summary>
        /// <param name="originalDinfo">The pre-armor damage info</param>
        /// <param name="pawn">The damaged pawn</param>
        /// <param name="hitPart">The pawn's body part that has been hit</param>
        /// <param name="armorDeflected">Whether the attack was completely absorbed by the armor</param>
        /// <param name="armorReduced">Whether sharp damage was deflected by armor</param>
        /// <param name="skipSecondary">Whether secondary damage application should be skipped</param>
        /// <returns>
        /// If shot is deflected returns a new dinfo cloned from the original with damage amount, Def and ForceHitPart adjusted for deflection, otherwise a clone with only the damage adjusted</returns>
        public static DamageInfo GetAfterArmorDamage(DamageInfo originalDinfo, Pawn pawn,
                BodyPartRecord hitPart, out bool armorDeflected, out bool armorReduced,
                out bool skipSecondary)
        {
            armorDeflected = false;
            armorReduced = false; // Unused
            skipSecondary = false;

            if (originalDinfo.Def.armorCategory == null
                    || (!(originalDinfo.Weapon?.projectile is ProjectilePropertiesCE)
                        && Verb_MeleeAttackCE.LastAttackVerb == null
                        && originalDinfo.Weapon == null
                        && originalDinfo.Instigator == null))
            {
                return originalDinfo;
            }
            if (originalDinfo.IsAmbientDamage())
            {
                originalDinfo.SetAmount(Mathf.CeilToInt(
                            GetAmbientPostArmorDamage(originalDinfo.Amount,
                                originalDinfo.ArmorRatingStat(),
                                pawn,
                                hitPart)));
                armorDeflected = originalDinfo.Amount <= 0;
                return originalDinfo;
            }

            var deflectionComp = pawn.TryGetComp<Comp_BurnDamageCalc>();
            if (deflectionComp != null)
            {
                deflectionComp.deflectedSharp = false;
            }

            var dinfo = new DamageInfo(originalDinfo);
            Log.Warning($"DEBUG: damage hitpart {dinfo.HitPart} vs arg hitpart {hitPart}");
            float penAmount = dinfo.ArmorPenetrationInt;
            if (penAmount <= 0f)
            {
                Log.Warning($"[CE] Attack {dinfo} has negative or zero penetration");
                dinfo.SetAmount(0);
                return dinfo;
            }
            float dmgAmount = dinfo.Amount;
            float bluntPenAmount = 0f;
            float bluntPerPenAmount = dinfo.IsSharp() ? dinfo.GetBluntPenetration() / penAmount : 0f;

            var involveArmor = dinfo.Def.harmAllLayersUntilOutside
                || hitPart.depth == BodyPartDepth.Outside;

            if (involveArmor && pawn.apparel != null && !pawn.apparel.WornApparel.NullOrEmpty())
            {
                var apparel = pawn.apparel.WornApparel;

                // Check for shield first
                var app = apparel.FirstOrDefault(x => x is Apparel_Shield);
                if (app != null && ShouldCheckAgainstShield(dinfo, pawn, app))
                {
                    // Base penetration
                    bluntPenAmount += bluntPerPenAmount * ResistPenetration(dinfo.Def,
                            ref dmgAmount, ref penAmount,
                            app.PartialStat(dinfo.ArmorRatingStat(), hitPart), app);

                    // Apply secondary damage if attack was deflected
                    if (penAmount <= 0f)
                    {
                        skipSecondary = true;
                        if (dinfo.Weapon?.projectile is ProjectilePropertiesCE props
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
                                var secDinfo = sec.GetDinfo();
                                var secDmgAmount = secDinfo.Amount;
                                var secPenAmount = secDinfo.ArmorPenetrationInt;
                                ResistPenetration(secDinfo.Def, ref secDmgAmount, ref secPenAmount,
                                        app.PartialStat(secDinfo.ArmorRatingStat(), hitPart), app);
                            }
                        }
                    }

                    // After-forces penetration
                    if (bluntPenAmount > 0f)
                    {
                        var bluntDmgAmount = GetDamageFromBluntPenetration(ref dinfo, bluntPenAmount);
                        ResistPenetration(DamageDefOf.Blunt, ref bluntDmgAmount, ref bluntPenAmount,
                                    app.PartialStat(StatDefOf.ArmorRating_Blunt, hitPart), app);
                        if (bluntPenAmount > 0f && dinfo.Weapon?.projectile is ProjectilePropertiesCE)
                        {
                            bluntPenAmount *= PROJECTILE_SHIELD_BLUNT_PEN_FACTOR;
                        }
                    }

                    // Check if attack was deflected/completely stopped
                    if (penAmount <= 0f)
                    {
                        if (dinfo.IsSharp() && deflectionComp != null)
                        {
                            deflectionComp.deflectedSharp = true;
                        }
                        // The after-forces become the main attack (deflection)
                        dinfo = GetDeflectDamageInfo(ref dinfo, ref bluntPenAmount,
                                out dmgAmount, out penAmount, out bluntPerPenAmount);
                        // If no after-forces, attack has been stopped
                        if (penAmount <= 0f)
                        {
                            armorDeflected = true;
                            return dinfo;
                        }

                        // The attack has been deflected, but not stopped - update its target
                        var parts = pawn.health.hediffSet.GetNotMissingParts(
                                depth: BodyPartDepth.Outside,
                                tag: BodyPartTagDefOf.ManipulationLimbCore);
                        BodyPartRecord partToHit = parts.FirstOrFallback(x =>
                                x.IsInGroup(CE_BodyPartGroupDefOf.LeftArm)
                                    || x.IsInGroup(CE_BodyPartGroupDefOf.RightArm));
                        if (partToHit == null)
                        {
                            partToHit = parts.First(x => x.IsInGroup(CE_BodyPartGroupDefOf.LeftShoulder));
                        }
                        dinfo.SetHitPart(partToHit);
                    }
                }

                // Apparel is arranged in draw order, we run through reverse to go from Shell -> OnSkin
                for (var i = apparel.Count - 1; i >= 0; i--)
                {
                    app = apparel[i];
                    if (app == null
                            || !app.def.apparel.CoversBodyPart(hitPart)
                            || app is Apparel_Shield)
                    {
                        continue;
                    }

                    // Base penetration
                    bluntPenAmount += bluntPerPenAmount * ResistPenetration(dinfo.Def,
                            ref dmgAmount, ref penAmount,
                            app.PartialStat(dinfo.ArmorRatingStat(), hitPart), app);

                    // After-forces penetration
                    if (bluntPenAmount > 0f)
                    {
                        var bluntDmgAmount = GetDamageFromBluntPenetration(ref dinfo, bluntPenAmount);
                        ResistPenetration(DamageDefOf.Blunt, ref bluntDmgAmount, ref bluntPenAmount,
                                    app.PartialStat(StatDefOf.ArmorRating_Blunt, hitPart), app);
                    }

                    // Check if attack was deflected/completely stopped
                    if (penAmount <= 0f)
                    {
                        if (dinfo.IsSharp() && deflectionComp != null)
                        {
                            deflectionComp.deflectedSharp = true;
                        }
                        // The after-forces become the main attack (deflection)
                        dinfo = GetDeflectDamageInfo(ref dinfo, ref bluntPenAmount,
                                out dmgAmount, out penAmount, out bluntPerPenAmount);
                        // If no after-forces, attack has been stopped
                        if (penAmount <= 0f)
                        {
                            armorDeflected = true;
                            return dinfo;
                        }
                    }
                }
            }

            // Apply natural armor

            // Set amount of initial damage info because GetDeflectDamageInfo modifies dmgAmount
            dinfo.SetAmount(dmgAmount);

            // Apply the after-forces damage
            if (bluntPenAmount > 0f)
            {
                var bluntDinfo = GetDeflectDamageInfo(ref dinfo, ref bluntPenAmount,
                        out dmgAmount, out penAmount, out bluntPerPenAmount);
                // Set to true so as not to cause infinite recursion
                bluntDinfo.SetIgnoreArmor(true);
                // Using the same damage worker as of the original
                dinfo.Def.Worker.Apply(bluntDinfo, pawn);
            }

            // Return the initial damage info
            return dinfo;
        }

        /// <summary>
        /// Shorthand for getting the armor rating stat of a damage info.
        /// </summary>
        /// <param name="dinfo">The DamageInfo from which to get the armor rating stat</param>
        /// <returns>The armor rating StatDef</returns>
        private static StatDef ArmorRatingStat(this ref DamageInfo dinfo)
        {
            return dinfo.Def.armorCategory.armorRatingStat;
        }

        /// <summary>
        /// Checks if the damage info's damage type is sharp.
        /// </summary>
        /// <param name="dinfo">The DamageInfo to check</param>
        /// <returns>True if the damage info's damage type is sharp, false otherwise</returns>
        private static bool IsSharp(this ref DamageInfo dinfo)
        {
            return dinfo.Def.armorCategory == DamageArmorCategoryDefOf.Sharp;
        }

        /// <summary>
        /// Checks if the damage info should have to go through a shield used by a pawn. Makes sure
        /// that the damage info is not from a melee weapon, is covered by the shield, and if it is
        /// the right arm, that the pawn's right arm isn't exposed
        /// </summary>
        /// <param name="dinfo">The DamageInfo to check</param>
        /// <param name="pawn">The Pawn who wields the shield</param>
        /// <param name="shield">The shield used</param>
        /// <returns>True if the damage info has to go through the shield, false otherwise</returns>
        private static bool ShouldCheckAgainstShield(DamageInfo dinfo, Pawn pawn, Apparel shield)
        {
            // Don't check for shields against attacks from melee weapons
            // Shields are already used in parrying
            if (dinfo.Weapon?.IsMeleeWeapon ?? false)
            {
                return false;
            }
            var shieldDef = shield.def.GetModExtension<ShieldDefExtension>();
            if (shieldDef == null)
            {
                Log.ErrorOnce($"[CE] {shield.def} is Apparel_Shield but lacks ShieldDefExtension",
                        shield.def.GetHashCode() + 12748102);
                return false;
            }
            var hitPart = dinfo.HitPart;
            if (!shieldDef.PartIsCoveredByShield(hitPart, pawn.IsCrouching()))
            {
                return false;
            }
            // Right arm is vulnerable during warmup/attack/cooldown
            return !hitPart.IsInGroup(CE_BodyPartGroupDefOf.RightArm)
                    || (pawn.stances?.curStance as Stance_Busy)?.verb == null;
        }

        /// <summary>
        /// Applies resistance to penetration and a reduction to its damage based on the armor amount.
        /// If the armor Thing is given, damages it.
        /// </summary>
        /// <param name="def">The DamageDef of the attack</param>
        /// <param name="dmgAmount">The amount of damage in the given attack. Will be modified</param>
        /// <param name="penAmount">The amount of penetration in the given attack. Will be modified</param>
        /// <param name="armorAmount">The amount of armor to resist with</param>
        /// <param name="armor">The armor apparel to damage</param>
        /// <returns>By how much penAmount was reduced (the smaller of armorAmount and penAmount)</returns>
        private static float ResistPenetration(DamageDef def, ref float dmgAmount, ref float penAmount,
                float armorAmount, Thing armor)
        {
            float blockedPenAmount = penAmount < armorAmount ? penAmount : armorAmount;
            float newPenAmount = penAmount - blockedPenAmount;
            float newDmgAmount = dmgAmount * (newPenAmount / penAmount);
            float armorDamage = 0f;

            if (armor != null)
            {
                var isSharpDmg = def.armorCategory == DamageArmorCategoryDefOf.Sharp;
                var isBluntDmg = def.armorCategory == CE_DamageArmorCategoryDefOf.Blunt;
                var isFireDmg = def.armorCategory == CE_DamageArmorCategoryDefOf.Heat;
                var isSoftArmor = armor.Stuff?.stuffProps.categories
                    .Any(s => SOFT_STUFFS.Contains(s))
                    ?? false;

                // Fire damage checks against flammability
                if (isFireDmg)
                {
                    armorDamage = armor.GetStatValue(StatDefOf.Flammability, true) * dmgAmount;
                }
                // Soft armor takes damage from blocked sharp and none from blunt damage.
                else if (isSoftArmor)
                {
                    if (isSharpDmg)
                    {
                        armorDamage = Mathf.Max(dmgAmount * SOFT_ARMOR_MIN_SHARP_DAMAGE_FACTOR,
                                dmgAmount - newDmgAmount);
                    }
                }
                // Hard armor takes damage depending on the damage amount and damage penetration
                // Most damage is taken when armor penetration is the same as armor amount
                // Because lower is a higher degree of failure to penetrate (smaller bulge in the armor)
                // and higher is a higher degree of over-penetration (cleaner hole in the armor)
                // It is assumed that elastic deformation (no damage) occurs when the attack is blunt
                // and has less armor penetration than the armor amount divided by 2
                else
                {
                    if (penAmount == 0 || armorAmount == 0)
                    {
                        Log.WarningOnce($"[CE] Penetration amount {penAmount} or armor amount {armorAmount} "
                                + $"is zero for attack of type {def} against {armor}",
                                def.GetHashCode() + armor.def.GetHashCode() + 846532021);
                    }

                    float penArmorRatio = penAmount / armorAmount;
                    if (!isBluntDmg || penArmorRatio >= 0.5f)
                    {
                        var blockedDmgAmount = dmgAmount - newDmgAmount;
                        armorDamage = blockedDmgAmount * Mathf.Clamp01(penArmorRatio * penArmorRatio)
                            + newDmgAmount * Mathf.Clamp01(1.0f / penArmorRatio);

                        armorDamage *= HARD_ARMOR_DAMAGE_FACTOR;
                    }
                }
            }

            if (armorDamage > 0f)
            {
                TryDamageArmor(def, armorDamage, armor);
            }
            penAmount = newPenAmount;
            dmgAmount = newDmgAmount;
            return blockedPenAmount;
        }

        /// <summary>
        /// Damages the armor by the amount rounded up or down based on the fractional part.
        /// </summary>
        /// <param name="def">The DamageDef of the attack</param>
        /// <param name="armorDamage">The amount of damage to apply</param>
        /// <param name="armor">The armor to damage</param>
        /// <returns>True if armor was damaged, otherwise false</returns>
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
                armor.TakeDamage(new DamageInfo(def, armorDamage));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the blunt penetration of a damage info. For blunt damages, returns ArmorPenetrationInt,
        /// for sharp damages, tries to get it from the projectile, the melee verb the trap or just
        /// gives up and gives back a flat value. Otherwise returns zero.
        /// </summary>
        /// <param name="dinfo">The DamageInfo of which to get the blunt penetration</param>
        /// <returns>A float which is the blunt penetration value</returns>
        private static float GetBluntPenetration(this ref DamageInfo dinfo)
        {
            var armorStat = dinfo.ArmorRatingStat();
            if (armorStat == StatDefOf.ArmorRating_Blunt)
            {
                return dinfo.ArmorPenetrationInt;
            }
            if (armorStat != StatDefOf.ArmorRating_Sharp)
            {
                return 0f;
            }
            if (dinfo.Weapon?.projectile is ProjectilePropertiesCE projectile)
            {
                return projectile.armorPenetrationBlunt;
            }
            if (dinfo.Instigator?.def.thingClass == typeof(Building_TrapDamager))
            {
                return dinfo.Instigator.GetStatValue(StatDefOf.TrapMeleeDamage, true)
                    * TRAP_BLUNT_PEN_FACTOR;
            }
            if (Verb_MeleeAttackCE.LastAttackVerb != null)
            {
                return Verb_MeleeAttackCE.LastAttackVerb.ArmorPenetrationBlunt;
            }

            Log.Warning($"[CE] Deflection for Instigator:{dinfo.Instigator} Target:{dinfo.IntendedTarget} "
                    + $"DamageDef:{dinfo.Def} Weapon:{dinfo.Weapon} has null verb, overriding AP.");
            return 50;
        }

        /// <summary>
        /// Calculates the damage of a blunt penetration. Used by sharp attacks.
        /// Respects damage fragmentation from projectiles.
        /// </summary>
        /// <param mame="info"></param>
        private static float GetDamageFromBluntPenetration(ref DamageInfo dinfo, float bluntPenAmount)
        {
            float result = Mathf.Pow(bluntPenAmount * 10000, 1 / 3f) / 10;
            if (dinfo.Weapon?.projectile is ProjectilePropertiesCE)
            {
                if (dinfo.Weapon.projectile.damageAmountBase != 0)
                {
                    result *= dinfo.Amount / (float)dinfo.Weapon.projectile.damageAmountBase;
                }
            }
            // TODO: maybe check for more than projectile damage fragmentation?
            return result;
        }

        /// <summary>
        /// Creates a new DamageInfo from a deflected one. Changes damage type to Blunt
        /// and hit part to the outermost parent of the originally hit part.
        /// </summary>
        /// <param name="dinfo">The dinfo that was deflected</param>
        /// <param name="hitPart">The originally hit part</param>
        /// <param name="partialPen">Is this is supposed to be a partial penetration</param>
        /// <returns>DamageInfo copied from dinfo with Def and forceHitPart adjusted</returns>
        private static DamageInfo GetDeflectDamageInfo(ref DamageInfo dinfo, ref float bluntPenAmount,
                out float dmgAmount, out float penAmount, out float bluntPerPenAmount)
        {
            DamageInfo newDinfo = new DamageInfo(DamageDefOf.Blunt,
                    GetDamageFromBluntPenetration(ref dinfo, bluntPenAmount),
                    bluntPenAmount,
                    dinfo.Angle,
                    dinfo.Instigator,
                    GetOuterMostParent(dinfo.HitPart));
            newDinfo.SetBodyRegion(dinfo.Height, dinfo.Depth);
            newDinfo.SetWeaponBodyPartGroup(dinfo.WeaponBodyPartGroup);
            newDinfo.SetWeaponHediff(dinfo.WeaponLinkedHediff);
            newDinfo.SetInstantPermanentInjury(dinfo.InstantPermanentInjury);
            newDinfo.SetAllowDamagePropagation(dinfo.AllowDamagePropagation);

            penAmount = newDinfo.ArmorPenetrationInt;
            dmgAmount = newDinfo.Amount;
            bluntPenAmount = 0f;
            bluntPerPenAmount = 0f;
            return newDinfo;
        }

        /// <summary>
        /// Retrieves the first part that is defined as being outside, going up the parents tree until
        /// such part is found or the end of the tree is reached.
        /// </summary>
        /// <param name="part">The part to get the parent of</param>
        /// <returns>The first part found that is of depth Outside, otherwise the root part</returns>
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
        /// Applies damage to a parry object based on its armor values. For ambient damage,
        /// percentage reduction is applied, direct damage uses deflection formulas.
        /// </summary>
        /// <param name="dinfo">DamageInfo to apply to parryThing</param>
        /// <param name="parryThing">Thing taking the damage</param>
        public static void ApplyParryDamage(DamageInfo dinfo, Thing parryThing)
        {
            if (parryThing is Pawn pawn)
            {
                // Pawns run their own armor calculations
                dinfo.SetAmount(dinfo.Amount * Mathf.Clamp01(Rand.Range(
                                0.5f - pawn.GetStatValue(CE_StatDefOf.MeleeParryChance),
                                1f - pawn.GetStatValue(CE_StatDefOf.MeleeParryChance) * 1.25f)));
                pawn.TakeDamage(dinfo);
            }
            else if (dinfo.IsAmbientDamage())
            {
                dinfo.SetAmount(Mathf.CeilToInt(dinfo.Amount * Mathf.Clamp01(
                            parryThing.GetStatValue(dinfo.Def.armorCategory.armorRatingStat))));
                parryThing.TakeDamage(dinfo);
            }
            else
            {
                var penAmount = dinfo.ArmorPenetrationInt;
                if (penAmount <= 0f)
                {
                    Log.Warning($"[CE] Parried attack {dinfo} has negative or zero penetration");
                    return;
                }
                var dmgAmount = dinfo.Amount * PARRY_THING_DAMAGE_FACTOR;
                float bluntPenAmount = 0f;
                float bluntPerPenAmount = dinfo.IsSharp() ? dinfo.GetBluntPenetration() / penAmount : 0f;

                float armorAmount = 0f;
                // For apparel
                if (parryThing is Apparel app)
                {
                    armorAmount = app.PartialStat(dinfo.Def.armorCategory.armorRatingStat, dinfo.HitPart);
                }
                // Special case for weapons
                else
                {
                    armorAmount = parryThing.GetStatValue(CE_StatDefOf.ToughnessRating);
                    // Compensation for blunt damage against weapons
                    if (dinfo.Def.armorCategory == CE_DamageArmorCategoryDefOf.Blunt)
                    {
                        armorAmount *= 1.5f;
                    }
                }

                bluntPenAmount += bluntPerPenAmount * ResistPenetration(dinfo.Def,
                        ref dmgAmount, ref penAmount, armorAmount, parryThing);

                // Blunt after-forces applied to the weapon
                if (bluntPenAmount > 0f)
                {
                    armorAmount *= 1.5f;
                    dinfo = GetDeflectDamageInfo(ref dinfo, ref bluntPenAmount,
                            out dmgAmount, out penAmount, out bluntPerPenAmount);
                    ResistPenetration(dinfo.Def, ref dmgAmount, ref penAmount,
                            armorAmount, parryThing);
                }
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
