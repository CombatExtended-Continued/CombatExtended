using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE;

// Boss gets his unique gun after loadout gen already ran, so hand him spare mags here. Read the
// weapon off the slate, not equipment.Primary - the quest nukes his gear and he grabs a sidearm.
[HarmonyPatch(typeof(QuestNode_Root_AncientMercenaries), "RunInt")]
internal static class QuestNode_Root_AncientMercenaries_RunInt
{
    private static readonly IntRange MagazineCount = new IntRange(3, 5);

    internal static void Postfix()
    {
        try
        {
            Pawn pawn = QuestGen.slate.Get<Pawn>("LEADER");
            ThingWithComps weapon = QuestGen.slate.Get<ThingWithComps>("WEAPON") ?? (pawn.equipment?.Primary as ThingWithComps);
            CompAmmoUser compAmmo = weapon?.TryGetComp<CompAmmoUser>();
            CompInventory inventory = pawn?.TryGetComp<CompInventory>();
            if (pawn == null || weapon == null || compAmmo == null || !compAmmo.UseAmmo || inventory == null || pawn.inventory == null)
            {
                return;
            }

            AmmoDef ammo = PickAmmo(compAmmo);
            if (ammo == null)
            {
                return;
            }

            compAmmo.ResetAmmoCount(ammo);

            int unit = Mathf.Max(1, compAmmo.MagSizeOverride > 0 ? compAmmo.MagSizeOverride : compAmmo.MagSize);

            // drop the old weapon's mags (dead weight, eats his carry cap) before handing out new ones
            foreach (Thing dead in inventory.container.Where(t => t.def is AmmoDef && t.def != ammo).ToList())
            {
                inventory.container.Remove(dead);
                if (!dead.Destroyed)
                {
                    dead.Destroy(DestroyMode.Vanish);
                }
            }
            inventory.UpdateInventory();

            Thing mags = ThingMaker.MakeThing(ammo);
            mags.stackCount = MagazineCount.RandomInRange * unit;
            if (inventory.CanFitInInventory(mags, out int maxCount))
            {
                if (maxCount < mags.stackCount)
                {
                    mags.stackCount = maxCount - (maxCount % unit);
                }
                if (mags.stackCount > 0)
                {
                    inventory.container.TryAdd(mags);
                }
            }
            else
            {
                Log.Warning($"[CE] Ancient mercenaries leader: no room for {ammo.defName} mags.");
            }
            inventory.UpdateInventory();
        }
        catch (Exception ex)
        {
            Log.Error($"[CE] Ancient mercenaries leader ammo gen failed: {ex}");
        }
    }

    private static AmmoDef PickAmmo(CompAmmoUser compAmmo)
    {
        if (compAmmo.Props?.ammoSet == null)
        {
            return null;
        }
        var links = compAmmo.Props.ammoSet.ammoTypes;
        var avail = links.Where(a => a.ammo != null && a.ammo.alwaysHaulable && !a.ammo.menuHidden && a.ammo.generateAllowChance > 0f)
                        .Select(a => a.ammo)
                        .ToList();
        if (avail.Count == 0)
        {
            avail = links.Where(a => a.ammo != null).Select(a => a.ammo).ToList();
        }
        return avail.Count > 0 ? avail.RandomElement() : null;
    }
}
