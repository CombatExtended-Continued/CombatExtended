using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.AI;

/*
 * Targetting the Verse.AI.JobDriver_Wait.CheckForAutoAttack()
 * Target Line:
 *  Thing thing = AttackTargetFinder.BestShootTargetFromCurrentPosition(this.pawn, null, verb.verbProps.range, verb.verbProps.minRange, targetScanFlag);
 *  
 * Basically modify that line to read something like:
 *  Thing thing = AttackTargetFinder.BestShootTargetFromCurrentPosition(this.pawn, GetValidTargetPredicate(verb), verb.verbProps.range, verb.verbProps.minRange, targetScanFlag);
 * 
 * Overall does a couple of things.  First it locates the local variable with the verb used to attack with and second it locates the null argument in the above method call
 * for Predicate and replaces that with an arg stack load of the verb and a call to create a predicate.  That call removes the verb from the call stack and replaces it
 * with a predicate (or a null).
 * 
 * A couple of helper functions to turn a bunch of ifs into a single call since IL can use one of 6 instructions for local variable load/save.
 */

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(JobDriver_Wait), "CheckForAutoAttack")]
    static class Harmony_JobDriverWait_CheckForAutoAttack
    {

        //static MethodInfo Patched_ClosestThingTarget_Global = null;

        /// <summary>
        /// Transpiler runs through the IL code of the method CheckForAutoAttack and makes some tweaks to a call so as to avoid having the pawn attack a target it can't hit.
        /// </summary>
        /// <param name="instructions">IEnumerable of CodeInstruction, required by Harmony, the IL code Harmony fetched/uses.</param>
        /// <returns>IEnumerable of CodeInstruction containing the changes to the method's IL code.</returns>
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int verbLocalIndex = -1;
            int indexKeyCall = 0;

            // turn instructions into a list so we can walk through it variably (instead of forward only).
            List<CodeInstruction> codes = instructions.ToList();
            
            // walk forward to find some key information.
            for (int i = 0; i < codes.Count(); i++)
            {
                // look for the verb instantiation/storage.
                {
                    MethodBase method = null;
                    if (codes[i].opcode == OpCodes.Callvirt && (method = codes[i].operand as MethodBase) != null && method.DeclaringType == typeof(Pawn) && method.Name == $"get_{nameof(Pawn.CurrentEffectiveVerb)}"
                        && codes.Count() >= i + 1)
                        verbLocalIndex = HarmonyBase.OpcodeStoreIndex(codes[i + 1]);
                }

                // see if we've found the instruction index of the key call.
                if (codes[i].opcode == OpCodes.Call && (codes[i].operand as MethodInfo) != null)
                {
                    MethodInfo method = codes[i].operand as MethodInfo;
                    if (method.DeclaringType == typeof(AttackTargetFinder) && method.Name == "BestShootTargetFromCurrentPosition")
                    {
                        indexKeyCall = i;
                        break;
                    }
                }
            }

            // walk backwards from the key call to locate the null load and replace it with our call to drop in our predicate into the arg stack.
            for (int i = indexKeyCall; i >= 0; i--)
            {
                if (codes[i].opcode == OpCodes.Ldnull)
                {
                    codes[i++] = HarmonyBase.MakeLocalLoadInstruction(verbLocalIndex);
                    codes.Insert(i, new CodeInstruction(OpCodes.Call, typeof(Harmony_JobDriverWait_CheckForAutoAttack).GetMethod("GetValidTargetPredicate", AccessTools.all)));
                    break;
                }
            }

            return codes;
        }

        /// <summary>
        /// Returns a predicate for valid targets if the verb is a Verb_LaunchProjectileCE or descendent.
        /// </summary>
        /// <param name="verb">Verb that is to be checked for type and used for valid target checking</param>
        /// <returns>Predicate of type Thing which indicates if that thing is a valid target for the pawn.</returns>
        static Predicate<Thing> GetValidTargetPredicate(Verb verb)
        {
            Predicate<Thing> predicate = null;
            if ((verb as Verb_LaunchProjectileCE) != null)
            {
                Verb_LaunchProjectileCE verbCE = verb as Verb_LaunchProjectileCE;
                predicate = t => verbCE.CanHitTargetFrom(verb.caster.Position, new LocalTargetInfo(t));
            }

            return predicate;
        }
    }
}
