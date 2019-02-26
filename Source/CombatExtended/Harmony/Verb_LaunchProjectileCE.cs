using System.Collections;
using System.Collections.Generic;
using Harmony;
using Verse;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(Verb_LaunchProjectileCE), nameof(Verb_LaunchProjectileCE.TryStartCastOn))]
    internal static class Harmony_Verb_LaunchProjectileCE
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var targetMethod = AccessTools.Method(typeof(Verb), nameof(Verb.TryFindShootLineFromTo));
            if (targetMethod == null)
            {
                Log.Error($"CE :: Transpiler could not find method info for {typeof(Verb).Name}.{nameof(Verb.TryStartCastOn)}");
            }
            
            foreach (var code in instructions)
            {
                if (targetMethod != null && code.operand != null && code.operand.Equals(targetMethod))
                {
                    code.operand = AccessTools.Method(typeof(Verb_LaunchProjectileCE), nameof(Verb_LaunchProjectileCE.TryFindCEShootLineFromTo));
                }

                yield return code;
            }
        }
    }
}