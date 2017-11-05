using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using RimWorld;
using UnityEngine;
using Harmony;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(DamageWorker_AddInjury), "ApplyDamageToPart")]
    internal static class Harmony_DamageWorker_AddInjury_ApplyDamageToPart
    {
        private static bool armorAbsorbed = false;

        private static void ArmorReroute(Pawn pawn, ref DamageInfo dinfo)
        {
            var newDinfo = ArmorUtilityCE.GetAfterArmorDamage(dinfo, pawn, dinfo.HitPart, out armorAbsorbed);
            if (dinfo.HitPart != newDinfo.HitPart)
            {
                if (pawn.Spawned) LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_ArmorSystem, OpportunityType.Critical);   // Inform the player about armor deflection
            }
            dinfo = newDinfo;
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            // Find armor block
            var armorBlockEnd = codes.FirstIndexOf(c => c.operand == typeof(ArmorUtility).GetField(nameof(ArmorUtility.GetPostArmorDamage)));
            int armorBlockStart = -1;
            for (int i = armorBlockEnd; i > 0; i--)
            {
                if (codes[i].opcode == OpCodes.Ldarg_2)
                {
                    armorBlockStart = i;
                    break;
                }
            }
            if (armorBlockStart == -1)
            {
                Log.Error("CE failed to transpile DamageWorker_AddInjury: could not identify armor block start");
            }

            // Replace armor block with our new instructions
            // First, load arguments for ArmorReroute method onto stack (pawn is already loaded by vanilla)
            var curCode = codes[armorBlockStart + 1];
            curCode.opcode = OpCodes.Ldarg_1;
            curCode.operand = null;

            curCode = codes[armorBlockStart + 2];
            curCode.opcode = OpCodes.Call;
            curCode.operand = typeof(Harmony_DamageWorker_AddInjury_ApplyDamageToPart).GetField(nameof(Harmony_DamageWorker_AddInjury_ApplyDamageToPart.ArmorReroute));

            // OpCode + 3 loads the dinfo we just modified and we want to access its damage value to store in the vanilla local variable at the end of the block
            curCode = codes[armorBlockStart + 4];
            curCode.opcode = OpCodes.Call;
            curCode.operand = typeof(DamageInfo).GetProperty(nameof(DamageInfo.Amount));

            // Null out the rest
            for (int i = 5; i <= armorBlockEnd; i++)
            {
                curCode = codes[i];
                curCode.opcode = OpCodes.Nop;
                curCode.operand = null;
            }

            return codes;
        }
    }
}
