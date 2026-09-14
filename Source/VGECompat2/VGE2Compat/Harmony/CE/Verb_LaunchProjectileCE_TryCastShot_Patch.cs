using HarmonyLib;
using VanillaGravshipExpanded2;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Harmony/Verb_LaunchProjectile_TryCastShot_Patch.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion


namespace CombatExtended.Compatibility.VGECompat;

// reproduce the patch from VGE2
[HarmonyPatch(typeof(Verb_LaunchProjectileCE), "TryCastShot")]
public class Verb_LaunchProjectileCE_TryCastShot_Patch
{
    public static void Postfix(Verb_LaunchProjectileCE __instance, bool __result)
    {
        if (__result && __instance.EquipmentSource is Building_GravshipTurretCE turret)
        {
            turret.TryAddVisibility();
        }
    }
}
