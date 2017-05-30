using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    // Mostly based on PawnColumnWorker_Outfit and adapted for loadouts. Should be kept up-to-date with any style changes in vanilla. -NIA
    public class PawnColumnWorker_Loadout : PawnColumnWorker
    {
        private const int TopAreaHeight = 65;

        private const int ManageOutfitsButtonHeight = 32;

        public override void DoHeader(Rect rect, PawnTable table)
        {
            base.DoHeader(rect, table);
            Rect rect2 = new Rect(rect.x, rect.y + (rect.height - TopAreaHeight), Mathf.Min(rect.width, 360f), ManageOutfitsButtonHeight);
            if (Widgets.ButtonText(rect2, "CE_ManageLoadouts".Translate(), true, false, true))
            {
                Find.WindowStack.Add(new Dialog_ManageLoadouts(null));
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_Loadouts, KnowledgeAmount.Total);
            }
            UIHighlighter.HighlightOpportunity(rect2, "CE_ManageLoadouts");
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (pawn.outfits == null)
            {
                return;
            }
            int num = Mathf.FloorToInt((rect.width - 4f) * 0.714285731f);
            int num2 = Mathf.FloorToInt((rect.width - 4f) * 0.2857143f);
            float num3 = rect.x;

            // Reduce width if we're adding a clear forced button
            bool somethingIsForced = pawn.HoldTrackerAnythingHeld();
            Rect loadoutButtonRect = new Rect(num3, rect.y + 2f, (float)num, rect.height - 4f);
            if (somethingIsForced)
            {
                loadoutButtonRect.width -= 4f + (float)num2;
            }

            // Main loadout button
            string label = pawn.GetLoadout().label.Truncate(loadoutButtonRect.width, null);
            if (Widgets.ButtonText(loadoutButtonRect, label, true, false, true))
            {
                LoadoutManager.SortLoadouts();
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (Loadout loadout in LoadoutManager.Loadouts)
                {
                    // need to create a local copy for delegate
                    Loadout localLoadout = loadout;
                    options.Add(new FloatMenuOption(localLoadout.LabelCap, delegate
                    {
                        pawn.SetLoadout(localLoadout);
                    }, MenuOptionPriority.Default, null, null));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // Clear forced button
            num3 += loadoutButtonRect.width;
            num3 += 4f;
            Rect forcedHoldRect = new Rect(num3, rect.y + 2f, (float)num2, rect.height - 4f);
            if (somethingIsForced)
            {
                if (Widgets.ButtonText(forcedHoldRect, "ClearForcedApparel".Translate(), true, false, true)) // "Clear forced" is sufficient and that's what this is at the moment.
                {
                    pawn.HoldTrackerClear(); // yes this will also delete records that haven't been picked up and thus not shown to the player...
                }
                TooltipHandler.TipRegion(forcedHoldRect, new TipSignal(delegate
                {
                    string text = "CE_ForcedHold".Translate() + ":\n";
                    foreach (HoldRecord rec in LoadoutManager.GetHoldRecords(pawn))
                    {
                        if (!rec.pickedUp) continue;
                        text = text + "\n   " + rec.thingDef.LabelCap + " x" + rec.count;
                    }
                    return text;
                }, pawn.GetHashCode() * 613));
                num3 += (float)num2;
                num3 += 4f;
            }

            // Edit loadout button
            Rect assignTabRect = new Rect(num3, rect.y + 2f, (float)num2, rect.height - 4f);
            if (Widgets.ButtonText(assignTabRect, "AssignTabEdit".Translate(), true, false, true))
            {
                Find.WindowStack.Add(new Dialog_ManageLoadouts(pawn.GetLoadout()));
            }
            num3 += (float)num2;
        }

        public override int GetMinWidth(PawnTable table)
        {
            return Mathf.Max(base.GetMinWidth(table), Mathf.CeilToInt(194f));
        }

        public override int GetOptimalWidth(PawnTable table)
        {
            return Mathf.Clamp(Mathf.CeilToInt(354f), this.GetMinWidth(table), this.GetMaxWidth(table));
        }

        public override int GetMinHeaderHeight(PawnTable table)
        {
            return Mathf.Max(base.GetMinHeaderHeight(table), TopAreaHeight);
        }

        public override int Compare(Pawn a, Pawn b)
        {
            return a.GetLoadoutId().CompareTo(b.GetLoadoutId());
        }
    }
}
