﻿using System;
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
                        if (!def.IsRangedWeapon && (!def.tools?.Any(tool => tool.capacities?.Any(capacityDef => DefDatabase<DamageDef>.defsList.Any(damageDef => damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp && capacityDef.defName == damageDef.defName)) ?? false) ?? false))
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
                            // C# magic
                            var largestIngredientCount = DefDatabase<RecipeDef>.AllDefs.ToList().Find(recipeDef => (bool)recipeDef.products?.Any(product => product.thingDef.defName == def.defName))?.ingredients?.MaxBy(ingredientCount => ingredientCount.count);
                            var largestIngredient = (largestIngredientCount?.IsFixedIngredient ?? false) ? largestIngredientCount.FixedIngredient : largestIngredientCount?.filter?.thingDefs?.MaxBy(thingDef => thingDef.statBases?.Find(statMod => statMod.stat.defName == StatDefOf.ArmorRating_Sharp.GetStatPart<StatPart_Stuff>().stuffPowerStat.defName)?.value);
                            float? largestIngredientSharpArmor = largestIngredient?.statBases?.Find(statMod => statMod.stat.defName == StatDefOf.ArmorRating_Sharp.GetStatPart<StatPart_Stuff>().stuffPowerStat.defName)?.value * (largestIngredient?.GetModExtension<StuffToughnessMultiplierExtensionCE>()?.toughnessMultiplier ?? 1f);

                            // For weapons that do not have a recipe
                            if (largestIngredientSharpArmor == null)
                            {
                                // Anything above spacer tech is assumed to be made out of plasteel at least
                                if (def.techLevel > TechLevel.Industrial)
                                {
                                    largestIngredientSharpArmor = 2f;
                                }
                                else
                                {
                                    largestIngredientSharpArmor = 1f;
                                }
                            }

                            def.statBases.Add(new StatModifier { stat = CE_StatDefOf.ToughnessRating, value = weaponThickness * (float)largestIngredientSharpArmor });
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
