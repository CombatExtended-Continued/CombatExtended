using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld.Planet;

namespace CombatExtended.Compatibility
{
    [StaticConstructorOnStartup]
    public class PawnKindPatcher
    {

        static PawnKindPatcher()
        {

            List<PawnKindDef> stuff = DefDatabase<PawnKindDef>.AllDefsListForReading.FindAll(i => {
                var hasModExtensions = i.modExtensions?.Any(tt => tt is LoadoutPropertiesExtension) ?? false;
                var isHuman = !i.RaceProps.Animal;
                var hasCompInv = i.race?.comps?.Any(t => t is CompProperties_Inventory) ?? false;

                return !hasModExtensions && isHuman && hasCompInv;
            });
            foreach (PawnKindDef thin in stuff)
            {
                if (thin.modExtensions == null)
                {
                    thin.modExtensions = new List<DefModExtension>();
                }

                thin.modExtensions.Add(new LoadoutPropertiesExtension { primaryMagazineCount = new FloatRange { min = 2, max = 5 } });
                

            }
        }
    }
   
}
