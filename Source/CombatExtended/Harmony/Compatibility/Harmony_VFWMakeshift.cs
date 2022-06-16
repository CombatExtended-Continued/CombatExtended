using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using CombatExtended.Compatibility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace CombatExtended.HarmonyCE.Compatibility
{
    public static class Harmony_VanillaWeaponsExpandedMakeshift
    {
        [HarmonyPatch]
        public class Harmony_Patch_PawnVerbGizmoUtility
        {
            private const string targetType = "MVCF.Utilities.PawnVerbGizmoUtility";
            private const string targetMethod = "GetGizmosForVerb";

            private static MethodBase _target;

            public static bool Prepare()
            {
                return (_target = AccessTools.Method($"{targetType}:{targetMethod}")) != null;
            }

            public static MethodBase TargetMethod()
            {
                return _target;
            }

	    public static void Postfix(ref IEnumerable<Gizmo> __result, Verb verb)
            {
		if (verb is Verb_ShootCEKEK)
		{
		    __result = new List<Gizmo>();
		}
            }
        }
    }
}
