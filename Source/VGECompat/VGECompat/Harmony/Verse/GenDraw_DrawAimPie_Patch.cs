using HarmonyLib;
using VanillaGravshipExpanded;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/HarmonyPatches/GenDraw_DrawAimPie_Patch.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat;

[HotSwappable]
[HarmonyPatch(typeof(GenDraw), nameof(GenDraw.DrawAimPie))]
public static class GenDraw_DrawAimPie_Patch
{
    public static void Prefix(Thing shooter, ref float offsetDist)
    {
        if (shooter is Building_GravshipTurretCE)
        {
            offsetDist += 5f;
        }
    }
}
