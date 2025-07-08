using RimWorld;
using Verse;

namespace CombatExtended;
[DefOf]
public static class CE_ThingDefOf
{
    static CE_ThingDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(CE_ThingDefOf));
    }
    //Motes
    public static ThingDef Mote_BigExplode;
    public static ThingDef Mote_FlareSmoke;
    public static ThingDef Mote_FlareGlow;
    public static ThingDef Mote_SuppressIcon;
    public static ThingDef Mote_HunkerIcon;
    public static ThingDef Mote_Firetrail;
    //Filth
    public static ThingDef Filth_RifleAmmoCasings;
    public static ThingDef FilthPrometheum;
    //Equipment
    public static ThingDef Mech_CentipedeBlaster;
    public static ThingDef Gun_BinocularsRadio;
    //Buildings
    public static ThingDef AmmoBench;
    public static ThingDef GunsmithingBench;
    [MayRequireRoyalty]
    public static ThingDef CombatExtended_MechAmmoBeacon;
    //Apparel
    public static ThingDef CE_Apparel_RadioPack;

    public static ThingDef FSX;
    public static ThingDef Flare;
    public static ThingDef ExplosionCE;
    public static ThingDef Gas_BlackSmoke;
    public static ThingDef Fragment_Large;
    public static ThingDef Fragment_Small;
}
