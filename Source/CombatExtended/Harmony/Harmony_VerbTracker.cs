using System.Collections.Generic;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_VerbTracker
    {
        /* Dev Notes:
         * The target code should look like this (high level view)
         *  for (int i = 0; i < this.verbs.Count; i++)
         *  {
         *      this.verbs[i].VerbTick();
         *      Verb_LaunchProjectileCE verbCE = this.verbs[i] as Verb_LaunchProjectileCE;
         *      if (verbCE != null)
         *          verbCE.VerbTickCE();
         *  }
         * 
         * The entire IL is short so here...
         * L_0000: Local var #0 System.Int32
         * L_0000: ldarg.0
         * L_0001: ldfld System.Collections.Generic.List`1[Verse.Verb] verbs
         * L_0006: brtrue Label #2
         * L_000b: br Label #0
         * L_0010: Label #2
         * L_0010: ldc.i4.0
         * L_0011: stloc.0
         * L_0012: br Label #3
         * L_0017: Label #4
         * L_0017: ldarg.0
         * L_0018: ldfld System.Collections.Generic.List`1[Verse.Verb] verbs
         * L_001d: ldloc.0
         * L_001e: callvirt Verse.Verb get_Item(Int32)
         * // insert a store to a new local variable here (also a load of that local variable)
         * L_0023: callvirt Void VerbTick()
         * // insert my new logic here.
         * L_0028: ldloc.0 // add a failure label here
         * L_0029: ldc.i4.1
         * L_002a: add
         * L_002b: stloc.0
         * L_002c: Label #3
         * L_002c: ldloc.0
         * L_002d: ldarg.0
         * L_002e: ldfld System.Collections.Generic.List`1[Verse.Verb] verbs
         * L_0033: callvirt Int32 get_Count()
         * L_0038: blt Label #4
         * L_003d: br Label #0
         * L_0042: Label #0
         * L_0042: ret
         */

        // Opted to do the logic and all in pure IL since speed is a bit of an issue so an additional unnecessary call (which would hold the logic) could be costly.
        [HarmonyPatch(typeof(VerbTracker), "VerbsTick")]
        static class Harmony_VerbTracker_VerbsTick
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                int patchPhase = 0;
                var verb = il.DeclareLocal(typeof(VerbTracker));
                var verbCE = il.DeclareLocal(typeof(Verb_LaunchProjectileCE));
                var failBranch = il.DefineLabel();

                foreach (CodeInstruction instruction in instructions)
                {
                    // locate the instruction ldloc.0 which is right after the callvirt that does the VerbTick.
                    if (patchPhase == 1 && instruction.opcode == OpCodes.Ldloc_0)
                    {
                        // load the VerbTracker object stored earlier
                        yield return new CodeInstruction(OpCodes.Ldloc, verb);
                        // convert the VerbTracker object to a Verb_LaunchProjectileCE object (from here just VerbCE)
                        yield return new CodeInstruction(OpCodes.Isinst, typeof(Verb_LaunchProjectileCE));
                        // store the VerbCE object into another local variable.
                        yield return new CodeInstruction(OpCodes.Stloc, verbCE);
                        // Load the just stored VerbCE object onto the stack.
                        yield return new CodeInstruction(OpCodes.Ldloc, verbCE);

                        // branch to failure if the current object is null.
                        yield return new CodeInstruction(OpCodes.Brfalse, failBranch);
                        // restore the VerbCE object onto the stack again...
                        yield return new CodeInstruction(OpCodes.Ldloc, verbCE);
                        // callvirt the method VerbTickCE() (no args, void return).
                        yield return new CodeInstruction(OpCodes.Callvirt, typeof(Verb_LaunchProjectileCE).GetMethod("VerbTickCE", AccessTools.all));

                        // modify the current instruction (should be ldloc.0) to have the label for fail condition.
                        instruction.labels.Add(failBranch);

                        // done
                        patchPhase = 2;
                    }

                    // locate the VerbTick instruction, want to insert before that.
                    if (patchPhase == 0 && instruction.opcode == OpCodes.Callvirt && (instruction.operand as MethodInfo) != null && (instruction.operand as MethodInfo).Name.Equals("VerbTick"))
                    {
                        // store the retrieved VerbTracker object in a local variable.
                        yield return new CodeInstruction(OpCodes.Stloc, verb);
                        // push the JUST stored local variable back onto the stack for use by the next instruction.
                        yield return new CodeInstruction(OpCodes.Ldloc, verb);
                        patchPhase = 1;
                    }


                    yield return instruction;
                }
            }
        }

        /* 
         * This patch is used to see if we need to hide a verb command (attach gizmo) for verbs that are not active
         * 
         * the patch will insert the call here in VerbTracker.GetVerbsCommands
         * 
         * for (int i = 0; i < verbs.Count; i++)
         * {
         *      Verb verb = verbs[i];
         *      if (verb.verbProps.hasStandardCommand && <code inserted here>ShouldGetCommand(verb)</code inserted here>)
         *      {
         *          yield return CreateVerbTargetCommand(ownerThing, verb);
         *      }
         * }         
         */
        [HarmonyPatch]
        public class Harmony_VerbTracker_GetVerbsCommands
        {
            private static FieldInfo fHasStandardCommand = AccessTools.Field(typeof(VerbProperties), nameof(VerbProperties.hasStandardCommand));

            public static MethodBase TargetMethod()
            {
                foreach (var t in typeof(VerbTracker).GetNestedTypes(AccessTools.all))
                    Log.Message(t.Name);
                return AccessTools.Method(typeof(VerbTracker).GetNestedTypes(AccessTools.all).First(t => t.Name.Contains("GetVerbsCommands")), "MoveNext");
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> codes = instructions.ToList();
                bool finished = false;
                for(int i =0;i < codes.Count; i++)
                {
                    yield return codes[i];

                    if (!finished && i > 1)
                    {
                        if(codes[i - 1].opcode == OpCodes.Ldfld && codes[i - 1].OperandIs(fHasStandardCommand))
                        {
                            object l1 = codes[i].operand;                            
                            finished = true;                            
                            yield return new CodeInstruction(OpCodes.Ldloc_2);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_VerbTracker_GetVerbsCommands), nameof(ShouldGetCommand)));
                            yield return new CodeInstruction(OpCodes.Brfalse_S, l1);
                        }
                    }                    
                }
            }

            private static bool ShouldGetCommand(Verb verb)
            {
                if(verb is Verb_ShootUseAttachment useAttachment)                
                    return useAttachment.Enabled;                
                return true;
            }
        }

        [HarmonyPatch(typeof(VerbTracker), nameof(VerbTracker.PrimaryVerb), MethodType.Getter)]
        public class Harmony_VerbTracker_PrimaryVerb
        {
            public static bool Prefix(VerbTracker __instance, ref Verb __result)
            {
                if (__instance.directOwner is CompEquippable equippable)
                {
                    if (equippable.verbTracker.verbs == null)
                    {
                        equippable.verbTracker.InitVerbsFromZero();
                        return true;
                    }
                    if (equippable.parent is WeaponPlatform platform)
                    {
                        __result = platform?.SelectedVerb;
                        return __result == null;
                    }                    
                }                                                 
                return true;
            }
        }
    }
}
