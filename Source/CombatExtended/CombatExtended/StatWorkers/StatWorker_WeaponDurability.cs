using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
    [DefOf]
    public class DefOfDurability : DefOf
    {
        public static StatDef Durability;
    }

    [StaticConstructorOnStartup]
    public class DurabilityPatcher
    {
        static DurabilityPatcher()
        {
            foreach (var def in DefDatabase<ThingDef>.AllDefs.Where(x => x.IsWeapon))
            {
                if (def.statBases == null)
                {
                    def.statBases = new List<StatModifier>();
                }

                def.statBases.Add(new StatModifier { stat = DefOfDurability.Durability, value = 0 });

                if (def.comps == null)
                {
                    def.comps = new List<CompProperties>();
                }

                def.comps.Add(new CompProperties { compClass = typeof(CompStatCacher) });
            }
        }
    }

    public class CompStatCacher : ThingComp
    {
        public float StatDurability = -1f;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref StatDurability, "StatDurability");
            base.PostExposeData();
        }
    }

    public class ToughNessModExt : DefModExtension
    {
        public float Toughness;
    }

    public class StatWorker_WeaponDurability : StatWorker
    {
        public float BaseVal(StatRequest req)
        {
            var parryThing = req.Thing;

            var ingredient = DefDatabase<RecipeDef>.AllDefs.ToList().Find(recipeDef => (bool)recipeDef.products?.Any(product => product.thingDef.defName == parryThing.def.defName))?.ingredients?.MaxBy(ingredientCount => ingredientCount.count);
            float? parryWeaponArmor = parryThing.Stuff?.statBases?.Find(statMod => statMod.stat.defName == StatDefOf.ArmorRating_Sharp.GetStatPart<StatPart_Stuff>().stuffPowerStat.defName)?.value
                ?? ingredient?.FixedIngredient?.statBases?.Find(statMod => statMod.stat.defName == StatDefOf.ArmorRating_Sharp.GetStatPart<StatPart_Stuff>().stuffPowerStat.defName)?.value
                ?? ingredient?.filter.thingDefs.MaxBy(thingDef => thingDef.statBases?.Find(statMod => statMod.stat == this.stat)?.value).statBases?.Find(statMod => statMod.stat.defName == StatDefOf.ArmorRating_Sharp.GetStatPart<StatPart_Stuff>().stuffPowerStat.defName)?.value;
            //float? parryWeaponArmor = DefDatabase<RecipeDef>.defsList.Find(recipeDef => recipeDef.products.Any(product => product.thingDef == parryThing.def));
            //As a fallback, check for the weapon's tech level and get different constant values for them
            if (parryWeaponArmor == null)
            {
                switch (parryThing.def.techLevel)
                {
                    //Steel
                    case (TechLevel.Medieval):
                    case (TechLevel.Industrial):
                        parryWeaponArmor = 1f;
                        break;
                    //Plasteel
                    case (TechLevel.Spacer):
                        parryWeaponArmor = 2f;
                        break;
                    case (TechLevel.Ultra):
                        parryWeaponArmor = 4f;
                        break;
                    case (TechLevel.Archotech):
                        parryWeaponArmor = 8f;
                        break;
                    //Arbitrary hardwood
                    default:
                        parryWeaponArmor = 0.5f;
                        break;
                }
            }
            parryWeaponArmor *= Mathf.Pow(parryThing.GetStatValue(CE_StatDefOf.Bulk), 1f / 3f) * 2f; //Since there's no weapon thickness stat, approximate the values by bulk. This works well enough: longswords get a thickness of 4mm, knives get 2mm
            if (!parryThing.def.tools.Any(tool => tool.capacities.Any(capacityDef => DefDatabase<DamageDef>.defsList.Any(damageDef => damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp && capacityDef.defName == damageDef.defName)))) //Blunt weapons get double thickness because edges are easier to damage
            {
                parryWeaponArmor *= 2f;
            }

            return (float)parryWeaponArmor;
        }

        public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess)
        {
            if (req.Thing != null && req.Thing.TryGetComp<CompStatCacher>() != null)
            {
                val = req.Thing.TryGetComp<CompStatCacher>().StatDurability;
                if (val <= this.stat.defaultBaseValue && !req.Thing.def.HasModExtension<ToughNessModExt>())
                {
                    val = BaseVal(req);

                    var parryThing = req.Thing;

                    var defender = parryThing.TryGetComp<CompEquippable>().Holder;

                    if (defender != null)
                    {
                        val *= defender.GetStatValue(CE_StatDefOf.MeleeParryChance) * 4f;
                    }

                    if (this.stat.parts != null)
                    {
                        foreach (StatPart part in this.stat.parts)
                        {
                            part.TransformValue(req, ref val);
                        }
                    }
                    
                    val = (float)Math.Round(val, 2);

                    req.Thing.TryGetComp<CompStatCacher>().StatDurability = val;
                }
                else if(req.Thing.def.HasModExtension<ToughNessModExt>())
                {
                    val = req.Thing.def.GetModExtension<ToughNessModExt>().Toughness;

                    if (this.stat.parts != null)
                    {
                        foreach (StatPart part in this.stat.parts)
                        {
                            part.TransformValue(req, ref val);
                        }
                    }

                    req.Thing.TryGetComp<CompStatCacher>().StatDurability = val;
                }
            }
        }

        public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
        {
            string result = "";

            if (this.stat.parts != null)
            {
                foreach (var part in this.stat.parts)
                {
                    if (!part.ExplanationPart(req).NullOrEmpty())
                    {
                        result += "\n" + part.ExplanationPart(req);
                    }
                }
            }

            if (req.Thing != null)
            {
                if (req.Thing.Stuff != null)
                {
                    result += "\n" + "CE_StuffEffect".Translate() + req.Thing.Stuff?.statBases?.Find(statMod => statMod.stat.defName == StatDefOf.ArmorRating_Sharp.GetStatPart<StatPart_Stuff>().stuffPowerStat.defName)?.value.ToStringPercent();
                }
                if (req.Thing?.TryGetComp<CompEquippable>()?.Holder != null)
                {
                    result += "\n" + "CE_HolderEffect_MeleeToughness".Translate() + ((req.Thing?.TryGetComp<CompEquippable>()?.Holder.GetStatValue(CE_StatDefOf.MeleeParryChance) ?? 1f) * 4f).ToStringPercent();
                } 
            }

            result += "\n" + "StatsReport_FinalValue".Translate() + finalVal;

            return result;
        }

        public override bool ShouldShowFor(StatRequest req)
        {
            return req.HasThing;
        }
    }
}

