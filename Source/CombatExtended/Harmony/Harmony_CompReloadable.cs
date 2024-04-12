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
    [HarmonyPatch(typeof(CompApparelVerbOwner_Charged), nameof(CompApparelVerbOwner_Charged.CompGetWornGizmosExtra))]
    public static class Harmony_CompApparelVerbOwner_Charged_CompygetWornGizmosExtra
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, CompApparelReloadable __instance)
        {
            foreach (Gizmo g in __result)
            {
                yield return g;
            }

            // Don't show the "reload worn armor" gizmo for non-colonist pawns / pawns in a mental break etc.
            if (__instance.Wearer.IsColonistPlayerControlled)
            {

                yield return new Command_ReloadArmor
                {
                    compReloadable = __instance,
                    action = () => TryReloadArmor(__instance),
                    defaultLabel = (string)"CE_ReloadLabel".Translate() + " worn armor",
                    defaultDesc = "CE_ReloadDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/Reload", true)

                };
            }

        }

        [global::CombatExtended.Compatibility.Multiplayer.SyncMethod]
        private static void TryReloadArmor(CompApparelReloadable __instance)
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
