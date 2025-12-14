using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE.Compatibility;

public static class Harmony_AlphaMechs
{
    private static Type CompSwapWeapons_Patch_HarmonyPatches
    {
        get
        {
            return AccessTools.TypeByName("AlphaMechs.CompSwapWeapons");
        }
    }
    [HarmonyPatch]
    public static class Harmony_CompSwapWeapons_Apply
    {
        public static bool Prepare()
        {
            return CompSwapWeapons_Patch_HarmonyPatches != null;
        }

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("AlphaMechs.CompSwapWeapons:Apply", new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) });
        }

        public static void Prefix(CompAbilityEffect __instance)
        {
            __instance?.parent?.pawn?.equipment.Primary?.TryGetComp<CompAmmoUser>()?.TryUnload();
        }
    }

}
