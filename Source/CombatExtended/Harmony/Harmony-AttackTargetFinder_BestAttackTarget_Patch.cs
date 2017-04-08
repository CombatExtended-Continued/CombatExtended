using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Harmony.ILCopying;
using Verse;
using Verse.AI;

namespace CombatExtended.Harmony
{
	/* Two part target.
	 * 1> Have AttackTargetFinder_BestAttackTarget_Patch.ClosestThingTarget_Global() be a copy of GenClosest.ClosestThing_Global() with one line of added code.
	 * 1a> This is a bit different since we use some Harmony functions to generate a patched version of ClosestThing_Global() and then insert a jump as the first
	 *     instruction to our AttackTargetFinder_BestAttackTarget_Patch.ClosestThingTarget_Global() thus creating a copy of the original function into a new function
	 *     with our line of code added.
	 * 1b> The added line of code basically being a check for if the potential target thing is too close to cover, to throw it out as if it were too far away.
	 * 
	 * 2> Simply patch AttackTargetFinder.BestAttackTarget() to use our new method AttackTargetFinder_BestAttackTarget_Patch.ClosestThingTarget_Global()
	 *    instead of GenClosest.ClosestThing_Global().
	 * 
	 * The result is that when a pawn is waiting for a target to come into range instead of wasting ammo on a target that is in range but can't be hit due to cover
	 * or standing around doing nothing, the pawn will end up targetting something else that it can hit.
	 * 
	 * Purpose of the non-standard method generation is that with virtually no copy code involved this should theoretically be easier to maintain.  If the patch
	 * code is sufficiently generic which it might not be...
	 */ 
	[StaticConstructorOnStartup]
	static class AttackTargetFinder_BestAttackTarget_Patch
	{
		static readonly string logPrefix = Assembly.GetExecutingAssembly().GetName().Name + " :: " + typeof(AttackTargetFinder_BestAttackTarget_Patch).Name + " :: ";
		static DynamicMethod Patched_ClosestThingTarget_Global = null;
		
		static AttackTargetFinder_BestAttackTarget_Patch()
		{
			// the method we want to copy from.
			MethodInfo oldMethod = typeof(GenClosest).GetMethod("ClosestThing_Global", AccessTools.all);
			// the method we want to copy to.
			MethodInfo newMethod = typeof(AttackTargetFinder_BestAttackTarget_Patch).GetMethod("ClosestThingTarget_Global", AccessTools.all);
			// the method which adds the line of code we want added in order to turn the old method into the new method.
			MethodInfo patchMethod = typeof(AttackTargetFinder_BestAttackTarget_Patch).GetMethod("CopyPatchClosestThing_Global", AccessTools.all);
			
			List<MethodInfo> empty = new List<MethodInfo>();  // doesn't matter...
			List<MethodInfo> transpilers = new List<MethodInfo>(); // puts the transpiler into the format expected by patcher.
			transpilers.Add(patchMethod);
			
			// due to how this patching takes place need to make sure debug is set before running the copy or else no debug output.
			//HarmonyInstance.DEBUG = true;
			// get the patched original method (after the below line the original code will have the new stuff we want added to it).
			Patched_ClosestThingTarget_Global = MethodPatcher.CreatePatchedMethod(oldMethod, empty, empty, transpilers);
			if (Patched_ClosestThingTarget_Global == null) throw new MissingMethodException("Cannot create dynamic replacement for " + oldMethod);
			
			// get the new method to instanciate it's IL.  Otherwise the memory call and WriteJump don't have something to work with...
			newMethod.GetMethodBody().GetILAsByteArray();
			
			// get the memory location of the start of the new method.
			var originalCodeStart = Memory.GetMethodStart(newMethod);
			// get the memory location of the start of our patched old method.
			var patchCodeStart = Memory.GetMethodStart(Patched_ClosestThingTarget_Global);
			// write the jump to the new code into the old method.
			Memory.WriteJump(originalCodeStart, patchCodeStart);
			
			// code to patch BestAttackTarget to use our new method... technically this could be done with class/method annotations.
			MethodInfo source = typeof(AttackTargetFinder).GetMethod("BestAttackTarget", AccessTools.all); // (ProfoundDarkness) I still don't know how to handle some types of args so skipped that.
			MethodInfo transpiler = typeof(AttackTargetFinder_BestAttackTarget_Patch).GetMethod("BestAttackTarget_Patch", AccessTools.all);
			HarmonyBase.instance.Patch(source, null, null, new HarmonyMethod(transpiler));
			
		}
		
