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
        private const float PenetrationRandVariation = 0.05f;    // Armor penetration will be randomized by +- this amount
        private const float SoftArmorMinDamageFactor = 0.2f;    // Soft body armor will always take at least original damage * this number from sharp attacks
        private static readonly SimpleCurve dmgMultCurve = new SimpleCurve { new CurvePoint(0.5f, 0), new CurvePoint(1, 0.5f), new CurvePoint(2, 1) };    // Used to calculate the damage reduction from the penetration / armor ratio
        private static readonly StuffCategoryDef[] softStuffs = { StuffCategoryDefOf.Fabric, DefDatabase<StuffCategoryDef>.GetNamed("Leathery") };

        /// <summary>
        /// Calculates damage through armor, depending on damage type, target and natural resistance. Also calculates deflection and adjusts damage type and impacted body part accordingly.
        /// </summary>
        /// <param name="origDinfo">The pre-armor damage info</param>
        /// <param name="pawn">The damaged pawn</param>
        /// <param name="hitPart">The pawn's body part that has been hit</param>
        /// <returns>If shot is deflected returns a new dinfo cloned from the original with damage amount, Def and ForceHitPart adjusted for deflection, otherwise a clone with only the damage adjusted</returns>
        public static DamageInfo GetAfterArmorDamage(DamageInfo origDinfo, Pawn pawn, BodyPartRecord hitPart)
        {
            if (origDinfo.Def.armorCategory == DamageArmorCategory.IgnoreArmor) return origDinfo;

            DamageInfo dinfo = new DamageInfo(origDinfo);
            float dmgAmount = dinfo.Amount;
            bool involveArmor = dinfo.Def.harmAllLayersUntilOutside;
            bool isAmbientDamage = dinfo.Def.armorCategory == DamageArmorCategory.Electric || dinfo.Def.armorCategory == DamageArmorCategory.Heat;

            // In case of ambient damage (fire, electricity) we apply a percentage reduction formula based on the sum of all applicable armor
            if (isAmbientDamage)
            {
                dinfo.SetAmount(Mathf.CeilToInt(GetAmbientPostArmorDamage(dmgAmount, origDinfo.Def.armorCategory.DeflectionStat(), pawn, hitPart)));
                return dinfo;
            }

            float penAmount = GetPenetrationValue(origDinfo);

            // Apply worn armor
            if (involveArmor && pawn.apparel != null && !pawn.apparel.WornApparel.NullOrEmpty())
            {
                // Apparel is arranged in draw order, we run through reverse to go from Shell -> OnSkin
                List<Apparel> apparel = pawn.apparel.WornApparel;
                for (int i = apparel.Count - 1; i >= 0; i--)
                {
                    if (apparel[i].def.apparel.CoversBodyPart(hitPart) 
                        && !TryPenetrateArmor(dinfo.Def, apparel[i].GetStatValue(dinfo.Def.armorCategory.DeflectionStat()), ref penAmount, ref dmgAmount, apparel[i]))
                    {
                        // Hit was deflected, convert damage type
                        dinfo = GetDeflectDamageInfo(dinfo, hitPart);
                        i++;    // We apply this piece of apparel twice on conversion, this means we can't use deflection on Blunt or else we get an infinite loop of eternal deflection
                        TutorUtility.DoModalDialogIfNotKnown(CE_ConceptDefOf.CE_ArmorSystem);   // Inform the player about armor deflection
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
                if (coveredByArmor) partArmor += pawn.GetStatValue(dinfo.Def.armorCategory.DeflectionStat());  // Get natural armor
                if (!TryPenetrateArmor(dinfo.Def, partArmor, ref penAmount, ref dmgAmount))
                {
                    dinfo.SetForcedHitPart(curPart);
                    if (!coveredByArmor || pawn.RaceProps.IsFlesh)
                    {
                        break;  // On body part deflection only convert damage if the hit part is covered by Mechanoid armor
                    }
                    dinfo = GetDeflectDamageInfo(dinfo, curPart);
                    i++;
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

            if (dinfo.WeaponGear != null)
            {
                // Case 1: projectile attack
                ProjectilePropertiesCE projectileProps = dinfo.WeaponGear.projectile as ProjectilePropertiesCE;
                if (projectileProps != null)
                {
                    return projectileProps.armorPenetration;
                }

                // Case 2: melee attack
                Pawn instigatorPawn = dinfo.Instigator as Pawn;
                if (instigatorPawn != null)
                {
                    // Pawn is using melee weapon
                    if (dinfo.WeaponGear.IsMeleeWeapon)
                    {
                        if (instigatorPawn.equipment == null
                            || instigatorPawn.equipment.Primary == null
                            || instigatorPawn.equipment.Primary.def != dinfo.WeaponGear)
                        {
                            Log.Error("CE tried getting armor penetration from melee weapon " + dinfo.WeaponGear.defName + " but instigator " + dinfo.Instigator.ToString() + " equipment does not match");
                            return 0;
                        }
                        return instigatorPawn.equipment.Primary.GetStatValue(CE_StatDefOf.ArmorPenetration);
                    }

                    // Pawn is using body parts
                    if (instigatorPawn.def == dinfo.WeaponGear)
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
                            VerbPropertiesCE verbProps = verb.verbProps as VerbPropertiesCE;
                            if (verbProps != null) return verbProps.meleeArmorPenetration;
                        }
                    }
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
            bool isSharpDmg = def.armorCategory == DamageArmorCategory.Sharp;
            float rand = UnityEngine.Random.Range(penAmount - PenetrationRandVariation, penAmount + PenetrationRandVariation);
            bool deflected = isSharpDmg && armorAmount > rand;

            // Apply damage reduction
            float dmgMult = 1;
            DamageDef_CE defCE = def as DamageDef_CE;
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
                    if (isSharpDmg) armor.TakeDamage(new DamageInfo(def, Mathf.Max(Mathf.CeilToInt(dmgAmount * SoftArmorMinDamageFactor), Mathf.CeilToInt(dmgAmount - newDmgAmount))));
                }
                else
                {
                    // Hard armor takes damage as reduced by damage resistance and can be impervious to low-penetration attacks
                    armor.TakeDamage(new DamageInfo(def, Mathf.CeilToInt(newDmgAmount)));
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
            DamageInfo newDinfo = new DamageInfo(DamageDefOf.Blunt, dinfo.Amount, dinfo.Angle, dinfo.Instigator, GetOuterMostParent(hitPart), dinfo.WeaponGear);
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
    }
}
