using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CombatExtended.AI;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE
{
    internal static class Harmony_ToxicUtility
    {
        /// <summary>
        /// Transpile <see cref="ToxicUtility.DoAirbornePawnToxicDamage"/> to tell AI pawns to wear mask
        /// used when outside during Toxic Fallout
        /// </summary>
        [HarmonyPatch(typeof(ToxicUtility), nameof(ToxicUtility.DoAirbornePawnToxicDamage))]
        static class Patch_DoAirbornePawnToxicDamage
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> codes = instructions.ToList();

                MethodInfo targetMethod = AccessTools.Method(typeof(ToxicUtility), nameof(ToxicUtility.DoPawnToxicDamage));
                MethodInfo additionalMethod = AccessTools.Method(typeof(Harmony_ToxicUtility), nameof(Harmony_ToxicUtility.TryNotify_ShouldEquipGasMask));
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].Calls(targetMethod))
                    {
                        codes.Insert(i - 1, new CodeInstruction(OpCodes.Call, additionalMethod));
                        codes.Insert(i - 1, new CodeInstruction(OpCodes.Dup));
                        break;
                    }
                }
                return codes;
            }
        }
        /// <summary>
        /// Transpile <see cref="ToxicUtility.PawnToxicTickInterval"/> to tell AI pawns to wear mask
        /// used when standing on polluted terrain
        /// </summary>
        [HarmonyPatch(typeof(ToxicUtility), nameof(ToxicUtility.PawnToxicTickInterval))]
        static class Patch_PawnToxicTickInterval
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> codes = instructions.ToList();

                MethodInfo targetMethod = AccessTools.Method(typeof(ToxicUtility), nameof(ToxicUtility.DoPawnToxicDamage));
                MethodInfo additionalMethod = AccessTools.Method(typeof(Harmony_ToxicUtility), nameof(Harmony_ToxicUtility.TryNotify_ShouldEquipGasMask));
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].Calls(targetMethod))
                    {
                        codes.Insert(i - 1, new CodeInstruction(OpCodes.Call, additionalMethod));
                        codes.Insert(i - 1, new CodeInstruction(OpCodes.Dup));
                        break;
                    }
                }
                return codes;
            }
        }
        public static void TryNotify_ShouldEquipGasMask(Pawn pawn)
        {
            if (pawn.RaceProps.Humanlike && !pawn.IsSubhuman)
            {
                pawn.TryGetComp<CompTacticalManager>()?.GetTacticalComp<CompGasMask>()?.Notify_ShouldEquipGasMask(true);
            }
        }
    }
}
