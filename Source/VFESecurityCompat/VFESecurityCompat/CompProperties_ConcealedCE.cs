using Verse;
#nullable enable

namespace CombatExtended.Compatibility.VFES
{
    // properties class so the comp can actually be attached via XML. the <li>
    // Class in defs has to be a CompProperties, which then points at the
    // ThingComp through compClass. dummyDef names the invisible blocker to
    // spawn while the structure is deployed (turret -> soft, barrier -> wall).
    public class CompProperties_ConcealedCE : CompProperties
    {
        public ThingDef? dummyDef;

        public CompProperties_ConcealedCE()
        {
            compClass = typeof(CompConcealedCE);
        }
    }
}
#nullable restore
