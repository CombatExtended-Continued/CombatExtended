using RimWorld;
using Verse;

namespace CombatExtended;
[DefOf]
public static class CE_JobDefOf
{
    static CE_JobDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(CE_JobDefOf));
    }
    public static JobDef TakeFromOther;
    public static JobDef ReloadWeapon;
    public static JobDef ReloadTurret;
    public static JobDef ReloadAutoLoader;
    public static JobDef HunkerDown;
    public static JobDef RunForCover;
    public static JobDef Stabilize;
    public static JobDef WaitKnockdown;
    public static JobDef EquipFromInventory;
    public static JobDef OpportunisticAttack;
    public static JobDef TendSelf;
    public static JobDef ModifyWeapon;
    public static JobDef GiveAmmo;
    public static JobDef RepairNaturalArmor;
    public static JobDef JobDef_SetUpBipod;
}
