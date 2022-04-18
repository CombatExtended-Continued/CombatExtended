using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class StatPatches
    {
        static StatPatches()
        {
            StatDefOf.ArmorRating_Sharp.parts.Add(new StatPart_MechDurability());

            StatDefOf.ArmorRating_Blunt.parts.Add(new StatPart_MechDurability());

            StatDefOf.ArmorRating_Heat.parts.Add(new StatPart_MechDurability());
        }
    }

    public class StatPart_MechDurability : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {

            var comp = (req.Thing?.TryGetComp<CompMechArmorDurability>() ?? null);
            if (comp != null)
            {
                var mech = (Pawn)req.Thing;

                val *= comp.curDurabilityPercent;                
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            var comp = (req.Thing?.TryGetComp<CompMechArmorDurability>() ?? null);
            if (comp != null)
            {
                var mech = (Pawn)req.Thing;

                return "Armor durability: " + comp.curDurabilityPercent.ToStringPercent();
            }
            return null;
        }
    }
}
