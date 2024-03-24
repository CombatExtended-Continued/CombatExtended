using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using RimWorld;
using CombatExtended;
using UnityEngine;

namespace CombatExtended.HarmonyCE
{
    /// <summary>
    /// Replaces DoExplosion on BUILDINGS, PAWNS, AMMO with the CE version that has correct AP and damage
    /// </summary>
    [HarmonyPatch(typeof(CompExplosive), nameof(CompExplosive.Detonate))]
    public class Harmony_CompExplosive_Detonate_Transpiler
    {
        internal static void ThrowFragments(ThingWithComps parent, Map map, Thing instigator)
        {
            var fragComp = parent.TryGetComp<CompFragments>();
            if (fragComp != null)
            {
                fragComp.Throw(parent.PositionHeld.ToVector3(), map, instigator);    //Mathf.Pow(parent.stackCount, 0.333f));
            }
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            //TODO: 1.5
            foreach (var code in instructions)
                yield return code;
            yield break;
            //            bool codeFound = false;
            //            int parentCalls = 0;
            //            bool codeChanged = false;
            //            bool storeDestructionCall = false;
            //
            //            foreach (var code in instructions)
            //            {
            //                if (storeDestructionCall)
            //                {
            //                    if (code.opcode == OpCodes.Callvirt)
            //                    {
            //                        storeDestructionCall = false;
            //                    }
            //
            //                    continue;
            //                }
            //
            //                if (code.opcode == OpCodes.Stfld && code.operand is FieldInfo info
            //                    && info == AccessTools.Field(typeof(CompExplosive), nameof(CompExplosive.destroyedThroughDetonation)))
            //                {
            //                    yield return code;
            //                    storeDestructionCall = true;
            //                    continue;
            //                }
            //
            //                if (code.opcode == OpCodes.Stloc_2)
            //                {
            //                    parentCalls++;
            //                }
            //
            //                // Replace GenExplosion.DoExplosion with GenExplosionCE.DoExplosion, populate extra four fields with "0f" and "Mathf.Pow(this.parent.stackCount, 0.333f)", "this.destroyedThroughDetonation" and "this.parent"
            //                if (codeChanged && !codeFound && code.opcode == OpCodes.Call && code.operand is MethodInfo info2
            //                    && info2 == AccessTools.Method(typeof(GenExplosion), nameof(GenExplosion.DoExplosion)))
            //                {
            //                    codeFound = true;
            //
            //                    yield return new CodeInstruction(OpCodes.Ldc_R4, 0f);  //Populate the height field added in the new doExplosion method
            //                    yield return new CodeInstruction(OpCodes.Ldarg_0);
            //                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingComp), nameof(ThingComp.parent)));
            //                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), nameof(Thing.stackCount)));
            //                    yield return new CodeInstruction(OpCodes.Conv_R4);
            //                    yield return new CodeInstruction(OpCodes.Ldc_R4, 0.333f);
            //                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.Pow)));    //Populate the scaleFactor field with parent.stackCount ^ (1/3)*/
            //                    yield return new CodeInstruction(OpCodes.Ldarg_0);
            //                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CompExplosive), nameof(CompExplosive.destroyedThroughDetonation)));    //Send along whether the thing should be destroyed
            //                    yield return new CodeInstruction(OpCodes.Ldarg_0);
            //                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingComp), nameof(ThingComp.parent)));                //Send along parent, e.g the thing that should be destroyed
            //                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GenExplosionCE), nameof(GenExplosionCE.DoExplosion)));
            //
            //                    continue;
            //                }
            //
            //                if (parentCalls == 2 && !codeChanged && code.opcode == OpCodes.Callvirt)
            //                {
            //                    // Call ThrowFragments and duplicate previous calls
            //                    yield return new CodeInstruction(OpCodes.Ldarg_1);
            //                    yield return new CodeInstruction(OpCodes.Ldloc_2);
            //                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_CompExplosive_Detonate_Transpiler), nameof(Harmony_CompExplosive_Detonate_Transpiler.ThrowFragments)));
            //                    yield return new CodeInstruction(OpCodes.Ldarg_0);
            //                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingComp), nameof(ThingComp.parent)));
            //
            //                    codeChanged = true;
            //                }
            //
            //                yield return code;
            //            }
            //
            //            if (!codeFound)
            //            {
            //                Log.Warning("CombatExtended :: Could not find doExplosionInfo");
            //            }
            //
            //            if (!codeChanged)
            //            {
            //                Log.Warning("CombatExtended :: Could not insert ThrowFragments call");
            //            }
        }
    }
}
