using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public static class RightTools
    {
        public static void EquipRightTool(Pawn pawn, StatDef def)
        {
            float num = -3.40282347E+38f;
            ThingWithComps thingWithComps = null;
            CompInventory compInventory = pawn.TryGetComp<CompInventory>();
            using (IEnumerator<Thing> enumerator = compInventory.container.AsEnumerable<Thing>().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    ThingWithComps thingWithComps2 = (ThingWithComps)enumerator.Current;
                    if (thingWithComps2.def.equippedStatOffsets != null)
                    {
                        foreach (StatModifier current in thingWithComps2.def.equippedStatOffsets)
                        {
                            if (current.stat == def)
                            {
                                if (current.value > num)
                                {
                                    num = current.value;
                                    thingWithComps = thingWithComps2;
                                }
                            }
                        }
                    }
                }
            }
            if (thingWithComps != null)
            {
                compInventory.TrySwitchToWeapon(thingWithComps);
            }
        }
    }
}
