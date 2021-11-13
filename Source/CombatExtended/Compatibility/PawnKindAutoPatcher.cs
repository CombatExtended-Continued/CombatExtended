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
           
            List<PawnKindDef> stuff = DefDatabase<PawnKindDef>.AllDefsListForReading.FindAll(i => i.modExtensions?.Any(tt => !(tt is LoadoutPropertiesExtension)) ?? true && i.RaceProps.Animal == false && i.race.comps.Any(t => t is CompProperties_Inventory));
            foreach (PawnKindDef thin in stuff)
            {


                thin.modExtensions = new List<DefModExtension>();
                thin.modExtensions.Add(new LoadoutPropertiesExtension { primaryMagazineCount = new FloatRange { min = 2, max = 5 } });
                

            }
        }
    }
   
}
