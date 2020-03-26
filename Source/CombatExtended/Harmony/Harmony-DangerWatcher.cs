using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Reflection;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch]
    class Harmony_DangerWatcher
    {
        const string className = "<>c";
        const string methodName = "<CalculateDangerRating>";

        public static MethodBase TargetMethod()
        {
            var targets = typeof(DangerWatcher).GetNestedTypes(AccessTools.all)
                .Where(x => x.Name.Contains(className));

            if (!targets.Any())
                Log.Error("CombatExtended :: Harmony_DangerWatcher couldn't find part `" + className + "`");

            var method = targets.SelectMany(x => x.GetMethods(AccessTools.all)).FirstOrDefault(x => x.Name.Contains(methodName) && x.ReturnType == typeof(float));

            if (method == null)
                Log.Error("CombatExtended :: Harmony_DangerWatcher couldn't find `" + className + "` sub-class containing `" + methodName + "`");

            return method;
        }

        [HarmonyPostfix]
        public static void PostFix(IAttackTarget t, ref float __result)
        {
            Building_TurretGunCE bce;
            if ((bce = (t as Building_TurretGunCE)) != null && bce.def.building.IsMortar && !bce.IsMannable)
                __result = bce.def.building.combatPower;
        }
    }
}
