using System.Text;
using RimWorld;
using Verse;

namespace CombatExtended;

public class StatPart_ExtendedMagazine : StatPart
{
    private int? magazineSizeIncrease;

    public override string ExplanationPart(StatRequest req)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Magazine Size Increase: " + (magazineSizeIncrease ?? 0));
        return sb.ToString();
    }

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (magazineSizeIncrease == null)
        {
            if (req.Thing is ThingWithComps thingWithComps && thingWithComps.TryGetComp<CompUniqueWeapon>(out var comp))
            {
                foreach (WeaponTraitDef trait in comp.TraitsListForReading)
                {
                    if (trait is CustomWeaponTraitDef custom)
                    {
                        Log.Message("Test");
                        magazineSizeIncrease = custom.magazineCapacityIncrease;
                    }
                }
            }
        }
        val += magazineSizeIncrease ?? 0;
    }

}
