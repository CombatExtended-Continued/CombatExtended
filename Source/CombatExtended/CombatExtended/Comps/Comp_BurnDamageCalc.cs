using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace CombatExtended
{
    /// <summary>
    /// This is only used to store the bool for one calculation
    /// </summary>
    public class Comp_BurnDamageCalc : ThingComp
    {
        public bool deflectedSharp;

        public ThingDef weapon;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref deflectedSharp, "deflsharp");

            Scribe_Defs.Look(ref weapon, "weapon");
        }
    }

    [StaticConstructorOnStartup]
    public class BurnCompAdder
    {
        static BurnCompAdder()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where(x => x.race?.Humanlike ?? false))
            {
                if (def.comps == null)
                    def.comps = new List<CompProperties>();
                def.comps.Add(new CompProperties { compClass = typeof(Comp_BurnDamageCalc) });
            }
        }
    }
}
