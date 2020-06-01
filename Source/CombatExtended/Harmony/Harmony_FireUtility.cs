using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(FireUtility), "ChanceToStartFireIn")]
    internal static class Harmony_FireUtility_ChanceToStartFireIn
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var write = false;
            foreach (CodeInstruction code in instructions)
            {
                if (write)
                {
                    if (code.opcode == OpCodes.Ldc_I4_1)
                    {
                        code.opcode = OpCodes.Ldc_I4_M1;
                    }

                    write = false;
                }
                else if (code.opcode == OpCodes.Ldfld && ReferenceEquals(code.operand, AccessTools.Field(typeof(ThingDef), nameof(ThingDef.category))))
                {
                    write = true;
                }
                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(FireUtility), "TryStartFireIn")]
    internal static class Harmony_FireUtility_TryStartFireIn
    {
        private const float CatchFireChance = 0.5f;

        internal static void Postfix(IntVec3 c, Map map, float fireSize, bool __result)
        {
            if (!__result)
                return;

            var pawn = c.GetFirstThing<Pawn>(map);
            if (pawn == null)
                return;

            if (Rand.Chance(CatchFireChance * pawn.GetStatValue(StatDefOf.Flammability)))
                pawn.TryAttachFire(fireSize);
        }
    }
}
