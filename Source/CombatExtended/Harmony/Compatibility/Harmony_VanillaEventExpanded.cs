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
            MethodInfo targetMethod = AccessTools.Method(typeof(List<Thing>), nameof(List<Thing>.Add));

            bool foundInjection = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].Calls(targetMethod))
                {
                    continue;
                }
                list.InsertRange(i + 1, [
                    new CodeInstruction(OpCodes.Ldloc_2),
                    new CodeInstruction(OpCodes.Call, overrideMethod)
                ]);
                foundInjection = true;
                break;
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
