using CombatExtended;
using HarmonyLib;
using Verse;

namespace AOC17CE
{
    [HarmonyPatch(typeof(CompAmmoUser), nameof(CompAmmoUser.PostExposeData))]
    public static class KillaKillAmmoMigration
    {
        public static void Postfix(CompAmmoUser __instance,
            ref AmmoDef ___currentAmmoInt, ref AmmoDef ___selectedAmmo)
        {
            if (Scribe.mode != LoadSaveMode.PostLoadInit ||
                __instance.parent?.def?.defName != "AOC_KILLAKILL" ||
                __instance.Props.ammoSet?.defName != "AmmoSet_AOC17KillaKill" ||
                __instance.Props.ammoSet.ammoTypes == null ||
                __instance.Props.ammoSet.ammoTypes.Count != 1) return;

            // Preserve the loaded count when migrating the former 25x59mm set.
            // Loose inventory ammunition remains unchanged and usable elsewhere.
            AmmoDef ammo = __instance.Props.ammoSet.ammoTypes[0].ammo;
            if (ammo == null) return;
            ___currentAmmoInt = ammo;
            ___selectedAmmo = ammo;
        }
    }
}
