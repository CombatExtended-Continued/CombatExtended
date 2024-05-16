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

        #region Classes

        internal class AttackInfo
        {
            public DamageInfo dinfo;
            public float penAmount = 0f;
            public float dmgPerPenAmount = 0f;
            public float penTransferRate = 0f;
            public AttackInfo next;

            public AttackInfo(DamageInfo info)
            {
                dinfo = info;
                penAmount = dinfo.ArmorPenetrationInt;
                dmgPerPenAmount = penAmount > 0f
                    ? dinfo.Amount / penAmount
                    : 0f;
            }

            public void Push(AttackInfo info)
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

            public void Push(DamageInfo info)
            {
                Push(new AttackInfo(info));
            }

            public AttackInfo FirstValidOrLast()
            {
                AttackInfo newHead = this;
                while (newHead.penAmount <= 0f && newHead.next != null)
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
        /// <param name="hitPart">The pawn's body part that has been hit</param>
        /// <param name="armorDeflected">Whether the attack was completely absorbed by the armor</param>
        /// <param name="armorReduced">Whether sharp damage was deflected by armor</param>
        /// <param name="skipSecondary">Whether secondary damage application should be skipped</param>
        /// <returns>
        /// If shot is deflected returns a new dinfo cloned from the original with damage amount,
        /// Def and ForceHitPart adjusted for deflection, otherwise a clone with only the damage adjusted
        /// </returns>
        public static DamageInfo GetAfterArmorDamage(DamageInfo origDinfo, Pawn pawn,
                BodyPartRecord _, out bool armorDeflected, out bool armorReduced,
                out bool skipSecondary)
        {
            Log.Warning($"GetAfterArmorDamage origDinfo={origDinfo} pawn={pawn}");
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
            dinfo.SetBodyRegion(dinfo.HitPart.height, dinfo.HitPart.depth);

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

            var ainfo = new AttackInfo(dinfo);
            if (dinfo.ArmorRatingStat() == StatDefOf.ArmorRating_Sharp)
            {
                ainfo.Push(GetDeflectDamageInfo(dinfo));
            }
            var dworker = dinfo.Def.Worker;

            var involveArmor = ainfo.dinfo.Def.harmAllLayersUntilOutside
                || ainfo.dinfo.HitPart.depth == BodyPartDepth.Outside;
            if (involveArmor && pawn.apparel != null && !pawn.apparel.WornApparel.NullOrEmpty())
            {
                var apparel = pawn.apparel.WornApparel;
                Apparel app;

                app = apparel.FirstOrDefault(x => x is Apparel_Shield);
                if (app != null && ShouldCheckShield(ainfo.dinfo, pawn, app))
                {
                    for (var info = ainfo; info != null && !app.Destroyed; info = info.next)
                    {
                        var blockedPenAmount = PenetrateArmor(info, app);
                        if (info.penTransferRate > 0f)
                        {
                            info.next.penAmount += info.penTransferRate * blockedPenAmount;
                        }
                    }

                    if (ainfo.penAmount <= 0f)
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

                        if (ainfo.dinfo.ArmorRatingStat() == StatDefOf.ArmorRating_Sharp
                                && deflectionComp != null)
                        {
                            deflectionComp.deflectedSharp = true;
                        }

                        var parts = pawn.health.hediffSet.GetNotMissingParts(
                                depth: BodyPartDepth.Outside,
                                tag: BodyPartTagDefOf.ManipulationLimbCore);
                        BodyPartRecord partToHit = parts.FirstOrFallback(x =>
                                x.IsInGroup(CE_BodyPartGroupDefOf.LeftArm)
                                    || x.IsInGroup(CE_BodyPartGroupDefOf.RightArm));
                        // Cannot wear shields without a left shoulder
                        if (partToHit == null)
                        {
                            partToHit = parts.First(x => x.IsInGroup(CE_BodyPartGroupDefOf.LeftShoulder));
                        }

                        ainfo = ainfo.FirstValidOrLast();
                        if (ainfo.penAmount <= 0f)
                        {
                            ainfo.dinfo.SetAmount(0f);
                            return ainfo.dinfo;
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

                for (var i = apparel.Count - 1; i >= 0; i--)
                {
                    app = apparel[i];
                    if (app == null
                            || !app.def.apparel.CoversBodyPart(dinfo.HitPart)
                            || app is Apparel_Shield)
                    {
                        continue;
                    }

                    for(var info = ainfo; info != null; info = info.next)
                    {
                        var blockedPenAmount = PenetrateArmor(info, app);
                        if (info.penTransferRate > 0f)
                        {
                            info.next.penAmount += info.penTransferRate * blockedPenAmount;
                        }
                    }

                    if (ainfo.penAmount <= 0f)
                    {
                        if (ainfo.dinfo.ArmorRatingStat() == StatDefOf.ArmorRating_Sharp
                                && deflectionComp != null)
                        {
                            deflectionComp.deflectedSharp = true;
                        }

                        ainfo = ainfo.FirstValidOrLast();
                        if (ainfo.penAmount <= 0f)
                        {
                            ainfo.dinfo.SetAmount(0f);
                            return ainfo.dinfo;
                        }
                    }
                }
            }

            var sharpPartDensity = pawn.GetStatValue(CE_StatDefOf.BodyPartSharpArmor);
            var bluntPartDensity = pawn.GetStatValue(CE_StatDefOf.BodyPartBluntArmor);
            for (var info = ainfo; info != null; info = info.next)
            {
                var partsToHit = new List<BodyPartRecord>()
                {
                    info.dinfo.HitPart
                };

                if (info.dinfo.Depth == BodyPartDepth.Inside
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
                if(armorRatingStat == StatDefOf.ArmorRating_Sharp)
                {
                    partDensity = sharpPartDensity;
                }
                else if(armorRatingStat == StatDefOf.ArmorRating_Blunt)
                {
                    partDensity = bluntPartDensity;
                }

                for (var i = partsToHit.Count - 1; i >= 0; i--)
                {
                    var partToHit = partsToHit[i];
                    var postArmorAmount = i > 0 ? partDensity : 0f;
                    PenetrateBodyPart(info, dworker, pawn, partToHit, postArmorAmount);
                }
            }

            if (ainfo.penAmount <= 0f)
            {
                if (ainfo.dinfo.ArmorRatingStat() == StatDefOf.ArmorRating_Sharp
                        && deflectionComp != null)
                {
                    deflectionComp.deflectedSharp = true;
                }

                ainfo = ainfo.FirstValidOrLast();
                if (ainfo.penAmount <= 0f)
                {
                    ainfo.dinfo.SetAmount(0f);
                    return ainfo.dinfo;
                }
            }

            for (var info = ainfo.next; info != null; info = info.next)
            {
                info.dinfo.SetIgnoreArmor(true);
                info.dinfo.weaponInt = null;
                info.dinfo.SetAmount(info.penAmount * info.dmgPerPenAmount);
                Log.Warning($"GetAfterArmorDamage additional damage {info.dinfo}");
                dworker.Apply(info.dinfo, pawn);
            }

            Log.Warning($"GetAfterArmorDamage return {ainfo.dinfo}");
            ainfo.dinfo.SetAmount(ainfo.penAmount * ainfo.dmgPerPenAmount);
            return ainfo.dinfo;
        }

        private static float PenetrateArmor(AttackInfo ainfo, Apparel armor)
        {
            var armorAmount = armor.PartialStat(ainfo.dinfo.ArmorRatingStat(), ainfo.dinfo.HitPart);
            return PenetrateArmor(ainfo, armor, armorAmount);
        }

        private static float PenetrateArmor(AttackInfo ainfo, Thing armor, float armorAmount)
        {
            Log.Warning($"PenetrateArmor ainfo={ainfo} armor={armor} armorAmount={armorAmount}");
            if (ainfo.penAmount <= 0f || armor == null || armor.Destroyed)
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
            else if (isSoftArmor)
            {
                if (armorRatingStat == StatDefOf.ArmorRating_Sharp)
                {
                    armorDamage = Mathf.Max(dmgAmount * SOFT_ARMOR_MIN_SHARP_DAMAGE_FACTOR,
                            blockedDmgAmount);
                }
            }
            else
            {
                if (penAmount == 0 || armorAmount == 0)
                {
                    Log.WarningOnce($"[CE] Penetration amount {penAmount} or armor amount {armorAmount} "
                            + $"is zero for attack of type {ainfo.dinfo.Def} against {armor}",
                            ainfo.dinfo.Def.GetHashCode() + armor.def.GetHashCode() + 846532021);
                }

                float penArmorRatio = penAmount / armorAmount;
                if (armorRatingStat == StatDefOf.ArmorRating_Sharp)
                {
                    armorDamage = blockedDmgAmount * Mathf.Clamp01(penArmorRatio * penArmorRatio)
                        + newDmgAmount * Mathf.Clamp01(1.0f / penArmorRatio);
                }
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

        private static float PenetrateBodyPart(AttackInfo ainfo, DamageWorker dworker,
                Pawn pawn, BodyPartRecord part, float postArmorAmount)
        {
            Log.Warning($"PenetrateBodyPart ainfo={ainfo} dworker={dworker} pawn={pawn} part={part} "
                    + $"postArmorAmount={postArmorAmount}");
            if (ainfo.penAmount <= 0f)
            {
                return 0f;
            }

            var armorAmount = part.IsInGroup(CE_BodyPartGroupDefOf.CoveredByNaturalArmor)
                ? pawn.PartialStat(ainfo.dinfo.ArmorRatingStat(), part)
                : 0f;
            var penAmount = ainfo.penAmount;
            var newPenAmount = Mathf.Max(penAmount - armorAmount, 0f);

            if (ainfo.dinfo.HitPart != part)
            {
                var dinfo = new DamageInfo(ainfo.dinfo);
                dinfo.SetHitPart(part);
                dinfo.SetBodyRegion(dinfo.HitPart.height, dinfo.HitPart.depth);
                dinfo.SetIgnoreArmor(true);
                dinfo.weaponInt = null;
                dinfo.SetAmount(newPenAmount * ainfo.dmgPerPenAmount);
                Log.Warning($"PenetrateBodyPart additional damage {dinfo}");
                dworker.Apply(dinfo, pawn);
            }

            newPenAmount = Mathf.Max(newPenAmount - postArmorAmount, 0f);
            ainfo.penAmount = newPenAmount;
            return penAmount - newPenAmount;
        }

        private static DamageInfo GetDeflectDamageInfo(DamageInfo dinfo)
        {
            Log.Warning($"GetDeflectDamageInfo dinfo={dinfo}");
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
            newDinfo.SetBodyRegion(newDinfo.HitPart.height, newDinfo.HitPart.depth);
            newDinfo.SetWeaponBodyPartGroup(dinfo.WeaponBodyPartGroup);
            newDinfo.SetWeaponHediff(dinfo.WeaponLinkedHediff);
            newDinfo.SetInstantPermanentInjury(dinfo.InstantPermanentInjury);
            newDinfo.SetAllowDamagePropagation(dinfo.AllowDamagePropagation);

            return newDinfo;
        }

        private static bool ShouldCheckShield(DamageInfo dinfo, Pawn pawn, Apparel shield)
        {
            Log.Warning($"ShouldCheckShield dinfo={dinfo} pawn={pawn} shield={shield}");
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

        private static bool TryDamageArmor(DamageDef def, float armorDamage, Thing armor)
        {
            Log.Warning($"TryDamageArmor def={def} armorDamage={armorDamage} armor={armor}");
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
            Log.Warning($"GetOuterMostParent {part} ret={curPart}");
            return curPart;
        }

        private static StatDef ArmorRatingStat(this ref DamageInfo dinfo)
        {
            return dinfo.Def.armorCategory.armorRatingStat;
        }

        public static void ApplyParryDamage(DamageInfo origDinfo, Thing parryThing)
        {
            Log.Warning($"ApplyParryDamage origDinfo={origDinfo} parryThing={parryThing}");
            var dinfo = new DamageInfo(origDinfo);
            dinfo.SetBodyRegion(dinfo.HitPart.height, dinfo.HitPart.depth);

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
            var ainfo = new AttackInfo(dinfo);
            if (dinfo.ArmorRatingStat() == StatDefOf.ArmorRating_Sharp)
            {
                var bluntDinfo = GetDeflectDamageInfo(dinfo);
                bluntDinfo.SetAmount(bluntDinfo.Amount * PARRY_THING_DAMAGE_FACTOR);
                ainfo.Push(bluntDinfo);
            }
            var dworker = dinfo.Def.Worker;

            if (parryThing is Apparel app)
            {
                for(var info = ainfo; info != null; info = info.next)
                {
                    var blockedPenAmount = PenetrateArmor(info, app);
                    if (info.penTransferRate > 0f)
                    {
                        info.next.penAmount += info.penTransferRate * blockedPenAmount;
                    }
                }
                return;
            }

            if (parryThing.def.IsWeapon)
            {
                for(var info = ainfo; info != null; info = info.next)
                {
                    var armorRatingStat = info.dinfo.ArmorRatingStat();
                    float armorAmount = 0f;
                    if(armorRatingStat == StatDefOf.ArmorRating_Sharp)
                    {
                        armorAmount = parryThing.GetStatValue(CE_StatDefOf.ToughnessRating);
                    }
                    else if (armorRatingStat == StatDefOf.ArmorRating_Blunt)
                    {
                        armorAmount = parryThing.GetStatValue(CE_StatDefOf.ToughnessRating) * 1.5f;
                    }

                    var blockedPenAmount = PenetrateArmor(info, parryThing, armorAmount);
                    if (info.penTransferRate > 0f)
                    {
                        info.next.penAmount += info.penTransferRate * blockedPenAmount;
                    }
                }
                return;
            }
        }

        private static float PenetrateWeapon(AttackInfo ainfo, Thing weapon)
        {
            Log.Warning($"PenetrateWeapon ainfo={ainfo} weapon={weapon}");
            if (ainfo.penAmount <= 0f || weapon.Destroyed)
            {
                return 0f;
            }

            var armorAmount = weapon.GetStatValue(CE_StatDefOf.ToughnessRating);
            return 0f;
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
