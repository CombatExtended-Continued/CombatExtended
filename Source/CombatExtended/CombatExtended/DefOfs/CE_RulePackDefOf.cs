using RimWorld;
using Verse;

namespace CombatExtended;
[DefOf]
public static class CE_RulePackDefOf
{
    static CE_RulePackDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(CE_RulePackDefOf));
    }
    public static RulePackDef AttackMote;
    public static RulePackDef SuppressedMote;

    public static RulePackDef SmokeCoverMote;
    public static RulePackDef TendingSelfMote;

    public static RulePackDef WeaponPickupRangedMote;
    public static RulePackDef WeaponPickupMeleeMote;

    public static RulePackDef AoeDeployedMote;
    public static RulePackDef FlareDeployedMote;

    public static RulePackDef GasMaskOnMote;
    public static RulePackDef GasMaskOffMote;

    public static RulePackDef FireSel_SuppressingCloseMote;
    public static RulePackDef FireSel_SuppressingFarMote;
    public static RulePackDef FireSel_AimingBadVisibilityMote;
    public static RulePackDef FireSel_AutoCloseMote;
    public static RulePackDef FireSel_AutoFarMote;
    public static RulePackDef FireSel_VeryLowAmmoMote;
    public static RulePackDef FireSel_LowAmmoMote;

    public static RulePackDef DamageEvent_ShellingExplosion;
    public static RulePackDef DamageEvent_CookOff;
    public static RulePackDef DamageEvent_Shelling;
}
