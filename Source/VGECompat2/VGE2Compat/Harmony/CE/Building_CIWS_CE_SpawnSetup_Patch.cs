using CombatExtended;
using CombatExtended.Compatibility.VGECompat2;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VGE2Compat.VGE2Compat.Harmony.CE;

// Adapt https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Harmony/CompPointDefence_InterceptionRadius_Patch.cs
// I Added a condition on defName to avoid buffing other CIWS
[HarmonyPatch(typeof(Building_CIWS_CE), nameof(Building_CIWS_CE.SpawnSetup))]
public static class Building_CIWS_CE_SpawnSetup_Patch
{
    public static void Postfix(Building_CIWS_CE __instance)
    {
        LongEventHandler.ExecuteWhenFinished(delegate
        {
            if (__instance.Faction == Faction.OfPlayer && __instance.def.defName == "VGE_PointDefenseTurret")
            {
                __instance.ApplyForcedWarcomputerBuffsForCIWS();
            }
        });
    }
}
