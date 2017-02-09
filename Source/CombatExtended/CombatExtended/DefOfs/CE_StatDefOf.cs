using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace CombatExtended
{
    [DefOf]
    public static class CE_StatDefOf
    {
        // *** Inventory stats ***
        public static readonly StatDef Bulk = StatDef.Named("Bulk"); // for items in inventory
      //  public static readonly StatDef Mass = StatDef.Named("Mass"); // items in inventory
        public static readonly StatDef WornBulk = StatDef.Named("WornBulk"); // apparel offsets
        public static readonly StatDef WornWeight = StatDef.Named("WornWeight"); // apparel offsets
        public static readonly StatDef CarryBulk = StatDef.Named("CarryBulk"); // pawn capacity
        public static readonly StatDef CarryWeight = StatDef.Named("CarryWeight"); // pawn capacity

        // *** Ranged weapon stats ***
        public static readonly StatDef ShotSpread = StatDef.Named("ShotSpread"); // pawn capacity
        public static readonly StatDef SwayFactor = StatDef.Named("SwayFactor"); // pawn capacity
        public static readonly StatDef AimEfficiency = StatDef.Named("AimEfficiency"); // pawn capacity
        public static readonly StatDef AimingAccuracy = StatDef.Named("AimingAccuracy"); // pawn capacity
        public static readonly StatDef ReloadSpeed = StatDef.Named("ReloadSpeed"); // pawn capacity

        // *** Melee weapon stats ***
        public static readonly StatDef ArmorPenetration = StatDef.Named("ArmorPenetration");
    }
}
