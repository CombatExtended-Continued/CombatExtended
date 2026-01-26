using HarmonyLib;
using Verse;
using VFESecurity;

namespace CombatExtended.Compatibility.VFES
{
    [HarmonyPatch(typeof(Building_TurretGunCE), "Active", MethodType.Getter)]
    public static class Building_TurretGunCE_Active_Patch
    {
        public static bool Prefix(Building_TurretGunCE __instance, ref bool __result)
        {
            CompConcealed comp = __instance.GetComp<CompConcealed>();
            if (comp != null && comp.Submerged)
            {
                Log.Message($"Prevented a submerged turret from firing.");
                __result = false;
                return false;
            }
            return true;
        }
    }
}
