using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE.Compatibility
{
    public static class Harmony_VanillaExpandedFramework
    {
        [HarmonyPatch]
        public class Harmony_Patch_PawnRenderer
        {
            /*
             * Created at 08/21/2021
             * ---------------------
             */
            private const string targetType = "VFECore.Patch_PawnRenderer";
            private const string targetMethod = "IsShell";

            private static MethodBase _target;

            public static bool Prepare()
            {
                return (_target = AccessTools.Method($"{targetType}:{targetMethod}")) != null;
            }

            public static MethodBase TargetMethod()
            {
                return _target;
            }

            public static void Postfix(ApparelLayerDef def, ref bool __result)
            {
                // If it's invisible skip everything
                if (def == CE_ApparelLayerDefOf.Backpack)
                {
                    __result = false;
                }
                // Enable toggling webbing rendering             
                else if (def == CE_ApparelLayerDefOf.Webbing && !Controller.settings.ShowTacticalVests)
                {
                    __result = false;
                }
                // Add our result to that of VEF
                else
                {
                    __result = __result || def.IsVisibleLayer();
                }
            }
        }
    }
}
