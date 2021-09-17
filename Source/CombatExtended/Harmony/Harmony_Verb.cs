using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Verse;
using RimWorld;
using System.Reflection.Emit;
using System.Reflection;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Verb), nameof(Verb.IsStillUsableBy))]
    internal static class Harmony_Verb_IsStillUsableBy
    {
        internal static void Postfix(Verb __instance, ref bool __result, Pawn pawn)
        {
            if (__result)
            {
                var tool = __instance.tool as ToolCE;
                if (tool != null)
                {
                    __result = tool.restrictedGender == Gender.None || tool.restrictedGender == pawn.gender;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Verb), nameof(Verb.TryCastNextBurstShot))]
    internal static class Harmony_Verb_TryCastNextBurstShot
    {
        private static FieldInfo fverbProps = AccessTools.Field(typeof(Verb), nameof(Verb.verbProps));
        private static FieldInfo fticksBetweenBurstShots = AccessTools.Field(typeof(VerbProperties), nameof(VerbProperties.ticksBetweenBurstShots));

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = instructions.ToList();
            bool finished = false;
            for(int i= 0;i < codes.Count; i++)
            {
                if (!finished)
                {
                    if (codes[i].opcode == OpCodes.Ldfld && codes[i + 1].opcode == OpCodes.Ldfld && codes[i].OperandIs(fverbProps) && codes[i + 1].OperandIs(fticksBetweenBurstShots))
                    {
                        finished = true;                        
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Verb_TryCastNextBurstShot), nameof(GetTicksBetweenBurstShots)));
                        i++;
                        continue;
                    }
                }
                yield return codes[i];
            }            
        }

        private static int GetTicksBetweenBurstShots(Verb verb)
        {
            float ticksBetweenBurstShots = verb.verbProps.ticksBetweenBurstShots;
            if(verb is Verb_LaunchProjectileCE && verb.EquipmentSource != null)
            {
                float modified = verb.EquipmentSource.GetStatValue(CE_StatDefOf.TicksBetweenBurstShots);
                if (modified > 0)
                    ticksBetweenBurstShots = modified;
            }
            return (int)ticksBetweenBurstShots;
        }
    }
}
