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
                dinfo.SetAmount(Mathf.CeilToInt(GetAmbientPostArmorDamage(dmgAmount, originalDinfo.Def.armorCategory.armorRatingStat, pawn, hitPart)));
                return dinfo;
            }

            float penAmount = originalDinfo.ArmorPenetrationInt; //GetPenetrationValue(originalDinfo);

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
                    if (blockedByShield && !TryPenetrateArmor(dinfo.Def, shield.GetStatValue(dinfo.Def.armorCategory.armorRatingStat), ref penAmount, ref dmgAmount, shield))
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
                                var pen = originalDinfo.ArmorPenetrationInt; //GetPenetrationValue(originalDinfo);
                                var dmg = (float)secDinfo.Amount;
                                TryPenetrateArmor(secDinfo.Def, shield.GetStatValue(secDinfo.Def.armorCategory.armorRatingStat), ref pen, ref dmg, shield);
                            }
                        }

                        return dinfo;
                    }
                }

                // Apparel is arranged in draw order, we run through reverse to go from Shell -> OnSkin
                for (int i = apparel.Count - 1; i >= 0; i--)
                {
                    if (apparel[i].def.apparel.CoversBodyPart(hitPart) 
                        && !TryPenetrateArmor(dinfo.Def, apparel[i].GetStatValue(dinfo.Def.armorCategory.armorRatingStat), ref penAmount, ref dmgAmount, apparel[i]))
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
                    partArmor += pawn.GetStatValue(dinfo.Def.armorCategory.armorRatingStat);
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

        private static ToolCE DistinguishBodyPartGroups(this IEnumerable<ToolCE> tools, DamageInfo dinfo)
        {
        	var potentialTools = tools
                .Where(x => DefDatabase<ManeuverDef>.AllDefs
                    .Any(y => x.capacities.Contains(y.requiredCapacity) && y.verb.meleeDamageDef.defName == dinfo.Def.defName.Replace("_Critical","")));
        	
        	if (!potentialTools.Any())
        		Log.ErrorOnce("CE :: Distinguishing tools based on damageDef failed - at some point the damageinfo changed damageDef (which is as expected). This is an issue because "+dinfo.Weapon+" has multiple ToolCE with the same linkedBodyPartsGroup (or multiple without one), and they can not be distinguished because of it. [While evaluating DamageInfo "+dinfo.ToString()+"]", dinfo.Weapon.GetHashCode() + dinfo.WeaponBodyPartGroup.GetHashCode() + 84827378);

            if (potentialTools.Count() > 1)
                Log.ErrorOnce("CE :: Distinguishing tools based on damageDef failed. There are multiple ToolCE with the same linkedBodyPartsGroup, the same restricted gender and the same maneuver (and thus the same damageDef) [While evaluating DamageInfo " + dinfo.ToString() + "]", dinfo.Weapon.GetHashCode() + dinfo.WeaponBodyPartGroup.GetHashCode() + 1298379123);

        	return potentialTools.FirstOrDefault();
        }
        
        private static ToolCE GetUsedTool(this IEnumerable<ToolCE> tools, DamageInfo dinfo)
        {
            Pawn instigatorPawn = dinfo.Instigator as Pawn;

            if (tools.Count() == 1)
        	{
        		if (dinfo.WeaponBodyPartGroup != null && tools.First().linkedBodyPartsGroup != dinfo.WeaponBodyPartGroup)
        		{
        			Log.ErrorOnce("CE :: For "+dinfo.Weapon+", WeaponBodyPartGroup was specified for DamageInfo "+dinfo.ToString()+", but none of the tools "+String.Join(",", tools.Select(t => t.ToString()).ToArray())+" have this linkedBodyPartsGroup.", dinfo.GetHashCode() + 3473534);
        		}
        	}
        	else if (!tools.Any())
        	{
        		Log.Warning("No ToolCE could be found for "+dinfo.ToString()+", but GetUsedTool was called.");
        	}
        	else
        	{
            	if (dinfo.WeaponBodyPartGroup != null)
            	{
            		var linkedTools = tools.Where(t => t.linkedBodyPartsGroup == dinfo.WeaponBodyPartGroup && (instigatorPawn == null || (t.restrictedGender == Gender.None || t.restrictedGender == instigatorPawn.gender)));
            		
            		if (linkedTools.Count() > 1)
            		{
            			Log.ErrorOnce("CE :: "+dinfo.Weapon+" has multiple ToolCE with linkedBodyPartsGroup="+dinfo.WeaponBodyPartGroup+", and they can not be fully distinguished because of it. [While evaluating DamageInfo "+dinfo.ToString()+"]", dinfo.Weapon.GetHashCode() + dinfo.WeaponBodyPartGroup.GetHashCode() + 84827378);
            			return linkedTools.DistinguishBodyPartGroups(dinfo);
            		}
            		
            		if (linkedTools.Any())
            			return linkedTools.First();
            	}
            	else
            	{
	            	var nonLinkedTools = tools.Where(t => t.linkedBodyPartsGroup == null && (instigatorPawn == null || (t.restrictedGender == Gender.None || t.restrictedGender == instigatorPawn.gender)));
	            	
	        		if (nonLinkedTools.Count() > 1)
	        		{
	        			Log.ErrorOnce("CE :: "+dinfo.Weapon+" has multiple ToolCE without linkedBodyPartsGroup, and they can not be fully distinguished because of it. [While evaluating DamageInfo "+dinfo.ToString()+"]", dinfo.Weapon.GetHashCode() + 5481278);
	        			return nonLinkedTools.DistinguishBodyPartGroups(dinfo);
	        		}
	        		
	        		if (nonLinkedTools.Any())
	        			return nonLinkedTools.First();
            	}
        	}
        	return tools.FirstOrDefault();
        }
        
        /// <summary>
        /// Determines the armor penetration value of a given dinfo. Attempts to extract the tool/verb from the damage info.
        /// </summary>
        /// <param name="dinfo">DamageInfo to determine penetration for</param>
        /// <returns>Armor penetration value for attack used, 0 if it can't be determined</returns>
        /*
        private static float GetPenetrationValue(DamageInfo dinfo)
        {
            
            if (dinfo.Def.isExplosive)
            {
                return dinfo.Amount * 0.1f; // Explosions have 10% of their damage as penetration
            }

            if (dinfo.Weapon != null)
            {
                // Case 1: projectile attack (Weapon.projectile indicates that Weapon IS a projectile)
                ProjectilePropertiesCE projectileProps = dinfo.Weapon.projectile as ProjectilePropertiesCE;
                if (projectileProps != null)
                {
                    return projectileProps.armorPenetration;
                }

                // Case 2: melee attack
                Pawn instigatorPawn = dinfo.Instigator as Pawn;
                if (instigatorPawn != null)
                {
                    // Case 2.1: .. of an equiped melee weapon
                    if (dinfo.Weapon.IsMeleeWeapon)
                    {
                    	ThingWithComps equipment = instigatorPawn.equipment?.Primary;
                    	
                        if (equipment == null || equipment.def != dinfo.Weapon)
                        {
                            Log.Error("CE tried getting armor penetration from melee weapon " + dinfo.Weapon.defName + " but instigator " + dinfo.Instigator.ToString() + " equipment (" + String.Join(",", instigatorPawn.equipment.AllEquipmentListForReading.Select(x => x.LabelCap).ToArray()) + ") does not match.");
                            return 0;
                        }
                        var penetrationMult = equipment.GetStatValue(CE_StatDefOf.MeleePenetrationFactor);
                        var tool = equipment.def.tools?.OfType<ToolCE>().GetUsedTool(dinfo);

                        if (tool != null)
                            return tool.armorPenetration * penetrationMult;
                    }
                    
                    // Case 2.2: .. of a ranged weapon
                    if (dinfo.Weapon.IsRangedWeapon)
                    {
                    	var tool = dinfo.Weapon.tools?.OfType<ToolCE>().GetUsedTool(dinfo);

                        if (tool != null)
                            return tool.armorPenetration;
                    }
                    
                    // Case 2.3: .. of the pawn
                    if (instigatorPawn.def == dinfo.Weapon)
                    {
                        // meleeVerbs: all verbs considered "melee worthy"
                        Verb availableVerb = instigatorPawn.meleeVerbs.TryGetMeleeVerb(dinfo.IntendedTarget);
                    	
	                    // Case 2.3.1: .. of a weaponized hediff (power claw, scyther blade)
                        HediffCompProperties_VerbGiver compProps = dinfo.WeaponLinkedHediff?.CompPropsFor(typeof(HediffComp_VerbGiver)) as HediffCompProperties_VerbGiver;
                        if (compProps != null)
                        {
                        	var tool = compProps.tools?.OfType<ToolCE>().GetUsedTool(dinfo);
                        	
                        	if (tool != null)
                        		return tool.armorPenetration;
                        	
                        	VerbPropertiesCE verbProps = compProps.verbs?.FirstOrDefault(v => v is VerbPropertiesCE) as VerbPropertiesCE;
                        	
                        	var verbs = compProps.verbs;
                        	
                        	if (verbs.Count() > 1)
                            {
                                Log.ErrorOnce("CE :: HediffCompProperties_VerbGiver for "+dinfo.WeaponLinkedHediff+" has multiple VerbPropertiesCE (" + String.Join(",", compProps.verbs.Select(x => x.label).ToArray()) + "). [While evaluating DamageInfo "+dinfo.ToString()+"]", dinfo.WeaponLinkedHediff.GetHashCode() + 128937921);
                        	}
                        	
                        	if (verbProps != null)
                        	{
                            	Log.ErrorOnce("CE :: HediffCompProperties_VerbGiver from DamageInfo "+dinfo.ToString()+ " has VerbPropertiesCE (" + String.Join(",", compProps.verbs.Select(x => x.label).ToArray()) + "), but these are preferably moved to <tools> for B18", dinfo.WeaponLinkedHediff.GetHashCode() + 128937921);
                        		
                            	return verbProps.meleeArmorPenetration;
                        	}
                        }
						
	                	// AllVerbs: bodyparts of the pawn
	                    // Case 2.4: .. of a toolCE/verbPropsCE naturally on the body (hands/fist, head)
	                    if (instigatorPawn.verbTracker != null
	                    && !instigatorPawn.verbTracker.AllVerbs.NullOrEmpty())
	                    {
	                    	var verbs = instigatorPawn.verbTracker.AllVerbs
                                .Where(v => {
	                    	        var toolCE = v.tool as ToolCE;
                                    var propsCE = v.verbProps as VerbPropertiesCE;
                                    // Case 2.4.1: .. of a tool restricted by gender
                                    return v.LinkedBodyPartsGroup == dinfo.WeaponBodyPartGroup
                                         && ((toolCE != null && (toolCE.restrictedGender == Gender.None || toolCE.restrictedGender == instigatorPawn.gender)
                                            || propsCE != null));
	                    	    });
	                        
	                        if (verbs.Count() > 1)
	                        {
                            	Log.ErrorOnce("CE :: Race "+instigatorPawn.def+ " has multiple ToolCE/VerbPropertiesCE (" + String.Join(",", instigatorPawn.verbTracker.AllVerbs.Select(x => x.ToString()).ToArray()) + ") with linkedBodyPartsGroup=" + dinfo.WeaponBodyPartGroup.ToString()+" which can not be distunguished between. Consider using different linkedBodyPartsGroups. [While evaluating DamageInfo "+dinfo.ToString()+"]", instigatorPawn.def.GetHashCode() + 128937921);
	                        }
	                        
	                        if (!verbs.Any())
	                        {
                                Log.ErrorOnce("CE :: Pawn " + instigatorPawn.ToString() + " for BodyPartGroup " + dinfo.WeaponBodyPartGroup.ToString() + " could not find matching ToolCE/Verb_MeleeAttackCE (in AllVerbs: " + String.Join(",", instigatorPawn.verbTracker.AllVerbs.Select(x => x.ToString()).ToArray()) + ") [While evaluating DamageInfo " + dinfo.ToString() + "]", instigatorPawn.def.GetHashCode() + 128937921);
                                return 0;
	                        }

                            var firstVerb = verbs.First();

                            if (firstVerb.tool is ToolCE)
                                return (firstVerb.tool as ToolCE).armorPenetration;

                            if (firstVerb.verbProps is VerbPropertiesCE)
                                return (firstVerb.verbProps as VerbPropertiesCE).meleeArmorPenetration;
	                    }
                    }
                }
            }
#if DEBUG
            Log.Warning("CE could not determine armor penetration, defaulting");
#endif
            return 9999;    // Really high default value so vanilla damage sources such as GiveInjuriesToKill always penetrate

    
        }*/

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
        /// <param name="armorRatingStat">The armor stat to use for damage reduction</param>
        /// <param name="pawn">The damaged pawn</param>
        /// <param name="part">The body part affected</param>
        /// <returns>The post-armor damage ranging from 0 to the original amount</returns>
        private static float GetAmbientPostArmorDamage(float dmgAmount, StatDef armorRatingStat, Pawn pawn, BodyPartRecord part)
        {
            float dmgMult = 1 - pawn.GetStatValue(armorRatingStat);
            if (dmgMult <= 0) return 0;
            if (pawn.apparel != null && !pawn.apparel.WornApparel.NullOrEmpty())
            {
                List<Apparel> apparelList = pawn.apparel.WornApparel;
                foreach (Apparel apparel in apparelList)
                {
                    if (apparel.def.apparel.CoversBodyPart(part)) dmgMult -= apparel.GetStatValue(armorRatingStat);
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
            DamageInfo newDinfo = new DamageInfo(DamageDefOf.Blunt, dinfo.Amount, 0, //Armor Penetration
                dinfo.Angle, dinfo.Instigator, GetOuterMostParent(hitPart), dinfo.Weapon);
            newDinfo.SetBodyRegion(dinfo.Height, dinfo.Depth);
            newDinfo.SetWeaponBodyPartGroup(dinfo.WeaponBodyPartGroup);
            newDinfo.SetWeaponHediff(dinfo.WeaponLinkedHediff);
            newDinfo.SetInstantPermanentInjury(dinfo.InstantPermanentInjury);
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
                int dmgAmount = Mathf.CeilToInt(dinfo.Amount * Mathf.Clamp01(parryThing.GetStatValue(dinfo.Def.armorCategory.armorRatingStat)));
                dinfo.SetAmount(dmgAmount);
                parryThing.TakeDamage(dinfo);
            }
            else
            {
                float dmgAmount = dinfo.Amount * 0.1f;
                float penAmount = dinfo.ArmorPenetrationInt; //GetPenetrationValue(dinfo);
                TryPenetrateArmor(dinfo.Def, parryThing.GetStatValue(dinfo.Def.armorCategory.armorRatingStat), ref penAmount, ref dmgAmount, parryThing);
            }
        }

        #endregion
    }
}
