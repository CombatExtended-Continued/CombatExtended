using System.Text;
using RimWorld;
using Verse;

namespace CombatExtended;

public class StatPart_ExtendedMagazine : StatPart
{
    public override string ExplanationPart(StatRequest req)
    {
        StringBuilder sb = new StringBuilder();
        float value = 0;
        TransformValue(req, ref value);
        sb.AppendLine("Magazine Size Increase: " + value);
        return sb.ToString();
    }

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (req.Thing is ThingWithComps thingWithComps && thingWithComps.TryGetComp<CompUniqueWeapon>(out var comp))
        {
            foreach (WeaponTraitDef trait in comp.TraitsListForReading)
            {
                if (trait is CustomWeaponTraitDef custom)
                {
                    val += custom.magazineCapacityIncrease;
                }
            }
        }
    }

}
