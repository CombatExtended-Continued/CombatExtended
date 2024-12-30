using CombatExtended;
using HarmonyLib;
using JetBrains.Annotations;
using Mono.Unix.Native;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using VEE.RegularEvents;
using Verse;
using Verse.AI;
using Verse.Noise;

namespace VEEventCompat
{
    [UsedImplicitly]
    [StaticConstructorOnStartup]
    public class PatchMain
    {
        private static Harmony harmonyInstance;

        internal static Harmony HarmonyInstance
        {
            get
            {
                if (harmonyInstance == null)
                {
                    harmonyInstance = new Harmony("CE_VEEventCompat");
                }
                return harmonyInstance;
            }
        }

        static PatchMain()
        {
            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(WeaponPod), "TryExecuteWorker")]
    public class InsertAmmo
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> transp(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> list = instructions.ToList();
            for (int i = list.Count - 1; i >= 0; --i)
            {
                if (list[i].opcode == OpCodes.Brtrue_S)
                {
                    MethodInfo overrideMethod = typeof(InsertAmmo).GetMethod("InsertMethod", BindingFlags.Static | BindingFlags.Public);

                    list.InsertRange(i + 1, new CodeInstruction[]
                        {
                                new CodeInstruction(OpCodes.Ldloc_S,6),
                                new CodeInstruction(OpCodes.Call, overrideMethod),
                        });
                    break;
                }
            }
            return list;
        }

        public static void InsertMethod(List<Thing> outThings)
        {
            CE_ThingSetMakerUtility.GenerateAmmoForWeapon(outThings, true, true, new IntRange(1, 3));
        }
    }
}
