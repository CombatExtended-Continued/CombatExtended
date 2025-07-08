using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE.Compatibility;
public class Harmony_Compat_VanillaEventExpanded
{
    private static Type WeaponPod_Patch_HarmonyPatches
    {
        get
        {
            return AccessTools.TypeByName("VEE.RegularEvents.WeaponPod");
        }
    }
    [HarmonyPatch]
    public static class Harmony_WeaponPod_Patch
    {
        public static bool Prepare()
        {
            return WeaponPod_Patch_HarmonyPatches != null;
        }

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("VEE.RegularEvents.WeaponPod:TryExecuteWorker");
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var list = instructions.ToList();
            MethodInfo overrideMethod = typeof(Harmony_WeaponPod_Patch).GetMethod("InsertMethod", BindingFlags.Static | BindingFlags.Public);
            bool foundInjection = false;
            for (int i = list.Count - 1; i >= 0; --i)
            {
                if (list[i].opcode == OpCodes.Brtrue_S)
                {
                    list.InsertRange(i + 1, new CodeInstruction[]
                    {
                        new CodeInstruction(OpCodes.Ldloc_S, 6),
                        new CodeInstruction(OpCodes.Call, overrideMethod),
                    });
                    foundInjection = true;
                    break;
                }
            }
            if (!foundInjection)
            {
                Log.Error($"Combat Extended :: Failed to find injection point when applying Patch: {HarmonyBase.GetClassName(MethodBase.GetCurrentMethod()?.DeclaringType)}");
            }
            return list;
        }
        public static void InsertMethod(List<Thing> outThings)
        {
            CE_ThingSetMakerUtility.GenerateAmmoForWeapon(outThings, true, true, new IntRange(1, 3));
        }
    }

}
