using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    /// <summary>
    /// Transpile <see cref="JobGiver_AIBreaching.UpdateBreachingTarget(Pawn, Verb)"/> to allow mechanoids to use breaching weapons
    /// with a nonzero friendly fire avoidance radius.
    /// </summary>
    [HarmonyPatch(typeof(JobGiver_AIBreaching), nameof(JobGiver_AIBreaching.UpdateBreachingTarget))]
    internal static class Harmony_JobGiver_AIBreaching
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();

            MethodInfo mIsSoloAttackVerb = AccessTools.Method(typeof(BreachingUtility), nameof(BreachingUtility.IsSoloAttackVerb));
            MethodInfo pRaceProps = AccessTools.DeclaredPropertyGetter(typeof(Pawn), nameof(Pawn.RaceProps));
            MethodInfo pIsMechanoid = AccessTools.DeclaredPropertyGetter(typeof(RaceProperties), nameof(RaceProperties.IsMechanoid));

            Label checkNoDesignatedSoloAttackerExistsLabel = generator.DefineLabel();

            for (int i = 0; i < instructionsList.Count; i++)
            {
                // If the current pawn's weapon is only usable for "solo attack", i.e. has a nonzero friendly fire avoidance radius,
                // ensure the pawn can still use it if they are a mechanoid. Mechanoid breach raids never have a designated solo attacker,
                // so they would not be able to use breaching weapons with a friendly fire avoidance radius without this change.
                if (instructionsList[i].Branches(out Label? checkPawnIsSoloAttackerLabel) && instructionsList[i - 1].Calls(mIsSoloAttackVerb))
                {
                    instructionsList[i + 1].labels.Add(checkNoDesignatedSoloAttackerExistsLabel);

                    yield return new CodeInstruction(OpCodes.Brfalse_S, checkNoDesignatedSoloAttackerExistsLabel);
                    yield return new CodeInstruction(OpCodes.Ldarg_1); // pawn
                    yield return new CodeInstruction(OpCodes.Callvirt, pRaceProps);
                    yield return new CodeInstruction(OpCodes.Callvirt, pIsMechanoid);
                    yield return new CodeInstruction(OpCodes.Brfalse_S, (Label)checkPawnIsSoloAttackerLabel);
                }
                else
                {
                    yield return instructionsList[i];
                }
            }
        }
    }
}
