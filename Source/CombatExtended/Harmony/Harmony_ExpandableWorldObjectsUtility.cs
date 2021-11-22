using System;
using RimWorld.Planet;
using HarmonyLib;
using Verse;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using System.Reflection;
using System.Reflection.Emit;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_ExpandableWorldObjectsUtility
    {
        [HarmonyPatch(typeof(ExpandableWorldObjectsUtility), nameof(ExpandableWorldObjectsUtility.ExpandableWorldObjectsOnGUI))]
        public static class Harmony_ExpandableWorldObjectsOnGUI
        {
			private static List<WorldObject> tmpWorldObjects = new List<WorldObject>();

			private static bool skip = true;			
			private static bool showExpandingIcons;
			private static float transitionPct;

            private static MethodBase allWorldObjectGetter = AccessTools.PropertyGetter(typeof(WorldObjectsHolder), nameof(WorldObjectsHolder.AllWorldObjects));
            private static MethodBase getAllWorldObjectCE = AccessTools.Method(typeof(Harmony_ExpandableWorldObjectsOnGUI), nameof(Harmony_ExpandableWorldObjectsOnGUI.GetAllWorldObjectCE));

            public static List<WorldObject> GetAllWorldObjectCE(List<WorldObject> worldObjects)
            {
                if (!skip)
                {
                    return worldObjects.Where(o => o is TravelingShell).ToList();
                }
                return worldObjects;
            }

            [HarmonyPrefix]
            public static void Prefix()
            {
                skip = true;
                transitionPct = ExpandableWorldObjectsUtility.transitionPct;
				showExpandingIcons = Find.PlaySettings.showExpandingIcons;
				if (ExpandableWorldObjectsUtility.TransitionPct == 0)
                {
                    skip = false;
                    ExpandableWorldObjectsUtility.transitionPct = 1.0f;
                    Find.PlaySettings.showExpandingIcons = true;
                }               
            }           

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                var finished = false;
                for(int i = 0; i < codes.Count;i++)
                {                    
                    if (!finished)
                    {
                        if (codes[i].opcode == OpCodes.Callvirt && codes[i].OperandIs(allWorldObjectGetter))
                        {
                            Log.Message("patched");
                            finished = true;
                            yield return codes[i];
                            yield return new CodeInstruction(OpCodes.Call, getAllWorldObjectCE);
                            continue;
                        }
                    }
                    yield return codes[i];
                }
            }

            [HarmonyPostfix]
            public static void Postfix()
            {                
                ExpandableWorldObjectsUtility.transitionPct = transitionPct;
                Find.PlaySettings.showExpandingIcons = showExpandingIcons;                
            }
        }
    }
}

