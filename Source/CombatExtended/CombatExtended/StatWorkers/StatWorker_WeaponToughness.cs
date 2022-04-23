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
    public class StatWorker_WeaponToughness : StatWorker
    {
        public const float parryChanceFactor = 5f; // Factor by which the weapon holder's skill affects the final weapon toughness

        public float GetHolderToughnessFactor(Pawn pawn)
        {
	    var apparel = pawn.apparel;
	    pawn.apparel = null;
	    var equipment = pawn.equipment;
	    pawn.equipment = null;
	    try {
		float factor = pawn.getStatValue(CE_StatDefOf.MeleeParryChance);
	    }
	    finally {
		pawn.apparel = apparel;
		pawn.equipment = equipment;
	    }
	    return factor;
        }

        public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess)
        {
            var thing = req.Thing;
            if (thing != null)
            {
                // Since the material factor acts like a StatPart, if the holder's skill was taken into account first, it wouldn't affect stuffable weapons
                if (stat.parts != null)
                {
                    foreach (StatPart part in this.stat.parts)
                    {
                        part.TransformValue(req, ref val);
                    }
                }

                // Additional stuff multiplier
                if (thing.Stuff != null)
                {
                    val *= thing.Stuff.GetModExtension<StuffToughnessMultiplierExtensionCE>()?.toughnessMultiplier ?? 1f;
                }

                // Factors in the holder's skill
                var compEq = thing.TryGetComp<CompEquippable>();
                var holder = compEq?.Holder;
                if (holder?.equipment?.Primary == thing)
                {
                    val *= GetHolderToughnessFactor(holder);
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

            if (req.Thing.Stuff != null)
            {
                result += "\n" + "CE_StatsReport_WeaponToughness_StuffEffect".Translate() + (req.Thing.Stuff.GetModExtension<StuffToughnessMultiplierExtensionCE>()?.toughnessMultiplier ?? 1f).ToStringPercent();
            }

            if (req.Thing != null)
            {
                if (req.Thing?.TryGetComp<CompEquippable>()?.Holder != null)
                {
                    result += "\n" + "CE_StatsReport_WeaponToughness_HolderEffect".Translate() + GetHolderToughnessFactor(req.Thing.TryGetComp<CompEquippable>().Holder).ToStringPercent();
                }
            }

            result += "\n" + "StatsReport_FinalValue".Translate() + ": " + stat.ValueToString(finalVal, stat.toStringNumberSense);

            return result;
        }

        public override bool ShouldShowFor(StatRequest req)
        {
            return Controller.settings.ShowExtraStats && req.HasThing && req.Thing.def.IsWeapon && base.ShouldShowFor(req);
        }
    }
}
