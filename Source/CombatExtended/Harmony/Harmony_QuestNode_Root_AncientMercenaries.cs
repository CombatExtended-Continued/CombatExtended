using System;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE;

// The ancient mercenaries boss gets his unique weapon shoved into his hands after he already went
// through pawn generation, meaning loadout gen never saw it and he carries exactly zero rounds for it.
// Standing there dry-firing at the colonist is not a personality trait, so give him a few mags here.
[HarmonyPatch(typeof(QuestNode_Root_AncientMercenaries), "RunInt")]
internal static class QuestNode_Root_AncientMercenaries_RunInt
{
    private static readonly IntRange magazineCountRange = new IntRange(3, 5);

    internal static void Postfix()
    {
        try
        {
            // Both are pushed onto the slate at the tail end of RunInt, so they're still around during the postfix.
            Pawn pawn = QuestGen.slate.Get<Pawn>("LEADER");
            if (pawn == null || pawn.equipment?.Primary is not ThingWithComps weapon)
            {
                return;
            }
            CompInventory inventory = pawn.TryGetComp<CompInventory>();
            if (inventory == null)
            {
                return;
            }
            pawn.kindDef?.GetModExtension<LoadoutPropertiesExtension>()?.GenerateAmmoFor(weapon, inventory, magazineCountRange.RandomInRange);
        }
        catch (Exception ex)
        {
            Log.Error($"[CE] Failed to give the ancient mercenaries leader ammo for his unique weapon: {ex}");
        }
    }
}
