using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended.Compatibility;
[StaticConstructorOnStartup]
class WeaponToughnessAutoPatcher
{
    static WeaponToughnessAutoPatcher()
    {
        if (!Controller.settings.EnableWeaponToughnessAutopatcher)
        {
            return;
        }

        StatDef SHARP_ARMOR_STUFF_POWER = StatDefOf.ArmorRating_Sharp.GetStatPart<StatPart_Stuff>().stuffPowerStat;

        foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(x => x.IsWeapon && !x.IsApparel))
        {
            try
            {
                // Check if toughness is already defined
                if (def.statBases?
                        .Any(x => x.stat == CE_StatDefOf.StuffEffectMultiplierToughness
                            || x.stat == CE_StatDefOf.ToughnessRating)
                        ?? true)
                {
                    continue;
                }

                // Approximate weapon thickness with the bulk of the weapon.
                // Longswords get about 2.83mm, knives get 1mm, spears get about 3.162mm
                float weaponThickness = def.statBases
                    .Find(statMod => statMod.stat == CE_StatDefOf.Bulk)?.value
                    ?? 0f;
                if (weaponThickness == 0f)
                {
                    continue;
                }
                weaponThickness = Mathf.Sqrt(weaponThickness);

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

                // Blunt-only weapons get additional weapon thickness. Ranged weapons excluded
                if (!def.IsRangedWeapon
                        && (!def.tools?
                            .Any(tool => tool.VerbsProperties
                                .Any(property => property.meleeDamageDef
                                    .armorCategory == DamageArmorCategoryDefOf.Sharp))
                            ?? false))
                {
                    weaponThickness *= 2f;
                }

                // Stuffable weapons receive the multiplier stat
                if (def.MadeFromStuff)
                {
                    def.statBases.Add(new StatModifier
                    {
                        stat = CE_StatDefOf.StuffEffectMultiplierToughness,
                        value = weaponThickness
                    });
                    continue;
                }

                // Non-stuffable weapons get the rating value
                // Search for a fitting recipe
                RecipeDef firstRecipeDef = DefDatabase<RecipeDef>.AllDefs
                    .FirstOrDefault(recipeDef => recipeDef.products?
                            .Any(productDef => productDef.thingDef == def) ?? false);

                IngredientCount biggestIngredientCount = null;
                if (!firstRecipeDef?.ingredients?.Empty() ?? false)
                {
                    biggestIngredientCount = firstRecipeDef.ingredients
                        .MaxBy(ingredientCount => ingredientCount.count);
                }

                float strongestIngredientSharpArmor = 1f;

                // Recipe does exist and has a fixed ingredient
                if (biggestIngredientCount?.IsFixedIngredient ?? false)
                {
                    strongestIngredientSharpArmor = biggestIngredientCount.FixedIngredient.statBases
                        .Find(statMod => statMod.stat == SHARP_ARMOR_STUFF_POWER)?.value
                        ?? 0f;
                    strongestIngredientSharpArmor *= biggestIngredientCount.FixedIngredient
                        .GetModExtension<StuffToughnessMultiplierExtensionCE>()?.toughnessMultiplier
                        ?? 1f;
                }
                // Recipe may or may not exist
                else
                {
                    strongestIngredientSharpArmor = biggestIngredientCount?.filter?.thingDefs?
                        .Max(thingDef => (thingDef.statBases?.Find(statMod => statMod.stat == SHARP_ARMOR_STUFF_POWER)?.value ?? 0f)
                                * (thingDef.GetModExtension<StuffToughnessMultiplierExtensionCE>()?.toughnessMultiplier ?? 1f))
                        ?? 1f;
                }

                def.statBases.Add(new StatModifier
                {
                    stat = CE_StatDefOf.ToughnessRating,
                    value = weaponThickness * strongestIngredientSharpArmor
                });
            }
            catch (Exception e)
            {
                Log.Error($"[CE] Failed to autopatch toughness for {def} with error {e}");
            }
        }
    }
}
