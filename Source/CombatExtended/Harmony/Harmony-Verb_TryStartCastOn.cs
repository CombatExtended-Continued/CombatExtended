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
 	[HarmonyPatch(typeof(Verb), "TryStartCastOn", new Type[] { typeof(LocalTargetInfo), typeof(bool), typeof(bool) } )]
	static class Harmony_Verb_TryStartCastOn
	{
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
            // targetting after the lines
            // if (!this.TryFindShootLineFromTo(this.caster.Position, castTarg, out line))
            // {
            // 		return false;
            // }

            int patchPhase = 0;

            Label? branchLabel = null;
            
            foreach (CodeInstruction instruction in instructions)
            {
            	if (patchPhase == 2)
            	{
                    // this is where we want to insert our call and branch if it failed.
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
            		yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Verb_TryStartCastOn), "CheckReload"));
            		yield return new CodeInstruction(OpCodes.Brfalse, branchLabel);
                    patchPhase = 3;
            	}
            	
            	if (patchPhase == 1 && instruction.opcode == OpCodes.Brfalse)
            	{
                    patchPhase = 2;
            		branchLabel = instruction.operand as Label?;
            	}

                if (patchPhase == 0 && instruction.opcode == OpCodes.Call // && HarmonyBase.doCast((instruction.operand as MethodInfo).MemberType.Equals(typeof(Verb))) // this condition isn't working for some unknown reason...
                    && HarmonyBase.doCast((instruction.operand as MethodInfo).Name.Equals("TryFindShootLineFromTo")))
                    patchPhase = 1;
            	
            	yield return instruction;
            }
		}
		
		// Functions like a prefix.  If this has something to do return false. if nothing to do return true.
		static bool CheckReload(Verb __instance)
		{
			if (!(__instance is Verb_ShootCE || __instance is Verb_ShootCEOneUse))
				return true; // no work to do as the verb isn't the right kind.

			CompAmmoUser gun = __instance.EquipmentSource.TryGetComp<CompAmmoUser>();
			if (gun == null || !gun.HasMagazine || gun.CurMagCount > 0)
				return true; // gun isn't an ammo user that stores ammo internally or isn't out of bullets.

            // we got work to do at this point.
            // Try starting the reload job.
            gun.TryStartReload();
			return false;
		}
	}
 }