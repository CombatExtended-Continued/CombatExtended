using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using System.Reflection.Emit;
using System;

namespace CombatExtended.HarmonyCE.Compatibility
{
    [HarmonyPatch]
    class Harmony_Compat_BBBodySupport
    {
        static readonly string logPrefix = Assembly.GetExecutingAssembly().GetName().Name + " :: ";
        static Assembly ass = AppDomain.CurrentDomain.GetAssemblies().
                                SingleOrDefault(assembly => assembly.
                                GetName().Name == "BBBodySupport");

        private static bool IsHeadwear(ApparelLayerDef layer)
        {
            return layer.GetModExtension<ApparelLayerExtension>()?.IsHeadwear ?? false;
        }

        static bool Prepare()
        {
            if (ass?.FullName.Contains("BBBodySupport") ?? false)
            {
                return true;
            }
            return false;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            var found = false;
            foreach (var t in ass.GetTypes())
            {
                foreach (var m in AccessTools.GetDeclaredMethods(t))
                {
                    if (m.Name.Contains("BBBody_ApparelPatch") || m.Name.Contains("BBBody_ApparelZombiefiedPatch"))
                    {
                        found = true;
                        yield return m;
                    }
                }
            }
            if (found)
            {
                Log.Message($"{logPrefix}Applying compatibility patch for {ass.FullName}");
            }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool patched = false;
            bool ready = false;

            List<CodeInstruction> patch = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldind_Ref),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), nameof(Thing.def))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef), nameof(ThingDef.apparel))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Property(typeof(ApparelProperties), nameof(ApparelProperties.LastLayer)).GetGetMethod()),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Compat_BBBodySupport), nameof(IsHeadwear))),
                new CodeInstruction(OpCodes.Brtrue)
            };

            foreach (var code in instructions)
            {
                yield return code;
                if (!patched)
                {
                    if (code.opcode == OpCodes.Ldsfld)
                    {
                        ready = true;
                    }
                    if (ready && code.opcode == OpCodes.Beq_S)
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
    }
}
