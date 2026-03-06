using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace CombatExtended;

public class StatPart_NaturalArmorDurability : StatPart
{
    float getMinArmorExplicit(CompArmorDurability comp)
    {
        if (parentStat == StatDefOf.ArmorRating_Sharp) { return comp.durabilityProps.MinArmorValueSharp; }
        if (parentStat == StatDefOf.ArmorRating_Blunt) { return comp.durabilityProps.MinArmorValueBlunt; }
        if (parentStat == StatDefOf.ArmorRating_Heat) { return comp.durabilityProps.MinArmorValueHeat; }
        if (parentStat == CE_StatDefOf.ArmorRating_Electric) { return comp.durabilityProps.MinArmorValueElectric; }
        return -1;
    }

    float getMinArmor(CompArmorDurability comp, float val)
    {
        float f = getMinArmorExplicit(comp);
        return f >= 0 ? f : comp.durabilityProps.MinArmorPct * val;
    }

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (val != 0)
        {
            var comp = (req.Thing?.TryGetComp<CompArmorDurability>() ?? null);
            if (comp != null)
            {
                var mech = (Pawn)req.Thing;

                float minArmor = getMinArmor(comp, val);

                val -= (val - minArmor) * (1 - comp.curDurabilityPercent);

                if (val < minArmor) { val = minArmor; }
            }
        }
    }

    public override string ExplanationPart(StatRequest req)
    {
        var comp = (req.Thing?.TryGetComp<CompArmorDurability>() ?? null);
        if (comp != null)
        {
            var mech = (Pawn)req.Thing;

            string minArmorExp = getMinArmorExplicit(comp) >= 0 ? "Minimal armor value: " + getMinArmorExplicit(comp).ToString() : "Minimal armor percentage: " + comp.durabilityProps.MinArmorPct.ToStringPercent();

            return "Armor durability: " + comp.curDurabilityPercent.ToStringPercent() + "\n" + comp.curDurability.ToString() + "/" + comp.maxDurability.ToString() + "\n" + minArmorExp;
        }
        return null;
    }
}
