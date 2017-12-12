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

        private const float PenetrationRandVariation = 0.05f;    // Armor penetration will be randomized by +- this amount
        private const float SoftArmorMinDamageFactor = 0.2f;    // Soft body armor will always take at least original damage * this number from sharp attacks

        #endregion

        #region Properties

        private static readonly SimpleCurve dmgMultCurve = new SimpleCurve { new CurvePoint(0.5f, 0), new CurvePoint(1, 0.5f), new CurvePoint(2, 1) };    // Used to calculate the damage reduction from the penetration / armor ratio
        private static readonly StuffCategoryDef[] softStuffs = { StuffCategoryDefOf.Fabric, DefDatabase<StuffCategoryDef>.GetNamed("Leathery") };

        #endregion

        #region Methods

        /// <summary>
        /// Calculates damage through armor, depending on damage type, target and natural resistance. Also calculates deflection and adjusts damage type and impacted body part accordingly.
        /// </summary>
        /// <param name="originalDinfo">The pre-armor damage info</param>
        /// <param name="pawn">The damaged pawn</param>
        /// <param name="hitPart">The pawn's body part that has been hit</param>
        /// <param name="shieldAbsorbed">Returns true if attack did not penetrate pawn's melee shield</param>
        /// <returns>If shot is deflected returns a new dinfo cloned from the original with damage amount, Def and ForceHitPart adjusted for deflection, otherwise a clone with only the damage adjusted</returns>
        public static DamageInfo GetAfterArmorDamage(DamageInfo originalDinfo, Pawn pawn, BodyPartRecord hitPart, out bool shieldAbsorbed)
        {
            shieldAbsorbed = false;

            if (originalDinfo.Def.armorCategory == null) return originalDinfo;

            DamageInfo dinfo = new DamageInfo(originalDinfo);
            float dmgAmount = dinfo.Amount;
            bool involveArmor = dinfo.Def.harmAllLayersUntilOutside;
            bool isAmbientDamage = dinfo.IsAmbientDamage();

            // In case of ambient damage (fire, electricity) we apply a percentage reduction formula based on the sum of all applicable armor
            if (isAmbientDamage)
            {
                dinfo.SetAmount(Mathf.CeilToInt(GetAmbientPostArmorDamage(dmgAmount, originalDinfo.Def.armorCategory.deflectionStat, pawn, hitPart)));
                return dinfo;
            }

            float penAmount = GetPenetrationValue(originalDinfo);

            // Apply worn armor
            if (involveArmor && pawn.apparel != null && !pawn.apparel.WornApparel.NullOrEmpty())
            {
                List<Apparel> apparel = pawn.apparel.WornApparel;

                // Check for shields first
                Apparel shield = apparel.FirstOrDefault(x => x is Apparel_Shield);
                if (shield != null)
                {
                    // Determine whether the hit is blocked by the shield
                    bool blockedByShield = false;
                    if (!(dinfo.Weapon?.IsMeleeWeapon ?? false))
                    {
                        var shieldDef = shield.def.GetModExtension<ShieldDefExtension>();
                        if (shieldDef == null)
                        {
                            Log.ErrorOnce("CE :: shield " + shield.def.ToString() + " is Apparel_Shield but has no ShieldDefExtension", shield.def.GetHashCode() + 12748102);
                        }
                        else
                        {
                            bool hasCoverage = shieldDef.PartIsCoveredByShield(hitPart, pawn);
                            if (hasCoverage)
                            {
                                // Right arm is vulnerable during warmup/attack/cooldown
                                blockedByShield = !((pawn.stances?.curStance as Stance_Busy)?.verb != null && hitPart.IsInGroup(CE_BodyPartGroupDefOf.RightArm));
                            }
                        }
                    }
                    // Try to penetrate the shield
                    if (blockedByShield && !TryPenetrateArmor(dinfo.Def, shield.GetStatValue(dinfo.Def.armorCategory.deflectionStat), ref penAmount, ref dmgAmount, shield))
                    {
                        shieldAbsorbed = true;
                        dinfo.SetAmount(0);

                        // Apply secondary damage to shield
                        var props = dinfo.Weapon.projectile as ProjectilePropertiesCE;
                        if (props != null && !props.secondaryDamage.NullOrEmpty())
                        {
                            foreach(SecondaryDamage sec in props.secondaryDamage)
                            {
                                if (shield.Destroyed) break;
                                var secDinfo = sec.GetDinfo();
                                var pen = GetPenetrationValue(originalDinfo);
                                var dmg = (float)secDinfo.Amount;
                                TryPenetrateArmor(secDinfo.Def, shield.GetStatValue(secDinfo.Def.armorCategory.deflectionStat), ref pen, ref dmg, shield);
                            }
                        }

                        return dinfo;
                    }
                }

                // Apparel is arranged in draw order, we run through reverse to go from Shell -> OnSkin
                for (int i = apparel.Count - 1; i >= 0; i--)
                {
                    if (apparel[i].def.apparel.CoversBodyPart(hitPart) 
                        && !TryPenetrateArmor(dinfo.Def, apparel[i].GetStatValue(dinfo.Def.armorCategory.deflectionStat), ref penAmount, ref dmgAmount, apparel[i]))
                    {
                        // Hit was deflected, convert damage type
                        dinfo = GetDeflectDamageInfo(dinfo, hitPart);
                        i++;    // We apply this piece of apparel twice on conversion, this means we can't use deflection on Blunt or else we get an infinite loop of eternal deflection
                    }
                    if (dmgAmount <= 0)
                    {
                        dinfo.SetAmount(0);
                        return dinfo;
                    }
                }
            }

            // Apply natural armor
            List<BodyPartRecord> partsToHit = new List<BodyPartRecord>() { hitPart };
            if (involveArmor)
            {
                BodyPartRecord curPart = hitPart;
                while (curPart.parent != null && curPart.depth == BodyPartDepth.Inside)
                {
                    curPart = curPart.parent;
                    partsToHit.Add(curPart);
                }
            }
            for (int i = partsToHit.Count - 1; i >= 0; i--)
            {
                BodyPartRecord curPart = partsToHit[i];
                bool coveredByArmor = curPart.IsInGroup(CE_BodyPartGroupDefOf.CoveredByNaturalArmor);
                float partArmor = pawn.HealthScale * 0.05f;   // How much armor is provided by sheer meat
                if (coveredByArmor)
                    partArmor += pawn.GetStatValue(dinfo.Def.armorCategory.deflectionStat);
                float unused = dmgAmount;

                // Only apply damage reduction when penetrating armored body parts
                if (coveredByArmor ? !TryPenetrateArmor(dinfo.Def, partArmor, ref penAmount, ref dmgAmount) : !TryPenetrateArmor(dinfo.Def, partArmor, ref penAmount, ref unused))
                {
                    dinfo.SetHitPart(curPart);
                    /*
                    if(coveredByArmor && pawn.RaceProps.IsMechanoid)
                    {
                        // For Mechanoid natural armor, apply deflection and blunt armor
                        dinfo = GetDeflectDamageInfo(dinfo, curPart);
                        TryPenetrateArmor(dinfo.Def, partArmor, ref penAmount, ref dmgAmount);
                    }
                    */
                    break;
                }
                if (dmgAmount <= 0)
                {
                    dinfo.SetAmount(0);
                    return dinfo;
                }
            }

            dinfo.SetAmount(Mathf.CeilToInt(dmgAmount));
            return dinfo;
        }

        /// <summary>
        /// Determines the armor penetration value of a given dinfo. Checks WeaponGear to see if it is a projectile, melee weapon or pawn and tries to retrieve the penetration value accordingly.
        /// </summary>
        /// <param name="dinfo">DamageInfo to determine penetration for</param>
        /// <returns>Armor penetration value for attack used, 0 if it can't be determined</returns>
        private static float GetPenetrationValue(DamageInfo dinfo)
        {
            if (dinfo.Def.isExplosive)
            {
                return dinfo.Amount * 0.1f; // Explosions have 10% of their damage as penetration
            }
            
            if (dinfo.Weapon != null)
            {
                // Case 1: projectile attack
                ProjectilePropertiesCE projectileProps = dinfo.Weapon.projectile as ProjectilePropertiesCE;
                if (projectileProps != null)
                {
                    return projectileProps.armorPenetration;
                }

                // Case 2: melee attack
                Pawn instigatorPawn = dinfo.Instigator as Pawn;
                if (instigatorPawn != null)
                {
                    // Pawn is using melee weapon
                    if (dinfo.Weapon.IsWeapon)
                    {
                        var equipment = instigatorPawn.equipment?.Primary;
                        if (equipment == null || equipment.def != dinfo.Weapon)
                        {
                            Log.Error("CE tried getting armor penetration from melee weapon " + dinfo.Weapon.defName + " but instigator " + dinfo.Instigator.ToString() + " equipment does not match");
                            return 0;
                        }
                        var penetrationMult = equipment.GetStatValue(CE_StatDefOf.MeleeWeapon_Penetration);
                        var tool = equipment.def.tools.FirstOrDefault(t => t.linkedBodyPartsGroup == dinfo.WeaponBodyPartGroup) as ToolCE;
                        return tool.armorPenetration * penetrationMult;
                    }

                    // Get penetration from tool
                    if (instigatorPawn.verbTracker != null
                    && !instigatorPawn.verbTracker.AllVerbs.NullOrEmpty())
                    {
                        Verb verb = instigatorPawn.verbTracker.AllVerbs.FirstOrDefault(v => v.tool.linkedBodyPartsGroup == dinfo.WeaponBodyPartGroup);
                        if (verb == null)
                        {
                            Log.Error("CE could not find matching verb on Pawn " + instigatorPawn.ToString() + " for BodyPartGroup " + dinfo.WeaponBodyPartGroup.ToString());
                            return 0;
                        }
                        var tool = verb.tool as ToolCE;
                        if (tool != null) return tool.armorPenetration;
                    }

                    /*
                    // Pawn is using body parts
                    if (instigatorPawn.def == dinfo.Weapon)
                    {
                        // Pawn is augmented
                        if (dinfo.WeaponLinkedHediff != null)
                        {
                            HediffCompProperties_VerbGiver compProps = dinfo.WeaponLinkedHediff.CompPropsFor(typeof(HediffComp_VerbGiver)) as HediffCompProperties_VerbGiver;
                            if (compProps != null)
                            {
                                VerbPropertiesCE verbProps = compProps.verbs.FirstOrDefault(v => v as VerbPropertiesCE != null) as VerbPropertiesCE;
                                if (verbProps != null) return verbProps.meleeArmorPenetration;
                            }
                            return 0;
                        }

                        // Regular pawn melee
                        if (dinfo.WeaponBodyPartGroup != null
                        && instigatorPawn.verbTracker != null
                        && !instigatorPawn.verbTracker.AllVerbs.NullOrEmpty())
                        {
                            Verb verb = instigatorPawn.verbTracker.AllVerbs.FirstOrDefault(v => v.verbProps.linkedBodyPartsGroup == dinfo.WeaponBodyPartGroup);
                            if (verb == null)
                            {
                                Log.Error("CE could not find matching verb on Pawn " + instigatorPawn.ToString() + " for BodyPartGroup " + dinfo.WeaponBodyPartGroup.ToString());
                                return 0;
                            }
                            var tool = verb.tool as ToolCE;
                            if (tool != null) return tool.armorPenetration;
                        }
                    }
                    */
                }
            }
#if DEBUG
            Log.Warning("CE could not determine armor penetration, defaulting");
#endif
            return 9999;    // Really high default value so vanilla damage sources such as GiveInjuriesToKill always penetrate
        }

        /// <summary>
        /// Calculates armor for penetrating damage types (Blunt, Sharp). Applies damage reduction based on armor penetration to armor ratio and calculates damage accordingly, with the difference being applied to the armor Thing. Also calculates whether a Sharp attack is deflected.
        /// </summary>
        /// <param name="def">The DamageDef of the attack</param>
        /// <param name="armorAmount">The amount of armor to apply</param>
        /// <param name="penAmount">How much penetration the attack still has</param>
        /// <param name="dmgAmount">The pre-armor amount of damage</param>
        /// <param name="armor">The armor apparel</param>
        /// <returns>False if the attack is deflected, true otherwise</returns>
        private static bool TryPenetrateArmor(DamageDef def, float armorAmount, ref float penAmount, ref float dmgAmount, Thing armor = null)
        {
            // Calculate deflection
            bool isSharpDmg = def.armorCategory == DamageArmorCategoryDefOf.Sharp;
            float rand = UnityEngine.Random.Range(penAmount - PenetrationRandVariation, penAmount + PenetrationRandVariation);
            bool deflected = isSharpDmg && armorAmount > rand;

            // Apply damage reduction
            float dmgMult = 1;
            DamageDefExtensionCE defCE = def.GetModExtension<DamageDefExtensionCE>() ?? new DamageDefExtensionCE();
            if (deflected && defCE != null && defCE.noDamageOnDeflect) dmgMult = 0;
            else dmgMult = dmgMultCurve.Evaluate(penAmount / armorAmount);
            float newDmgAmount = dmgAmount * dmgMult;
            float newPenAmount = penAmount * dmgMult;

            // Apply damage to armor
            if (armor != null)
            {
                bool isSoftArmor = armor.Stuff != null && armor.Stuff.stuffProps.categories.Any(s => softStuffs.Contains(s));
                if (isSoftArmor)
                {
                    // Soft armor takes absorbed damage from sharp and no damage from blunt
                    if (isSharpDmg)
                    {
                        float armorDamage = Mathf.Max(dmgAmount * SoftArmorMinDamageFactor, dmgAmount - newDmgAmount);
                        armor.TakeDamage(new DamageInfo(def, Mathf.CeilToInt(armorDamage)));
                    }
                }
                else
                {
                    // Hard armor takes damage as reduced by damage resistance and can be almost impervious to low-penetration attacks
                    float armorDamage = Mathf.Max(1, newDmgAmount);
                    armor.TakeDamage(new DamageInfo(def, Mathf.CeilToInt(armorDamage)));
                }
            }

            dmgAmount = Mathf.Max(0, newDmgAmount);
            penAmount = Mathf.Max(0, newPenAmount);
            return !deflected;
        }

        /// <summary>
        /// Calculates damage reduction for ambient damage types (fire, electricity) versus natural and worn armor of a pawn. Adds up the total armor percentage (clamped at 0-100%) and multiplies damage by that amount.
        /// </summary>
        /// <param name="dmgAmount">The original amount of damage</param>
        /// <param name="deflectionStat">The armor stat to use for damage reduction</param>
        /// <param name="pawn">The damaged pawn</param>
        /// <param name="part">The body part affected</param>
        /// <returns>The post-armor damage ranging from 0 to the original amount</returns>
        private static float GetAmbientPostArmorDamage(float dmgAmount, StatDef deflectionStat, Pawn pawn, BodyPartRecord part)
        {
            float dmgMult = 1 - pawn.GetStatValue(deflectionStat);
            if (dmgMult <= 0) return 0;
            if (pawn.apparel != null && !pawn.apparel.WornApparel.NullOrEmpty())
            {
                List<Apparel> apparelList = pawn.apparel.WornApparel;
                foreach (Apparel apparel in apparelList)
                {
                    if (apparel.def.apparel.CoversBodyPart(part)) dmgMult -= apparel.GetStatValue(deflectionStat);
                    if (dmgMult <= 0)
                    {
                        dmgMult = 0;
                        break;
                    }
                }
            }
            return dmgAmount * dmgMult;
        }

        /// <summary>
        /// Creates a new DamageInfo from a deflected one. Changes damage type to Blunt and hit part to the outermost parent of the originally hit part.
        /// </summary>
        /// <param name="dinfo">The dinfo that was deflected</param>
        /// <param name="hitPart">The originally hit part</param>
        /// <returns>DamageInfo copied from dinfo with Def and forceHitPart adjusted</returns>
        private static DamageInfo GetDeflectDamageInfo(DamageInfo dinfo, BodyPartRecord hitPart)
        {
            DamageInfo newDinfo = new DamageInfo(DamageDefOf.Blunt, dinfo.Amount, dinfo.Angle, dinfo.Instigator, GetOuterMostParent(hitPart), dinfo.Weapon);
            newDinfo.SetBodyRegion(dinfo.Height, dinfo.Depth);
            newDinfo.SetWeaponBodyPartGroup(dinfo.WeaponBodyPartGroup);
            newDinfo.SetWeaponHediff(dinfo.WeaponLinkedHediff);
            newDinfo.SetInstantOldInjury(dinfo.InstantOldInjury);
            newDinfo.SetAllowDamagePropagation(dinfo.AllowDamagePropagation);

            return newDinfo;
        }

        /// <summary>
        /// Retrieves the first parent of a body part with depth Outside
        /// </summary>
        /// <param name="part">The part to get the parent of</param>
        /// <returns>The first parent part with depth Outside, the original part if it already is Outside or doesn't have a parent, the root part if no parents are Outside</returns>
        private static BodyPartRecord GetOuterMostParent(BodyPartRecord part)
        {
            BodyPartRecord curPart = part;
            while(curPart.parent != null && curPart.depth != BodyPartDepth.Outside)
            {
                curPart = curPart.parent;
            }
            return curPart;
        }

        /// <summary>
        /// Determines whether a dinfo is of an ambient (i.e. heat, electric) damage type and should apply percentage reduction, as opposed to deflection-based reduction
        /// </summary>
        /// <param name="dinfo"></param>
        /// <returns>True if dinfo armor category is Heat or Electric, false otherwise</returns>
        private static bool IsAmbientDamage(this DamageInfo dinfo)
        {
            return (dinfo.Def.GetModExtension<DamageDefExtensionCE>() ?? new DamageDefExtensionCE()).isAmbientDamage;
        }

        /// <summary>
        /// Applies damage to a parry object based on its armor values. For ambient damage, percentage reduction is applied, direct damage uses deflection formulas.
        /// </summary>
        /// <param name="dinfo">DamageInfo to apply to parryThing</param>
        /// <param name="parryThing">Thing taking the damage</param>
        public static void ApplyParryDamage(DamageInfo dinfo, Thing parryThing)
        {
            Pawn pawn = parryThing as Pawn;
            if (pawn != null)
            {
                // Pawns run their own armor calculations
                dinfo.SetAmount(Mathf.CeilToInt(dinfo.Amount * Rand.Range(0f, 0.5f)));
                pawn.TakeDamage(dinfo);
            }
            else if (dinfo.IsAmbientDamage())
            {
                int dmgAmount = Mathf.CeilToInt(dinfo.Amount * Mathf.Clamp01(parryThing.GetStatValue(dinfo.Def.armorCategory.deflectionStat)));
                dinfo.SetAmount(dmgAmount);
                parryThing.TakeDamage(dinfo);
            }
            else
            {
                float dmgAmount = dinfo.Amount * 0.1f;
                float penAmount = GetPenetrationValue(dinfo);
                TryPenetrateArmor(dinfo.Def, parryThing.GetStatValue(dinfo.Def.armorCategory.deflectionStat), ref penAmount, ref dmgAmount, parryThing);
            }
        }

        #endregion
    }
}
