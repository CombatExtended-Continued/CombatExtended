using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public class ThingSetMaker_CountEnabledAmmoOnly : ThingSetMaker_Count
    {
        public override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
        {
            parms.validator = delegate (ThingDef d)
            {
                if (d is AmmoDef ammodef && (ammodef.tradeTags?.Contains(AmmoInjector.destroyWithAmmoDisabledTag) ?? false))
                {
                    return AmmoUtility.IsAmmoSystemActive(ammodef);
                }
                return true;
            };
            base.Generate(parms, outThings);
        }
    }
}
