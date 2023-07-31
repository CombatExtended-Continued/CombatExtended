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
        private static MethodBase mGet_IsMechanoid = AccessTools.Method(typeof(RaceProperties), "get_IsMechanoid");

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            //Code handling automatically killing mechs on down.  Stock code has the random death on down code in a Rand.Chance block, and would pass a 1 to Rand.Chance if the pawn is a mech.
            //We replace the Rand.Chance check with a method that returns false to remove random death on down.  Thus we need to re-route the branch statement after the get_IsMechanoid() call.
            //Note this is a brittle method, and changes in core code may change where what is effectively a goto call needs to point.
            Label deathOnDownLabel = generator.DefineLabel();

            bool flag = false;

            Log.Message("Combat Extended :: Harmony_Pawn_HealthTracker_CheckForStateChange transpiler called");

            List<CodeInstruction> codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                //Find the get_IsMechanoid() check
                if (codes[i].opcode == OpCodes.Callvirt && codes[i].OperandIs(mGet_IsMechanoid))
                {
                    flag = true;
                    Log.Message("Combat Extended :: Found Get_IsMecchanoid call");
                }
                //take the next unconditional branch
                else if (flag == true && codes[i].opcode == OpCodes.Br_S)
                {
                    // point branch at the down on death code, instead of the random chance check blocking it
                    codes[i] = new CodeInstruction(OpCodes.Br_S, deathOnDownLabel);
                    flag = false;
                    Log.Message("Combat Extended :: Branch at " + i.ToString("X") + " replaced");
                }
                // find instruction calling Verse.DebugViewSettings::logCauseOfDeath and label it
                else if (codes[i].opcode == OpCodes.Ldsfld && ReferenceEquals(codes[i].operand, AccessTools.Field(typeof(DebugViewSettings), "logCauseOfDeath")))
                {
                    codes[i].labels = new List<Label>()
                        {
                            deathOnDownLabel
                        };
                    Log.Message("Combat Extended :: logCauseOfDeath label placed");
                }
            }
            return Transpilers.MethodReplacer(
                       codes,
                       AccessTools.Method(typeof(Rand), nameof(Rand.Chance)),
                       AccessTools.Method(typeof(Harmony_Pawn_HealthTracker_CheckForStateChange), nameof(Harmony_Pawn_HealthTracker_CheckForStateChange.NoChance))
                   );
        }

        static bool NoChance(float unusedChance)
        {
            Log.Message("Combat Extended :: Random Death on down bypassed");
            return false;
        }
    }
}
