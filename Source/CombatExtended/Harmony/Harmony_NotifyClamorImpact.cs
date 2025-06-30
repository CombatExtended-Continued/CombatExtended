using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE;

[HarmonyPatch(typeof(Pawn_MindState), nameof(Pawn_MindState.Notify_ClamorImpact))]
public class Harmony_NotifyClamorImpact
{
    public static bool Prefix(Pawn_MindState __instance, Thing instigator)
    {
        if (__instance.pawn.IsAnimal && instigator is ProjectileCE proj && proj.AnimalsFleeImpact &&
            (__instance.pawn.playerSettings == null || __instance.pawn.playerSettings.Master == null) &&
            Rand.Chance(0.6f) && FleeUtility.ShouldAnimalFleeDanger(__instance.pawn))
        {
            __instance.StartFleeingBecauseOfPawnAction(proj);
            return false;
        }
        return true;
    }
}