		// This patches the code to insert our line of code which will test if the target is too near cover.
		static IEnumerable<CodeInstruction> CopyPatchClosestThing_Global(IEnumerable<CodeInstruction> instructions)
		{
			// This group is related to the state of finding the local variable refering to the currently being checked Thing.
			bool foundClassCast = false;
			int localThingIndex = -1;
			List<OpCode> indexOpcode = new List<OpCode>() { OpCodes.Stloc_0, OpCodes.Stloc_1, OpCodes.Stloc_2, OpCodes.Stloc_3 };
			List<OpCode> outputOpcode = new List<OpCode>() { OpCodes.Ldloc_0, OpCodes.Ldloc_1, OpCodes.Ldloc_2, OpCodes.Ldloc_3 };
			
			
			// this group is related to finding the desired insertion point for our new code.
			bool foundComputation = false;
			bool foundBGE = false;
			bool foundBGT = false;
			bool foundInsertion = false;
			int instructionCount = 0;
			
			// this is the label we will use to emulate a 'continue' in a 'foreach'
			object continueLabel = null;
			
			// (ProfoundDarkness) Yeah, rather inefficient but I find flag based code in this instance a little easier to conceptualize/debug.  Generally only goes through about 1/4th of the IL.
			foreach(CodeInstruction instruction in instructions)
			{
				// the interesting instruction comes after the class cast so that's why this if is above the Classcast check.
				if (foundClassCast)
				{
					if (indexOpcode.Contains(instruction.opcode))
						localThingIndex = indexOpcode.IndexOf(instruction.opcode);
					else if (instruction.opcode == OpCodes.Stloc)
						localThingIndex = (int)instruction.operand;
				}
				
				// (ProfoundDarkness) I know there is a more apt way than ToString() but not sure what that is...
				if (!foundClassCast && instruction.opcode == OpCodes.Castclass && instruction.operand.ToString().Contains("Thing"))
					foundClassCast = true;
				
				
				if (foundComputation && foundBGE && !foundBGT && instruction.opcode == OpCodes.Bgt_Un)
				{
					foundBGT = true;
					continueLabel = instruction.operand;
					break; // found the stuff we needed to setup the patch...
				}
				
				if (foundComputation && !foundBGE && instruction.opcode == OpCodes.Bge_Un)
					foundBGE = true;
				    
				if (!foundInsertion && foundComputation && instruction.opcode == OpCodes.Ldloc_0)
					foundInsertion = true;
				
				if (!foundComputation && instruction.opcode == OpCodes.Call && (instruction.operand as MethodInfo).Name.Contains("get_LengthHorizontalSquared"))
					foundComputation = true;
				
				if (!foundInsertion)
					instructionCount++;
			}
			
			// error check...
			if (continueLabel == null || localThingIndex < 0 || !foundInsertion)
			{
				Log.Warning(string.Concat(logPrefix, "Unable to find either the key label or local variable when generating method based on Verse.GenClosest.ClosestThing_Global(), Pawns may waste ammo on targets they have no hope of hitting."));
				foreach (CodeInstruction instruction in instructions)
					yield return instruction;
			} else {
				bool patched = false;
				foreach (CodeInstruction instruction in instructions)
				{
					if (!patched)
					{
						if (instructionCount <= 0)
						{
							// Roughyly equivelent line of code: if (!CanShootTarget(center, thing)) continue;
							// patched after the line float lengthHorizontalSquared = (center - thing.Position).LengthHorizontalSquared;
							// reason is that location is relatively easy to find in the IL.  Instruction count is what matters so if a better insertion point is found, change instructionCount to fit.
							
							// load first argument for method call. (also first argument that called us.)
							yield return new CodeInstruction(OpCodes.Ldarg_0);
							// load second argument for method call, the current Thing (target) being considered for being shot.
							object index = null;
							if (localThingIndex > 3) index = localThingIndex;
							yield return new CodeInstruction(localThingIndex < 4 ? outputOpcode[localThingIndex] : OpCodes.Ldloc, index);
							// call the new method.
							yield return new CodeInstruction(OpCodes.Call, typeof(AttackTargetFinder_BestAttackTarget_Patch).GetMethod("CanShootTarget", AccessTools.all));
							// branch if the call failed...
							yield return new CodeInstruction(OpCodes.Brfalse, continueLabel);
							patched = true;
						} else
							instructionCount --;
					}
					
					yield return instruction;
				}
			}
		}
		
		// code here doesn't matter...  It's nice if it's got the same signauture.
		static Thing ClosestThingTarget_Global(IntVec3 center, IEnumerable searchSet, float maxDistance = 99999, Predicate<Thing> validator = null)
		{
			return null;
		}
		
		static IEnumerable<CodeInstruction> BestAttackTarget_Patch(IEnumerable<CodeInstruction> instructions)
		{
			return instructions
				.MethodReplacer(typeof(GenClosest).GetMethod("ClosestThing_Global", AccessTools.all),
				                typeof(AttackTargetFinder_BestAttackTarget_Patch).GetMethod("ClosestThingTarget_Global", AccessTools.all));
		}
		
		// goal here is to do a raytrace from target to shooter of no more than 3 cells in length.  If a Thing is encountered which is taller than the target, it can't be hit.
		static bool CanShootTarget(IntVec3 shooterPosition, Thing target)
		{
			//Log.Message(string.Concat(logPrefix, "Shooter at ", shooterPosition.ToString(), " is considering target ", target.LabelCap, " at ", target.Position.ToString()));
			Thing cover;
			if (!Verb_LaunchProjectileCE.GetPartialCoverBetween(target.Map, target.Position.ToVector3Shifted(), shooterPosition.ToVector3Shifted(), out cover))
			{
				//Log.Message(string.Concat(logPrefix, "Result: Can Shoot."));
				return true;
			}
			//Log.Message(string.Concat(logPrefix, "Result: Cannot shoot."));
			return false;
		}
	}
}