using System.Text;
using RimWorld;
using Verse;

namespace CombatExtended;

public class StatPart_ExtendedMagazine : StatPart
{
    public override string ExplanationPart(StatRequest req)
    {
        if (!req.HasThing || req.Thing is not ThingWithComps thingWithComps || !thingWithComps.TryGetComp<CompUniqueWeapon>(out var comp))
        {
            return null;
        }

        StringBuilder sb = new();
        foreach (WeaponTraitDef trait in comp.TraitsListForReading)
        {
            if (trait is CustomWeaponTraitDef custom)
            {
                if (custom.magazineCapacityFactor != 1f)
                {
                    sb.AppendLine($"{custom.LabelCap}: x{custom.magazineCapacityFactor}");
                }
                if (custom.magazineCapacityIncrease != 0)
                {
                    sb.AppendLine($"{custom.LabelCap}: +{custom.magazineCapacityIncrease}");
                }
            }
        }
        return sb.Length > 0 ? sb.ToString() : null;
    }

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (req.Thing is ThingWithComps thingWithComps && thingWithComps.TryGetComp<CompUniqueWeapon>(out var comp))
        {
            int offsetValue = 0;
            foreach (WeaponTraitDef trait in comp.TraitsListForReading)
            {
                if (trait is CustomWeaponTraitDef custom)
                {
                    val *= custom.magazineCapacityFactor;
                    offsetValue += custom.magazineCapacityIncrease;
                }
            }
            val += offsetValue;
        }
    }

}
