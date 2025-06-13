using System;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE;

[HarmonyPatch(typeof(BackCompatibilityConverter_1_5), nameof(BackCompatibilityConverter_1_5.BackCompatibleDefName))]
public class Harmony_BackCompatibility_1_5
{
    public static void Postfix(BackCompatibilityConverter_1_5 __instance, ref string __result, Type defType, string defName)
    {
        if (defType == typeof(ThingDef))
        {
            switch (defName)
            {
                // Core
                case "Proj_GrenadeConcussion":
                    __result = "CE_Proj_GrenadeConcussion";
                    break;
                case "Weapon_GrenadeConcussion":
                    __result = "CE_Weapon_GrenadeConcussion";
                    break;
                case "Proj_GrenadeFlashbang":
                    __result = "CE_Proj_GrenadeFlashbang";
                    break;
                case "Weapon_GrenadeFlashbang":
                    __result = "CE_Weapon_GrenadeFlashbang";
                    break;
                case "Proj_GrenadeStickBomb":
                    __result = "CE_Proj_GrenadeStickBomb";
                    break;
                case "Weapon_GrenadeStickBomb":
                    __result = "CE_Weapon_GrenadeStickBomb";
                    break;
                case "Proj_GrenadeSmoke":
                    __result = "CE_Proj_GrenadeSmoke";
                    break;
                case "Weapon_GrenadeSmoke":
                    __result = "CE_Weapon_GrenadeSmoke";
                    break;
                case "Proj_GrenadeFirefoam":
                    __result = "CE_Proj_GrenadeFirefoam";
                    break;
                case "Weapon_GrenadeFirefoam":
                    __result = "CE_Weapon_GrenadeFirefoam";
                    break;

                // Armory
                case "Gun_TwelvePounder":
                    __result = "CE_Gun_TwelvePounder";
                    break;
                case "Turret_TwelvePounder":
                    __result = "CE_Turret_TwelvePounder";
                    break;
                case "Blueprint_Turret_TwelvePounder":
                    __result = "Blueprint_CE_Turret_TwelvePounder";
                    break;
                case "Frame_Turret_TwelvePounder":
                    __result = "Frame_CE_Turret_TwelvePounder";
                    break;
                case "Blueprint_Install_Turret_TwelvePounder":
                    __result = "Blueprint_Install_CE_Turret_TwelvePounder";
                    break;

                case "Gun_12PounderBombard":
                    __result = "CE_Gun_12PounderBombard";
                    break;
                case "Turret_12PounderBombard":
                    __result = "CE_Turret_12PounderBombard";
                    break;
                case "Blueprint_Turret_12PounderBombard":
                    __result = "Blueprint_CE_Turret_12PounderBombard";
                    break;
                case "Frame_Turret_12PounderBombard":
                    __result = "Frame_CE_Turret_12PounderBombard";
                    break;
                case "Blueprint_Install_Turret_12PounderBombard":
                    __result = "Blueprint_Install_CE_Turret_12PounderBombard";
                    break;

                case "Gun_GatlingGun":
                    __result = "CE_Gun_GatlingGun";
                    break;
                case "Turret_GatlingGun":
                    __result = "CE_Turret_GatlingGun";
                    break;
                case "Blueprint_Turret_GatlingGun":
                    __result = "Blueprint_CE_Turret_GatlingGun";
                    break;
                case "Frame_Turret_GatlingGun":
                    __result = "Frame_CE_Turret_GatlingGun";
                    break;
                case "Blueprint_Install_Turret_GatlingGun":
                    __result = "Blueprint_Install_CE_Turret_GatlingGun";
                    break;

                case "Gun_M1919Browning":
                    __result = "CE_Gun_M1919Browning";
                    break;
                case "Turret_M1919Browning":
                    __result = "CE_Turret_M1919Browning";
                    break;
                case "Blueprint_Turret_M1919Browning":
                    __result = "Blueprint_CE_Turret_M1919Browning";
                    break;
                case "Frame_Turret_M1919Browning":
                    __result = "Frame_CE_Turret_M1919Browning";
                    break;
                case "Blueprint_Install_Turret_M1919Browning":
                    __result = "Blueprint_Install_CE_Turret_M1919Browning";
                    break;

                case "Gun_M2HB":
                    __result = "CE_Gun_M2HB";
                    break;
                case "Turret_M2HB":
                    __result = "CE_Turret_M2HB";
                    break;
                case "Blueprint_Turret_M2HB":
                    __result = "Blueprint_CE_Turret_M2HB";
                    break;
                case "Frame_Turret_M2HB":
                    __result = "Frame_CE_Turret_M2HB";
                    break;
                case "Blueprint_Install_Turret_M2HB":
                    __result = "Blueprint_Install_CE_Turret_M2HB";
                    break;

                case "Gun_MkNineteenGL":
                    __result = "CE_Gun_MkNineteenGL";
                    break;
                case "Turret_MkNineteenGL":
                    __result = "CE_Turret_MkNineteenGL";
                    break;
                case "Blueprint_Turret_MkNineteenGL":
                    __result = "Blueprint_CE_Turret_MkNineteenGL";
                    break;
                case "Frame_Turret_MkNineteenGL":
                    __result = "Frame_CE_Turret_MkNineteenGL";
                    break;
                case "Blueprint_Install_Turret_MkNineteenGL":
                    __result = "Blueprint_Install_CE_Turret_MkNineteenGL";
                    break;

                case "Gun_OrganGun":
                    __result = "CE_Gun_OrganGun";
                    break;
                case "Turret_OrganGun":
                    __result = "CE_Turret_OrganGun";
                    break;
                case "Blueprint_Turret_OrganGun":
                    __result = "Blueprint_CE_Turret_OrganGun";
                    break;
                case "Frame_Turret_OrganGun":
                    __result = "Frame_CE_Turret_OrganGun";
                    break;
                case "Blueprint_Install_Turret_OrganGun":
                    __result = "Blueprint_Install_CE_Turret_OrganGun";
                    break;

                case "Gun_PKM":
                    __result = "CE_EmplacementGun_PKM";
                    break;
                case "Turret_PKM":
                    __result = "CE_Turret_PKM";
                    break;
                case "Blueprint_Turret_PKM":
                    __result = "Blueprint_CE_Turret_PKM";
                    break;
                case "Frame_Turret_PKM":
                    __result = "Frame_CE_Turret_PKM";
                    break;
                case "Blueprint_Install_Turret_PKM":
                    __result = "Blueprint_Install_CE_Turret_PKM";
                    break;

                case "Gun_PortabletMortar":
                    __result = "CE_Gun_PortabletMortar";
                    break;
                case "Turret_PortabletMortar":
                    __result = "CE_Turret_PortabletMortar";
                    break;
                case "Blueprint_Turret_PortabletMortar":
                    __result = "Blueprint_CE_Turret_PortabletMortar";
                    break;
                case "Frame_Turret_PortabletMortar":
                    __result = "Frame_CE_Turret_PortabletMortar";
                    break;
                case "Blueprint_Install_Turret_PortabletMortar":
                    __result = "Blueprint_Install_CE_Turret_PortabletMortar";
                    break;

                case "Gun_SPGNine":
                    __result = "CE_Gun_SPGNine";
                    break;
                case "Turret_SPGNine":
                    __result = "CE_Turret_SPGNine";
                    break;
                case "Blueprint_Turret_SPGNine":
                    __result = "Blueprint_CE_Turret_SPGNine";
                    break;
                case "Frame_Turret_SPGNine":
                    __result = "Frame_CE_Turret_SPGNine";
                    break;
                case "Blueprint_Install_Turret_SPGNine":
                    __result = "Blueprint_Install_CE_Turret_SPGNine";
                    break;

                case "Gun_ShotgunTurret":
                    __result = "CE_Gun_ShotgunTurret";
                    break;
                case "Turret_ShotgunTurret":
                    __result = "CE_Turret_ShotgunTurret";
                    break;
                case "Blueprint_Turret_ShotgunTurret":
                    __result = "Blueprint_CE_Turret_ShotgunTurret";
                    break;
                case "Frame_Turret_ShotgunTurret":
                    __result = "Frame_CE_Turret_ShotgunTurret";
                    break;
                case "Blueprint_Install_Turret_ShotgunTurret":
                    __result = "Blueprint_Install_CE_Turret_ShotgunTurret";
                    break;

                case "Gun_Vickers":
                    __result = "CE_Gun_Vickers";
                    break;
                case "Turret_Vickers":
                    __result = "CE_Turret_Vickers";
                    break;
                case "Blueprint_Turret_Vickers":
                    __result = "Blueprint_CE_Turret_Vickers";
                    break;
                case "Frame_Turret_Vickers":
                    __result = "Frame_CE_Turret_Vickers";
                    break;
                case "Blueprint_Install_Turret_Vickers":
                    __result = "Blueprint_Install_CE_Turret_Vickers";
                    break;
            }
        }
    }
}
