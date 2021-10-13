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
    public class PawnKindPatcher : WorldComponent
    {
        public PawnKindPatcher(World world) : base(world)
        {
        }
        public override void FinalizeInit()
        {
            List<PawnKindDef> stuff = DefDatabase<PawnKindDef>.AllDefs.ToList().FindAll(i => i.modExtensions?.Any(tt => !(tt is LoadoutPropertiesExtension)) ?? true && i.RaceProps.Animal == false && i.RaceProps.IsMechanoid == false);
            foreach (PawnKindDef thin in stuff)
            {


                thin.modExtensions = new List<DefModExtension>();
                thin.modExtensions.Add(new LoadoutPropertiesExtension { primaryMagazineCount = new FloatRange { min = 2, max = 5 } });
                LoadoutPropertiesExtension aadada = thin.modExtensions.Find(tt => tt is LoadoutPropertiesExtension) as LoadoutPropertiesExtension;
           
            }
            base.FinalizeInit();
        }
    }
}
