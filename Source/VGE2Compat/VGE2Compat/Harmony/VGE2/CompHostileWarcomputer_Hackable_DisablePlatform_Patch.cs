using CombatExtended.Compatibility.VGECompat;
using HarmonyLib;
using System.Linq;
using VanillaGravshipExpanded2;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Comps/CompHostileWarcomputer_Hackable.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGE2Compat;

[HarmonyPatch(typeof(CompHostileWarcomputer_Hackable), nameof(CompHostileWarcomputer_Hackable.DisablePlatform))]
public class CompHostileWarcomputer_Hackable_DisablePlatform_Patch
{
    public static bool Prefix(Map map)
    {
        foreach (var thing in map.listerBuildings.allBuildingsNonColonist.ToList())
        {
            if (thing is Building_GravshipTurretCE turret)
            {
                turret.DisablePermanently();
            }
        }
        return false;
    }
}
