using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Pawn_ApparelTracker), "Notify_ApparelAdded")]
    internal static class Harmony_ApparelTracker_Notify_ApparelAdded
    {
        internal static void Postfix(Pawn_ApparelTracker __instance, Apparel apparel)
        {
            var isRadioPack = apparel.def.GetModExtension<ApparelDefExtension>()?.isRadioPack ?? false;

            if (isRadioPack && (__instance.pawn.equipment?.equipment?.Any ?? false))
            {
                foreach(Verb verb in __instance.pawn.equipment.AllEquipmentVerbs)
                {
                    if (verb is Verb_MarkForArtillery marker)
                        marker.Dirty();
                }
            }

            var hediffDef = apparel.def.GetModExtension<ApparelHediffExtension>()?.hediff;
            if (hediffDef == null)
                return;

            var pawn = __instance.pawn;
            pawn.health.AddHediff(hediffDef);
        }
    }

    [HarmonyPatch(typeof(Pawn_ApparelTracker), "Notify_ApparelRemoved")]
    internal static class Harmony_ApparelTracker_Notify_ApparelRemoved
    {
        internal static void Postfix(Pawn_ApparelTracker __instance, Apparel apparel)
        {
            var isRadioPack = apparel.def.GetModExtension<ApparelDefExtension>()?.isRadioPack ?? false;
            if (isRadioPack && (__instance.pawn.equipment?.equipment?.Any ?? false))
            {
                foreach (Verb verb in __instance.pawn.equipment.AllEquipmentVerbs)
                {
                    if (verb is Verb_MarkForArtillery marker)
                        marker.Dirty();
                }
            }

            var hediffDef = apparel.def.GetModExtension<ApparelHediffExtension>()?.hediff;
            if (hediffDef == null)
                return;

            var pawn = __instance.pawn;
            var hediff = pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.def == hediffDef);
            if (hediff == null)
            {
                Log.Warning($"Combat Extended :: Apparel {apparel} tried removing hediff {hediffDef} from {pawn} but could not find any");
                return;
            }
            pawn.health.RemoveHediff(hediff);
        }
    }
}