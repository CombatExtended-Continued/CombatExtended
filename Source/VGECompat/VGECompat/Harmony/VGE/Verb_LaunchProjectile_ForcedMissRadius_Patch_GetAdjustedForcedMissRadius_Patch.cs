using HarmonyLib;
using VanillaGravshipExpanded;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/Verb_LaunchProjectile_GetForcedMissTarget_Patch.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat;

// -- Patchception --
// We need to patch the Verb_LaunchProjectile_ForcedMissRadius_Patch.GetAdjustedForcedMissRadius method to correclty draw HighlightFieldRadiusAroundTarget

[HarmonyPatch(nameof(Verb_LaunchProjectile_ForcedMissRadius_Patch.GetAdjustedForcedMissRadius))]
[HarmonyBefore("vanillaexpanded.gravship")] // Use priority to avoid crash
public class Verb_LaunchProjectile_ForcedMissRadius_Patch_GetAdjustedForcedMissRadius_Patch
{
    public static bool Prefix(float baseMiss, Verb verb, ref float __result)
    {
        if (verb != null && verb.caster is Building_GravshipTurretCE turret)
        {
            __result = turret.GetLocalForcedMissRadius(baseMiss);
            return false;
        }
        __result = baseMiss;
        return false;
    }
}
