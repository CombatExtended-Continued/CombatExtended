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
    [HarmonyPatch(typeof(CompExplosive),"Detonate")]
    public class Harmony_CompExplosive_Detonate_Transpiler
    {
        internal static void ThrowFragments(ThingWithComps parent)
        {
            var fragComp = parent.TryGetComp<CompFragments>();
            if (fragComp != null)
                fragComp.Throw(parent.PositionHeld.ToVector3(), parent.Map, parent, Mathf.Pow(parent.stackCount, 0.333f));
        }
        
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool codeFound = false;
            int parentCalls = 0;
            bool codeChanged = false;
            
            foreach (var code in instructions)
            {
                if (code.opcode == OpCodes.Stloc_2)
                {
                    parentCalls++;
                }
                
                // Replace GenExplosion.DoExplosion with GenExplosionCE.DoExplosion
                if (codeChanged && !codeFound && code.opcode == OpCodes.Call && code.operand is MethodInfo info
                    && info == AccessTools.Method(typeof(GenExplosion), "DoExplosion"))
                {
                    codeFound = true;

                    yield return new CodeInstruction(OpCodes.Ldc_R4, 0f);  //Populate the height field added in the new doExplosion method
                    yield return new CodeInstruction(OpCodes.Ldarg_0, null);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingComp), "parent"));
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "stackCount"));
                    yield return new CodeInstruction(OpCodes.Conv_R4, null);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 0.333f);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), "Pow"));    //Populate the scaleFactor field with parent.stackCount ^ (1/3)*/
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GenExplosionCE), "DoExplosion"));

                    continue;
                }

                if (parentCalls == 2 && !codeChanged && code.opcode == OpCodes.Callvirt)
                {
                    // Call ThrowFragments and duplicate previous calls
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_CompExplosive_Detonate_Transpiler), "ThrowFragments"));
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingComp), "parent"));

                    codeChanged = true;
                }

                yield return code;
            }
            
            if (!codeFound)
                Log.Warning("CombatExtended :: Could not find doExplosionInfo");

            if (!codeChanged)
                Log.Warning("CombatExtended :: Could not insert ThrowFragments call");
        }
    }
}