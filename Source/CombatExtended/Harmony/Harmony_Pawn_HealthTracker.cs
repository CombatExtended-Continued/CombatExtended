using System.Collections.Generic;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;

namespace CombatExtended.HarmonyCE
{
    /* Dev Notes:
     * The goal in this case is to remove the RNG death event.
     * This is done by replacing the RNG chance with a forced no chance.
     */

    [HarmonyPatch(typeof(Pawn_HealthTracker), "CheckForStateChange")]
    static class Harmony_Pawn_HealthTracker_CheckForStateChange
    {


        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return Transpilers.MethodReplacer(
                instructions,
                AccessTools.Method(typeof(Rand), nameof(Rand.Chance)),
                AccessTools.Method(typeof(Harmony_Pawn_HealthTracker_CheckForStateChange), nameof(Harmony_Pawn_HealthTracker_CheckForStateChange.NoChance))
            );
        }

        static bool NoChance(float unusedChance)
        {
            return false;
        }
    }
}
