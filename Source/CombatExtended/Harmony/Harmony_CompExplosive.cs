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

        /*
            instance void Detonate (
            class Verse.Map map, -> Ldarg_1
            [opt] bool ignoreUnspawned
            )
            
            1.5
            .locals init (
            [0] class RimWorld.CompProperties_Explosive,
            [1] float32,
            [2] class Verse.Thing,
            [3] valuetype [mscorlib]System.Nullable`1<valuetype Verse.DamageInfo>,
            [4] bool,
            [5] valuetype [mscorlib]System.Nullable`1<float32>,
            [6] valuetype [mscorlib]System.Nullable`1<valuetype Verse.FloatRange>
            )

            1.4
            .locals init (
            [0] class RimWorld.CompProperties_Explosive,
            [1] float32,
            [2] class Verse.Thing,
            [3] valuetype [mscorlib]System.Nullable`1<valuetype Verse.DamageInfo>,
            [4] valuetype [mscorlib]System.Nullable`1<float32>,
            [5] valuetype [mscorlib]System.Nullable`1<valuetype Verse.FloatRange>
            )
        */
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool codeFound = false;

            foreach (var code in instructions)
            {

                // Replace GenExplosion.DoExplosion with GenExplosionCE.DoExplosion, populate extra four fields with "0f" and "Mathf.Pow(this.parent.stackCount, 0.333f)", "this.destroyedThroughDetonation" and "this.parent"
                if (!codeFound && code.Calls(AccessTools.Method(typeof(GenExplosion), nameof(GenExplosion.DoExplosion))))
                {
                    codeFound = true;

                    yield return new CodeInstruction(OpCodes.Ldc_R4, 0f);  //Populate the height field added in the new doExplosion method
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingComp), nameof(ThingComp.parent)));
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), nameof(Thing.stackCount)));
                    yield return new CodeInstruction(OpCodes.Conv_R4);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 0.333f);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.Pow)));    //Populate the scaleFactor field with parent.stackCount ^ (1/3)*/
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CompExplosive), nameof(CompExplosive.destroyedThroughDetonation)));    //Send along whether the thing should be destroyed
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingComp), nameof(ThingComp.parent)));                //Send along parent, e.g the thing that should be destroyed
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GenExplosionCE), nameof(GenExplosionCE.DoExplosion)));

                    // Fragment part
                    yield return new CodeInstruction(OpCodes.Ldarg_0); //instance
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingComp), nameof(ThingComp.parent))); //instance.parent
                    yield return new CodeInstruction(OpCodes.Ldarg_1); //map
                    yield return new CodeInstruction(OpCodes.Ldloc_2); //instigator
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_CompExplosive_Detonate_Transpiler), nameof(Harmony_CompExplosive_Detonate_Transpiler.ThrowFragments)));

                    continue;
                }

                yield return code;
            }

            if (!codeFound)
            {
                Log.Warning("CombatExtended :: Could not find doExplosionInfo");
            }

            /* if (!codeChanged)
            {
                Log.Warning("CombatExtended :: Could not insert ThrowFragments call");
            } */
        }
    }
}
