using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Verse;
using Verse.AI;



/*
 * Initial notes:
 * Trying to find a general solution to the reload after warmup problem (want reload before warmup).
 * Followed several paths and ended up with Verb.TryStartCastOn.
 * That logic has the bit where the pawn gets put into the warmup stance.  Now need to figure out how to insert a path change so that
 *  if a pawn is out of ammo they try to reload and if that job fails they should fail from the TryStartCastOn...
 */
 
 namespace CombatExtended.Harmony
 {
 	[HarmonyPatch(typeof(Verb))]
 	[HarmonyPatch("TryStartCastOn")]
 	[HarmonyPatch(new Type[] { typeof(LocalTargetInfo), typeof(bool), typeof(bool) } )]
	static class Verb_TryStartCastOn_Patch
	{
		
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			// targetting after the lines
			// if (!this.TryFindShootLineFromTo(this.caster.Position, castTarg, out line))
        	// {
            // 		return false;
            // }
			
            bool foundCall = false;
            bool foundBranch = false;
            bool donePatch = false;
            object branchLabel = null;
            
            foreach (CodeInstruction instruction in instructions)
            {
            	if (foundBranch && !donePatch)
            	{
            		// this is where we want to insert our call and branch if it failed.
            		CodeInstruction ldArg = new CodeInstruction(OpCodes.Ldarg_0);
            		CodeInstruction call = new CodeInstruction(OpCodes.Call, typeof(Verb_TryStartCastOn_Patch).GetMethod("CheckReload", AccessTools.all));
            		
            		//CodeInstruction branch = new CodeInstruction(OpCodes.Brfalse, );
            		yield return ldArg;
            		yield return call;
            		yield return new CodeInstruction(OpCodes.Brfalse, branchLabel);
            		donePatch = true;
            	}
            	
            	if (foundCall && !foundBranch && instruction.opcode == OpCodes.Brfalse)
            	{
            		foundBranch = true;
            		branchLabel = instruction.operand;
            		Log.Message(instruction.operand.ToString());
            	}
            	
            	if (!foundCall && instruction.opcode == OpCodes.Call && ((MethodInfo)instruction.operand).Name.Contains("TryFindShootLineFromTo"))
            		foundCall = true;
            	
            	yield return instruction;
            }
		}
		
		// Functions like a prefix.  If this has something to do return false. if nothing to do return true.
		static bool CheckReload(Verb __instance)
		{
			if (!(__instance is Verb_ShootCE || __instance is Verb_ShootCEOneUse))
				return true; // no work to do as the verb isn't the right kind.
			
			CompAmmoUser gun = __instance.ownerEquipment.TryGetComp<CompAmmoUser>();
			if (gun == null || !gun.hasMagazine || gun.curMagCount > 0)
				return true; // gun isn't an ammo user that stores ammo internally or isn't out of bullets.
			
			// we got work to do at this point.
			// Try starting the reload job.
			gun.TryStartReload();
			
			return false;
		}
	}
 }