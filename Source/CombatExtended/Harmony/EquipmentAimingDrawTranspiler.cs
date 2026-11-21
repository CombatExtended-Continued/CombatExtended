using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace CombatExtended.HarmonyCE;

internal static class EquipmentAimingDrawTranspiler
{
    internal static bool TryReplaceDrawCall(List<CodeInstruction> codes, MethodInfo original, MethodInfo replacement)
    {
        // Other transpilers (including RimDark's decorations) can append calls
        // before ret. Match the exact DrawMesh overload, not its old position.
        int index = codes.FindLastIndex(code => code.Calls(original));
        if (index < 0)
        {
            return false;
        }

        var call = codes[index];
        var equipment = new CodeInstruction(OpCodes.Ldarg_0);
        equipment.labels.AddRange(call.labels);
        equipment.blocks.AddRange(call.blocks);
        call.labels.Clear();
        call.blocks.Clear();
        call.opcode = OpCodes.Call;
        call.operand = replacement;
        codes.InsertRange(index, new[]
        {
            equipment,
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldarg_2)
        });
        return true;
    }
}
