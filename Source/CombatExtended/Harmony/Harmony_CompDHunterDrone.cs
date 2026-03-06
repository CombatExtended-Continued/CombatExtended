using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended.HarmonyCE;

[HarmonyPatch(typeof(CompHunterDrone), nameof(CompHunterDrone.Detonate))]
public class Harmony_CompHunterDrone_Transpiler
{
    internal static void ThrowFragments(ThingWithComps parent, Map map)
    {
        var fragComp = parent.TryGetComp<CompFragments>();
        fragComp?.Throw(parent.PositionHeld.ToVector3(), map, parent);
    }

    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> list = instructions.ToList();
        bool foundInjection = false;

        foreach (CodeInstruction code in list)
        {

            if (!foundInjection && code.Calls(AccessTools.Method(typeof(GenExplosion), nameof(GenExplosion.DoExplosion))))
            {
                yield return new CodeInstruction(OpCodes.Ldc_R4, 0f); //Populate the height field added in the new doExplosion method
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingComp), nameof(ThingComp.parent)));
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), nameof(Thing.stackCount)));
                yield return new CodeInstruction(OpCodes.Conv_R4);
                yield return new CodeInstruction(OpCodes.Ldc_R4, 0.333f);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.Pow))); //Populate the scaleFactor field with parent.stackCount ^ (1/3)*/

                yield return new CodeInstruction(OpCodes.Ldc_I4_0); // false

                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingComp), nameof(ThingComp.parent)));

                yield return new CodeInstruction(OpCodes.Ldnull);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GenExplosionCE), nameof(GenExplosionCE.DoExplosion)));

                // Fragment part
                yield return new CodeInstruction(OpCodes.Ldarg_0); //instance
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingComp), nameof(ThingComp.parent))); //instance.parent
                yield return new CodeInstruction(OpCodes.Ldarg_1); //map
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_CompHunterDrone_Transpiler), nameof(ThrowFragments)));

                foundInjection = true;
                continue;
            }
            yield return code;

        }

        if (!foundInjection)
        {
            Log.Error($"Combat Extended :: Failed to find first injection point when applying Patch: {HarmonyBase.GetClassName(MethodBase.GetCurrentMethod()?.DeclaringType)}");
        }

    }
}
