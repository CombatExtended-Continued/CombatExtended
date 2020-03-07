using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(GlobalControls), "GlobalControlsOnGUI")]
    internal static class Harmony_GlobalControls
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var track = false;
            var write = false;
            var getComponentMethodInfo = AccessTools.Method(typeof(Map), nameof(Map.GetComponent), new Type[] { }).MakeGenericMethod(typeof(WeatherTracker));
            foreach (var code in instructions)
            {
                if (write)
                {
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Property(typeof(Find), nameof(Find.CurrentMap)).GetGetMethod());
                    yield return new CodeInstruction(OpCodes.Call, getComponentMethodInfo);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WeatherTracker), nameof(WeatherTracker.DoWindGUI)));
                    write = false;
                }
                else if (track)
                {
                    write = code.opcode == OpCodes.Stloc_1;
                    track = !write;
                }
                else
                {
                    track = code.operand is MethodInfo info && info == AccessTools.Method(typeof(WeatherManager), nameof(WeatherManager.DoWeatherGUI));
                }

                yield return code;
            }
        }
    }
}