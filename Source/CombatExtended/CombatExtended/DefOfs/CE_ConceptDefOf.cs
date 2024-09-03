using RimWorld;

namespace CombatExtended
{
    [DefOf]
    public class CE_ConceptDefOf
    {
        static CE_ConceptDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CE_ConceptDefOf));
        }
        // *** Modal dialog concepts ***
        public static ConceptDef CE_AmmoSettings;

        // *** Self-activated concepts ***
        public static ConceptDef CE_InventoryWeightBulk;
        public static ConceptDef CE_Loadouts;
        public static ConceptDef CE_ObtainingFSX;
        public static ConceptDef CE_ObtainingPrometheum;

        // *** Opportunistic concepts ***
        public static ConceptDef CE_ReusableNeolithicProjectiles;
        public static ConceptDef CE_AimingSystem;
        public static ConceptDef CE_FireModes;
        public static ConceptDef CE_AimModes;
        public static ConceptDef CE_Spotting;
        public static ConceptDef CE_MortarDirectFire;
        public static ConceptDef CE_Stabilizing;
        public static ConceptDef CE_ArmorSystem;
        public static ConceptDef CE_SuppressionReaction;
        public static ConceptDef CE_Hunkering;
        public static ConceptDef CE_WornBulk;
        public static ConceptDef CE_Crouching;
    }
}
