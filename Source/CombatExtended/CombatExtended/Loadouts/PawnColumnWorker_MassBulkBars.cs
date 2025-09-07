using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended;
// Mostly based on PawnColumnWorker_Loadout and adapted for mass/bulk.
public class PawnColumnWorker_MassBulkBar : PawnColumnWorker
{
    private const int TopAreaHeight = 65;
    private const string BarMass = "CEMass";
    private const string BarBulk = "CEBulk";
    private static int _MinWidth = 40;
    private static int _OptimalWidth = 50;

    public override void DoHeader(Rect rect, PawnTable table)
    {
        base.DoHeader(rect, table);
    }

    public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
    {
        CompInventory inventory = pawn.TryGetComp<CompInventory>();
        if (inventory == null)
        {
            return;
        }
        Rect barRect = new Rect(rect.x, rect.y + 2f, rect.width, rect.height - 4f);
        if (def.defName.Equals(BarBulk))
        {
            Utility_Loadouts.DrawBar(barRect, inventory.currentBulk, inventory.capacityBulk, "", pawn.GetBulkTip());
        }
        if (def.defName.Equals(BarMass))
        {
            Utility_Loadouts.DrawBar(barRect, inventory.currentWeight, inventory.capacityWeight, "", pawn.GetWeightTip());
        }
    }

    public override int GetMinWidth(PawnTable table)
    {
        return Mathf.Max(base.GetMinWidth(table), Mathf.CeilToInt(_MinWidth));
    }

    public override int GetOptimalWidth(PawnTable table)
    {
        return Mathf.Clamp(Mathf.CeilToInt(_OptimalWidth), this.GetMinWidth(table), this.GetMaxWidth(table));
    }

    public override int GetMinHeaderHeight(PawnTable table)
    {
        return Mathf.Max(base.GetMinHeaderHeight(table), TopAreaHeight);
    }

    public override int Compare(Pawn a, Pawn b)
    {
        CompInventory inventoryA = a.TryGetComp<CompInventory>();
        CompInventory inventoryB = b.TryGetComp<CompInventory>();
        if (inventoryA == null || inventoryB == null)
        {
            return 0;
        }

        if (def.defName.Equals(BarBulk))
        {
            return (inventoryA.capacityBulk - inventoryA.currentBulk).CompareTo(inventoryB.capacityBulk - inventoryB.currentBulk);
        }
        if (def.defName.Equals(BarMass))
        {
            return (inventoryA.capacityWeight - inventoryA.currentWeight).CompareTo(inventoryB.capacityWeight - inventoryB.currentWeight);
        }
        return 0;
    }
}
