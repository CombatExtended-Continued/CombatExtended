using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using System.Reflection.Emit;
using System;


namespace CombatExtended.HarmonyCE.Compatibility
{
    [HarmonyPatch]
    class Harmony_Compat_RunAndGun
    {
        static readonly string logPrefix = Assembly.GetExecutingAssembly().GetName().Name + " :: ";
        static readonly Assembly ass = AppDomain.CurrentDomain.GetAssemblies().
                                        SingleOrDefault(assembly => assembly.GetName().Name == "RunAndGun");
        static MethodBase targetMethod = null;
        static Type Stance_RunAndGun = null;

        internal static bool Prepare()
        {
            if (ass != null)
            {
                foreach (var t in ass.GetTypes())
                {
                    if (t.Name == "Verb_TryStartCastOn")
                    {
                        targetMethod = AccessTools.Method(t, "Prefix");
                    }
                    if (t.Name == "Stance_RunAndGun")
                    {
                        Stance_RunAndGun = t;
                        CompAmmoUser.rgStance = t;
                    }
                }
                if (targetMethod == null || Stance_RunAndGun == null)
                {
                    Log.Error($"{logPrefix}Failed to find target method while attempting to patch RunAndGun.");
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static MethodBase TargetMethod()
        {
            Log.Message($"{logPrefix}Applying compatibility patch for {ass.FullName}");
            return targetMethod;
        }
        
        internal static IEnumerable<CodeInstruction> Transpiler(ILGenerator gen, IEnumerable<CodeInstruction> instructions)
        {
            bool patched = false;
            bool ready = false;

            List<CodeInstruction> patch = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Compat_RunAndGun), nameof(CanBeFiredNow))),
                new CodeInstruction(OpCodes.Brfalse)
            };

            foreach (var code in instructions)
            {
                yield return code;
                if (!patched)
                {
		  if (code.opcode == OpCodes.Isinst && ReferenceEquals(code.operand, Stance_RunAndGun))
                    {
                        ready = true;
                    }
                    if (ready && code.opcode == OpCodes.Brtrue)
                    {
                        patch.Last().operand = code.operand;
                        foreach (var c in patch)
                        {
                            yield return c;
                        }
                        patched = true;
                    }
                }
            }
        }

        internal static bool CanBeFiredNow(Verb instance)
        {
            return instance.EquipmentSource.TryGetComp<CompAmmoUser>()?.CanBeFiredNow ?? true;
        }
    }
}
