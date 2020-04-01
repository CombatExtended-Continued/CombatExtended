using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Reflection;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch]
    class Harmony_PlaceWorker_ShowTurretRadius
    {
        const string className = "<>c";
        const string methodName = "<AllowsPlacing>";

        public static MethodBase TargetMethod()
        {
            var targets = typeof(PlaceWorker_ShowTurretRadius).GetNestedTypes(AccessTools.all)
                .Where(x => x.Name.Contains(className));

            if (!targets.Any())
                Log.Error("CombatExtended :: Harmony_PlaceWorker_ShowTurretRadius couldn't find part `"+ className + "`");

            var method = targets.SelectMany(x => x.GetMethods(AccessTools.all)).FirstOrDefault(x => x.Name.Contains(methodName));

            if (method == null)
                Log.Error("CombatExtended :: Harmony_PlaceWorker_ShowTurretRadius couldn't find `<>c` sub-class containing `"+ methodName + "`");

            return method;
        }
        
        [HarmonyPostfix]
        public static void PostFix(VerbProperties v, ref bool __result)
        {
            __result = __result || v.verbClass == typeof(Verb_ShootCE);
        }
    }
}
