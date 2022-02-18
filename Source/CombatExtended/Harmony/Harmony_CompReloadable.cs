using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse.AI;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(CompReloadable), "CompGetWornGizmosExtra")]
    public static class Harmony_CompReloadable_CompygetWornGizmosExtra
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, CompReloadable __instance)
        {
            foreach (Gizmo g in __result)
            {
                yield return g;
            }

            yield return new Command_ReloadArmor
            {
                compReloadable = __instance,
                action = () => TryReloadArmor(__instance),
                defaultLabel = (string)"CE_ReloadLabel".Translate() + " worn armor",
                defaultDesc = "CE_ReloadDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/Reload", true)

            };

        }

        [global::CombatExtended.Compatibility.Multiplayer.SyncMethod]
        private static void TryReloadArmor(CompReloadable __instance)
        {
            ThingWithComps gear = __instance.parent;
            Pawn wearer = __instance.Wearer;
            Job j = null;
            bool gotJob = Harmony_JobGiver_Reload_TryGiveJob.Prefix(wearer, ref j);
            if (j != null)
            {
                wearer.jobs.StartJob(j, JobCondition.InterruptForced, null, wearer.CurJob?.def != j.def, true);
            }
        }
    }
}
