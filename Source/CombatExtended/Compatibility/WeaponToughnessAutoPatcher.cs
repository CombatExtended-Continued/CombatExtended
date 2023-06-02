using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended.Compatibility
{
    [StaticConstructorOnStartup]
    class WeaponToughnessAutoPatcher
    {
        static WeaponToughnessAutoPatcher()
        {
            if (!Controller.settings.EnableWeaponToughnessAutopatcher)
            {
                return;
            }
            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(x => x.IsWeapon && !x.IsApparel))
            {
                try
                {
                    if (!def.statBases.Any(x => x.stat == CE_StatDefOf.StuffEffectMultiplierToughness || x.stat == CE_StatDefOf.ToughnessRating))
                    {
                        // Approximate weapon thickness via the bulk of the weapon. Longswords get about 2.83mm, knives get 1mm, spears get about 3.162mm
                        float weaponThickness = Mathf.Sqrt(def.statBases?.Find(statMod => statMod.stat.defName == CE_StatDefOf.Bulk.defName)?.value ?? 0f);

                        if (weaponThickness == 0f)
                        {
                            continue;
                        }

                        // Tech level improves toughness
                        switch (def.techLevel)
                        {
                            //Plasteel
                            case (TechLevel.Spacer):
                                weaponThickness *= 2f;
                                break;
                            case (TechLevel.Ultra):
                                weaponThickness *= 4f;
                                break;
                            case (TechLevel.Archotech):
                                weaponThickness *= 8f;
                                break;
                        }

                        // Blunt weapons get double thickness because edges are easier to damage. Note that ranged weapons are excluded
                        if (!def.IsRangedWeapon
                                && (!def.tools?
                                    .Any(tool => tool.capacities?
                                        .Any(capacityDef => DefDatabase<DamageDef>.defsList
                                            .Any(damageDef => damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp && capacityDef.defName == damageDef.defName))
                                        ?? false)
                                    ?? false))
                        {
                            weaponThickness *= 2f;
                        }

                        // Stuffable weapons get the multiplier stat
                        if (def.MadeFromStuff)
                        {
                            def.statBases.Add(new StatModifier { stat = CE_StatDefOf.StuffEffectMultiplierToughness, value = weaponThickness });
                        }
                        // Non-stuffable weapons get the rating value
                        else
                        {
                            // Search for a fitting recipe
                            RecipeDef firstRecipeDef = DefDatabase<RecipeDef>.AllDefs
                                .FirstOrDefault(recipeDef => recipeDef.products?
                                        .Any(productDef => productDef.thingDef.defName == def.defName) ?? false);

                            IngredientCount biggestIngredientCount = firstRecipeDef?.ingredients?
                                .MaxBy(ingredientCount => ingredientCount.count);

                            float strongestIngredientSharpArmor = 0f;

                            // Recipe does exist and has a fixed ingredient
                            if (biggestIngredientCount?.IsFixedIngredient ?? false)
                            {
                                strongestIngredientSharpArmor = biggestIngredientCount.FixedIngredient.statBases?
                                    .Find(statMod => statMod.stat.defName == StatDefOf.ArmorRating_Sharp.GetStatPart<StatPart_Stuff>().stuffPowerStat.defName)?
                                    .value * (biggestIngredientCount.FixedIngredient.GetModExtension<StuffToughnessMultiplierExtensionCE>()?.toughnessMultiplier ?? 1f) ?? 0f;
                            }
                            // Recipe may or may not exist
                            else
                            {
                                // Becomes null if the recipe doesn't exist or doesn't have any ingredients
                                float? nullableSharpArmor = biggestIngredientCount?.filter?.thingDefs?
                                    .Max(thingDef => thingDef.statBases?
                                            .Find(statMod => statMod.stat.defName == StatDefOf.ArmorRating_Sharp.GetStatPart<StatPart_Stuff>().stuffPowerStat.defName)?
                                            .value * (thingDef.GetModExtension<StuffToughnessMultiplierExtensionCE>()?.toughnessMultiplier ?? 1f) ?? 0f);

                                // Fallback to tech level; above industrial is assumed to have items made out of plasteel (hardcoded)
                                strongestIngredientSharpArmor = nullableSharpArmor ?? (def.techLevel > TechLevel.Industrial ? 2f : 1f);
                            }

                            def.statBases.Add(new StatModifier { stat = CE_StatDefOf.ToughnessRating, value = weaponThickness * strongestIngredientSharpArmor });
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to patch {def} with error {e}");
                }
            }

        }
    }
}
