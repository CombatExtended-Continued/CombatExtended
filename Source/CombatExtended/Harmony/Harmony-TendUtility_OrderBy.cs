using System.Reflection;
using Harmony;
using Verse;
using System;
using System.Reflection.Emit;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace CombatExtended.Harmony
{
    /* Somewhat complex patch...  If we JUST wanted to order by bleedrate it's easy, Harmony prefix of:
     *  private static float <DoTend>m__55(Hediff_Injury x)
     *  {
     *      return x.Severity; // change this to return x.BleedRate;
     *  }
     * Instead we want bleedrate THEN severity.
     * 
     * Targetting RimWorld.TendUtility.DoTend method.
     * Goal is to change the 'line' from:
     *  IEnumerator<Hediff_Injury> enumerator = (
     *      from x in patient.health.hediffSet.GetInjuriesTendable()
     *      orderby x.Severity descending
     *      select x).GetEnumerator();
     * To:
     *  IEnumerator<Hediff_Injury> enumerator = (
     *      from x in patient.health.hediffSet.GetInjuriesTendable()
     *      orderby x.BleedRate descending
     *      thenby x.Severity descending
     *      select x).GetEnumerator();
     * 
     * Basically ended up swapping out all the code after GetInjuriesTendable to GetEnumerator...
     * 
     * I (ProfoundDarkness) left much of my planning thoughts here as an example... It took some study to figure out what I should do but fell back on what is the stack state to solve...
     */

    /*
     * New Behavior Notes:
     *  -Pawns who are bleeding due to amputation seem to have a different treatment system, the amputation is treated last still.
     *  -The most rapid bleeding injury is now treated first however this can be hidden from view as groups of injuries are pooled and this is looking at per injury.
     *  -It appears that pawns who are bleeding to death faster get treatment sooner, this was unintended and a very small sample size.
     */

    /* (ProfoundDarkness) Planning thoughts... (Retained in case someone works on something similar)
     * concept: Exploration of changing the order that injuries are treated from severity to bleedrate.
     * Ideally would also order bleedrate by severity so more severe injuries are treated if bleedrate is equal among them.
     * 
     * After studying the code some... since I don't know precisely how linqs get transformed I've settled on the best bet to
     * add in a call which takes the current list (and anything else it needs to work) and does a ThenBy Descending linq, returning the result...
     * 
     * So needs to be between the IL lines:
     *  L_00e3: call IOrderedEnumerable`1 OrderByDescending[Hediff_Injury,Single](IEnumerable`1, System.Func`2[Verse.Hediff_Injury,System.Single])
     *  L_00e8: callvirt IEnumerator`1 GetEnumerator()
     * 
     * Trick is to keep the stack in the right situation for GetEnumerator while setting the stack up for my call...
     * 
     * Looks like my return type is IOrderedEnumerable.
     * My method needs the current IOrderedEnumerable/IEnumerable object and probably nothing else...
     *  Should be simple, just catch the enumerator call and insert my call before it, mine consumes one stack and puts one back of the same type which is what GetEnumerator() is called on.
     * 
     * For increased performance I should probably remove all the original functions and checks for OrderBy x.Severity... so roughly from/to (inclusive):
     *  L_00c3: ldsfld System.Func`2[Verse.Hediff_Injury,System.Single] <>f__am$cache2
     *  L_00c8: brtrue Label #10
     *  L_00cd: ldnull
     *  L_00ce: ldftn Single <DoTend>m__55(Verse.Hediff_Injury)
     *  L_00d4: newobj Void .ctor(Object, IntPtr)
     *  L_00d9: stsfld System.Func`2[Verse.Hediff_Injury,System.Single] <>f__am$cache2
     *  L_00de: Label #10
     *  L_00de: ldsfld System.Func`2[Verse.Hediff_Injury,System.Single] <>f__am$cache2
     *  L_00e3: call IOrderedEnumerable`1 OrderByDescending[Hediff_Injury,Single](IEnumerable`1, System.Func`2[Verse.Hediff_Injury,System.Single])
     * 
     * So the new code would be something like:
     *  L_00be: callvirt IEnumerable`1 GetInjuriesTendable()
     *  <Insert my call...>
     *  L_00e8: callvirt IEnumerator`1 GetEnumerator()
     * 
     * (Edit) My original pass was JUST inserting before GetEnumerator and after that functioned I added the removal code and additional logic.(/Edit)
     */

    [HarmonyPatch(typeof(TendUtility))] //target class
    [HarmonyPatch("DoTend")]            //target method
    [HarmonyPatch(new Type[] { typeof(Pawn), typeof(Pawn), typeof(Medicine) })] //target signature
    static class Harmony_TendUtility_OrderBySeverity_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool cuttingCode = false;
            int hitCount = 0;
            bool patched = false;

            foreach (CodeInstruction instruction in instructions)
            {
                // looking for the 2nd (of 2) instance of the callvirt for GetInjuriesTendable...
                if (!patched && !cuttingCode && instruction.opcode == OpCodes.Callvirt && (instruction.operand as MethodInfo)?.Name == "GetInjuriesTendable")
                {
                    if (hitCount >= 1)
                    {
                        yield return instruction; // need to yield this instruction, it's not being cut...
                        cuttingCode = true; // the normal yield won't fire with this set to true.
                    }
                    hitCount++; // increment our found instances... Do this last so we don't accidentally fire codeCutting too soon.
                }
                
                // looking for the only callVirt instruction to GetEnumerator...
                if (!patched && cuttingCode && instruction.opcode == OpCodes.Callvirt && (instruction.operand as MethodInfo)?.Name == "GetEnumerator")
                {
                    cuttingCode = false;  // stop cutting code.
                    patched = true; // flag that we have inserted our patch (the next line).
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_TendUtility_OrderBySeverity_Patch), "InjuriesReordered"));
                }
                if (!cuttingCode)
                    yield return instruction;
            }
        }

        /// <summary>
        /// Handles the new orderby rules to treat Bleedrate before Severity (but within equal sets of bleedrate, treat more severe first).
        /// </summary>
        /// <param name="ordered">the IEnumerable of Hediff_Injury to apply the new ordering to.</param>
        /// <returns>An IOrderedEnumerable of Hediff_Injury object with the new order applied.</returns>
        static IOrderedEnumerable<Hediff_Injury> InjuriesReordered(IEnumerable<Hediff_Injury> ordered)
        {
            return ordered.OrderByDescending(x => x.BleedRate).ThenByDescending(x => x.Severity);
        }
    }
}
