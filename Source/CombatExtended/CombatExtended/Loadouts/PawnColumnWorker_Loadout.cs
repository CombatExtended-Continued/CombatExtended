using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Multiplayer.API;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;

namespace CombatExtended
{
    // Mostly based on PawnColumnWorker_Outfit and adapted for loadouts. Should be kept up-to-date with any style changes in vanilla. -NIA
    public class PawnColumnWorker_Loadout : PawnColumnWorker
    {
        private const int TopAreaHeight = 65;

        private const int ManageOutfitsButtonHeight = 32;

        // Transpiler referenced items should be changed with extreme caution.  Values can be changed but visibility, type, and name should not be.
        #region TranspilerReferencedItems
        internal const float _MinWidth = 158f;  //194f default
        internal const float _OptimalWidth = 188;  //251f default

        internal static float IconSize = 16f;
        // using property format since I don't know what the lambda expression '=>' gets compiled into in this context.
        public static Texture2D EditImage => ContentFinder<Texture2D>.Get("UI/Icons/edit");
        public static Texture2D ClearImage => ContentFinder<Texture2D>.Get("UI/Icons/clear");

        internal static string textGetter(string untranslatedString)
        {
            return "CE_EditX".Translate(untranslatedString.Translate());
        }
        #endregion TranspilerReferencedItems

        private IEnumerable<Widgets.DropdownMenuElement<Loadout>> Button_GenerateMenu(Pawn pawn)
        {
            using (List<Loadout>.Enumerator enu = LoadoutManager.Loadouts.GetEnumerator())
            {
                while (enu.MoveNext())
                {
                    Loadout loadout = enu.Current;
                    yield return new Widgets.DropdownMenuElement<Loadout>
                    {
                        option = new FloatMenuOption(loadout.LabelCap, delegate
                        {
                            SetLoadout(pawn, loadout);
                        }),
                        payload = loadout
                    };
                }
            }
        }

        [SyncMethod]
        private static void SetLoadout(Pawn pawn, Loadout loadout) => pawn.SetLoadout(loadout);

        [SyncMethod]
        private static void HoldTrackerClear(Pawn pawn) => pawn.HoldTrackerClear();

        [SyncMethod]
        private static void UpdateLoadoutNow(Pawn pawn)
        {
            Job job = pawn.thinker?.GetMainTreeThinkNode<JobGiver_UpdateLoadout>()?.TryGiveJob(pawn);
            if (job != null)
                pawn.jobs.StartJob(job, JobCondition.InterruptForced);
        }

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

        /* (ProfoundDarkness) I've intentionally left some code remarked in the following code because it's a useful guide on how to create
         * and maintain the transpilers that will do nearly identical changes to RimWorld's code for the other 2 PawnColumnWorkers.
         */
        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (pawn.outfits == null)
            {
                return;
            }
            //changed: int num = Mathf.FloorToInt((rect.width - 4f) * 0.714285731f);
            int num = Mathf.FloorToInt((rect.width - 4f) - IconSize);
            //changed: int num2 = Mathf.FloorToInt((rect.width - 4f) * 0.2857143f);
            int num2 = Mathf.FloorToInt(IconSize);
            float num3 = rect.x;
            //added:
            float num4 = rect.y + ((rect.height - IconSize) / 2);
            float equipNowWidth = "CE_UpdateLoadoutNow".Translate().GetWidthCached();

            // Reduce width if we're adding a clear forced button
            bool somethingIsForced = pawn.HoldTrackerAnythingHeld();
            Rect loadoutRect = new Rect(num3, rect.y + 2f, (float)num, rect.height - 4f);
            if (somethingIsForced)
            {
                loadoutRect.width -= 4f + (float)num2;
            }

            // Main loadout button
            Rect mainLoadoutButton = pawn.Spawned ? loadoutRect.LeftPartPixels(loadoutRect.width - equipNowWidth - 16) : loadoutRect;
            mainLoadoutButton.xMax -= 2;
            string label = pawn.GetLoadout().label.Truncate(mainLoadoutButton.width, null);
            Widgets.Dropdown<Pawn, Loadout>(mainLoadoutButton, pawn, (Pawn p) => p.GetLoadout(), new Func<Pawn, IEnumerable<Widgets.DropdownMenuElement<Loadout>>>(Button_GenerateMenu), label, null, null, null, null, true);

            if (pawn.Spawned)
            {
                Rect forceEquipNow = loadoutRect.RightPartPixels(equipNowWidth + 12);
                if (Widgets.ButtonText(forceEquipNow, "CE_UpdateLoadoutNow".Translate()))
                {
                    UpdateLoadoutNow(pawn);
                }
            }

            // Clear forced button
            num3 += loadoutRect.width;
            num3 += 4f;
            //changed: Rect forcedHoldRect = new Rect(num3, rect.y + 2f, (float)num2, rect.height - 4f);
            Rect forcedHoldRect = new Rect(num3, num4, (float)num2, (float)num2);
            if (somethingIsForced)
            {
                if (Widgets.ButtonImage(forcedHoldRect, ClearImage))
                {
                    HoldTrackerClear(pawn); // yes this will also delete records that haven't been picked up and thus not shown to the player...
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

            //changed: Rect assignTabRect = new Rect(num3, rect.y + 2f, (float)num2, rect.height - 4f);
            Rect assignTabRect = new Rect(num3, num4, (float)num2, (float)num2);
            //changed: if (Widgets.ButtonText(assignTabRect, "AssignTabEdit".Translate(), true, false, true))
            if (Widgets.ButtonImage(assignTabRect, EditImage))
            {
                Find.WindowStack.Add(new Dialog_ManageLoadouts(pawn.GetLoadout()));
            }
            // Added this next line.
            TooltipHandler.TipRegion(assignTabRect, new TipSignal(textGetter("CE_Loadouts"), pawn.GetHashCode() * 613));
            num3 += (float)num2;
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
            return a.GetLoadoutId().CompareTo(b.GetLoadoutId());
        }
    }
}
