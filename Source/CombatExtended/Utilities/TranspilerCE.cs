using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;
using CombatExtended;

namespace CombatExtended.Utilities
{
    public static class TranspilerCE
    {
	public static IEnumerable<CodeInstruction> SeekStart(IEnumerator<CodeInstruction> iterInstructions, CodeInstruction startTarget)
	{
	    bool found = false;
	    while (iterInstructions.MoveNext()) {
		CodeInstruction code = iterInstructions.Current;
		yield return code;
		if (code.opcode == startTarget.opcode && (startTarget.operand == null || code.operand == startTarget.operand))
		{
		    found = true;
		    break;
		}
	    }
	    if (!found)
	    {
		throw new Exception($"Cannot find {startTarget} in current transpiler");
	    }
	}

	public static void SeekStop(IEnumerator<CodeInstruction> iterInstructions, CodeInstruction stopTarget)
	{
	    while (iterInstructions.MoveNext()) {
		CodeInstruction code = iterInstructions.Current;
		if (code.opcode == stopTarget.opcode && (stopTarget.operand == null || code.operand == stopTarget.operand))
		{
		    return;
		}
	    }
	    throw new Exception($"Cannot find {stopTarget} in current transpiler");
	}

	public static IEnumerable<CodeInstruction> Transpile(ILGenerator gen,
							     IEnumerable<CodeInstruction> instructions,
							     IEnumerable<CodeInstruction> patch,
							     CodeInstruction startTarget,
							     CodeInstruction stopTarget = null,
							     int offsetCount = 0,
							     int cutCount = 0,
							     int count = 1)
	{
	    IEnumerator<CodeInstruction> iterInstructions = instructions.GetEnumerator();

	    while (count-- > 0)
	    {
		foreach(var code in SeekStart(iterInstructions, startTarget))
		{
		    yield return code;
		}
		for (int offsetIdx = 0; offsetIdx < offsetCount; offsetIdx++)
		{
		    iterInstructions.MoveNext();
		    yield return iterInstructions.Current;
		}
		foreach(var code in patch)
		{
		    yield return code;
		}
		if (stopTarget != null)
		{
		    SeekStop(iterInstructions, stopTarget);
		}
		for (int cutIdx = 0; cutIdx < cutCount; cutIdx++)
		{
		    iterInstructions.MoveNext();
		}
	    }
	    while (iterInstructions.MoveNext())
	    {
		yield return iterInstructions.Current;
	    }
	}
    }
}
