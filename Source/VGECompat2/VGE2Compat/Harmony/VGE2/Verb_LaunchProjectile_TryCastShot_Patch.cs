using HarmonyLib;
using VanillaGravshipExpanded2;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/WorldComponent_GravshipController_LandingEnded_Patch.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion


namespace CombatExtended.Compatibility.VGECompat;

// reproduce the patch from VGE2
[HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
public class Verb_LaunchProjectile_TryCastShot_Patch
{
    public static void Postfix(Verb_LaunchProjectile __instance, bool __result)
    {
        if (__result && __instance.EquipmentSource is Building_GravshipTurretCE turret)
        {
            turret.TryAddVisibility();
        }
    }
}
