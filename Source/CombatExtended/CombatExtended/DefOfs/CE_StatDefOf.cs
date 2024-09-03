using RimWorld;

namespace CombatExtended
{
    [DefOf]
    public static class CE_StatDefOf
    {
        static CE_StatDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CE_StatDefOf));
        }
        // *** Item stats ***
        public static StatDef Bulk; // for items in inventory
        public static StatDef WornBulk; // worn apparel

        // *** Weapon stats ***
        public static StatDef StuffEffectMultiplierToughness;
        public static StatDef ToughnessRating;

        // *** Ranged weapon stats ***
        public static StatDef ShotSpread; // pawn capacity
        public static StatDef SwayFactor; // pawn capacity
        public static StatDef SightsEfficiency;
        public static StatDef AimingAccuracy; // pawn capacity
        public static StatDef ReloadSpeed; // pawn capacity
        public static StatDef MuzzleFlash;
        public static StatDef MagazineCapacity;
        public static StatDef AmmoGenPerMagOverride;
        public static StatDef NightVisionEfficiency_Weapon;
        public static StatDef TicksBetweenBurstShots;
        public static StatDef BurstShotCount;
        public static StatDef Recoil;
        public static StatDef ReloadTime;
        public static StatDef OneHandedness;
        public static StatDef BipodStats;

        // *** Melee weapon stats ***
        public static StatDef MeleePenetrationFactor;
        public static StatDef MeleeCounterParryBonus;

        // *** Pawn stats ***
        public static StatDef CarryBulk;    // Inventory max space
        public static StatDef CarryWeight;  // Inventory max weight
        public static StatDef MeleeCritChance;
        public static StatDef MeleeDodgeChance;
        public static StatDef MeleeParryChance;
        public static StatDef UnarmedDamage;
        public static StatDef BodyPartSharpArmor;
        public static StatDef BodyPartBluntArmor;
        public static StatDef AverageSharpArmor;
        public static StatDef NightVisionEfficiency;

        public static StatDef SmokeSensitivity;

        public static StatDef Suppressability;

        public static StatDef ArmorRating_Electric;
    }
}
