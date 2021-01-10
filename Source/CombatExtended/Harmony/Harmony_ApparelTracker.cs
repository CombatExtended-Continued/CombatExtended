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

    [HarmonyPatch(typeof(Pawn_ApparelTracker), "Wear")]
    internal static class Harmony_ApparelTracker_Wear
    {
        internal static void Postfix(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            Pawn owner = __instance.pawn;
            //We are equipping a shield and our current primary weapon is not a valid one-handed.
            if (newApparel is Apparel_Shield && !(owner.equipment?.Primary?.def?.weaponTags?.Contains("CE_OneHandedWeapon") ?? false))
            {
                CompInventory compInventory = owner.TryGetComp<CompInventory>();
                //Find the first thing that is one-handed
                ThingWithComps eq = owner.inventory.innerContainer.FirstOrDefault(s => s.def.weaponTags?.Contains("CE_OneHandedWeapon") ?? false) as ThingWithComps;
                if (eq != null && eq.TryGetComp<CompEquippable>() != null)
                {
                    //equip it
                    compInventory.TrySwitchToWeapon(eq);
                }
            }
        }
    }
}