using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using CombatExtended.RocketGUI;
using GUIUtility = CombatExtended.RocketGUI.GUIUtility;

namespace CombatExtended
{
    public class Window_AttachmentsEditor : IWindow_AttachmentsEditor
    {
        struct SelectionHolder
        {
            public AttachmentDef attachment;
        }

        private AttachmentDef hoveringOver = null;

        private readonly List<string> categories;

        private readonly HashSet<AttachmentDef> selected = new HashSet<AttachmentDef>();

        private readonly Listing_Collapsible collapsible = new Listing_Collapsible(true, true);

        private readonly Dictionary<string, List<AttachmentDef>> categoryToDef = new Dictionary<string, List<AttachmentDef>>();

        private readonly Dictionary<string, SelectionHolder> catergoryToSelected = new Dictionary<string, SelectionHolder>();

        private readonly AttachmentDef[] availableDefs;

        public Window_AttachmentsEditor(WeaponPlatform platform, Map map) : base(platform, map)
        {
            // sort categories from all weapons
            categories = weaponDef.attachments.SelectMany(t => t.attachment.slotTags).Distinct().ToList();
            categories.Sort();

            // save available stuff
            availableDefs = platform.AvailableAttachmentDefs;

            // register currently equiped attachments
            foreach (Thing attachment in platform.attachments)
                this.Add((AttachmentDef)attachment.def);

            // cache stuff
            foreach (string s in categories)
                categoryToDef[s] = availableDefs.Where(t => t.slotTags.First() == s)?.ToList() ?? new List<AttachmentDef>();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            RebuildCache();
        }

        protected override void DoLeftPanel(Rect inRect)
        {
            Rect statRect = inRect.BottomPartPixels(195);
            inRect.yMin += 5;
            inRect.yMax -= 200;
            inRect.width = Mathf.Min(inRect.width, inRect.height);
            inRect = inRect.CenteredOnXIn(statRect);
            Widgets.DrawBoxSolid(inRect, Widgets.MenuSectionBGBorderColor);
            Widgets.DrawBoxSolid(inRect.ContractedBy(1), new Color(0.2f, 0.2f, 0.2f));
            GUIUtility.DrawWeaponWithAttachments(inRect, weaponDef, selected, hoveringOver, 0.7f);
            Widgets.DrawBoxSolid(statRect.TopPartPixels(1), Widgets.MenuSectionBGBorderColor);
        }

        protected override void DoRightPanel(Rect inRect)
        {
            if (!Mouse.IsOver(inRect))
                hoveringOver = null;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;

            collapsible.Expanded = true;
            collapsible.Begin(inRect, "CE_Attachments_Selection".Translate(), false, false, false);
            foreach (string cat in categories)
            {
                collapsible.Label($"CE_AttachmentSlot_{cat}".Translate(), fontSize: GameFont.Tiny, fontStyle: FontStyle.Bold);
                collapsible.Gap(3);
                foreach (AttachmentDef attachment in categoryToDef[cat])
                {
                    collapsible.Lambda(22, (rect) =>
                    {
                        bool checkOn = attachment.slotTags.All(s => catergoryToSelected[s].attachment == attachment);
                        bool checkOnPrev = checkOn;
                        GUIUtility.CheckBoxLabeled(rect, attachment.label.CapitalizeFirst(), ref checkOn, texChecked: Widgets.RadioButOnTex, texUnchecked: Widgets.RadioButOffTex, drawHighlightIfMouseover: false);
                        if (checkOn != checkOnPrev)
                        {
                            if (checkOn)
                                Add(attachment);
                            else
                                Remove(attachment);
                        }
                        if (Mouse.IsOver(rect))
                        {
                            hoveringOver = attachment;
                            TooltipHandler.TipRegion(rect, attachment.description.CapitalizeFirst());
                        }
                    }, useMargins: true, hightlightIfMouseOver: true);
                }
                collapsible.Gap(1.00f);
                collapsible.Line(1.0f);
            }
            collapsible.End(ref inRect);
            if (Mouse.IsOver(inRect))
                hoveringOver = null;
        }

        protected override void DoActionPanel(Rect inRect)
        {
            Text.Font = GameFont.Small;
            Rect closeRect = inRect.RightHalf();
            closeRect.xMin += 2.5f;
            Rect applyRect = inRect.LeftHalf();
            applyRect.xMax -= 2.5f;
            if (Widgets.ButtonText(closeRect, "CE_Close".Translate()))
                Close();
            if (Widgets.ButtonText(applyRect, "CE_Apply".Translate()))
            {
                Apply();
                Close();
            }
        }

        private void Add(AttachmentDef def)
        {
            selected.RemoveWhere(d => d.slotTags.Any(t => def.slotTags.Contains(t)));
            selected.Add(def);
            RebuildCache();
        }

        private void Remove(AttachmentDef def)
        {
            selected.RemoveWhere(d => d.slotTags.Any(t => def.slotTags.Contains(t)));
            RebuildCache();
        }

        private void RebuildCache()
        {
            catergoryToSelected.Clear();
            foreach (string s in categories)
                catergoryToSelected[s] = new SelectionHolder() { attachment = null };
            foreach (AttachmentDef def in selected)
            {
                foreach (string s in def.slotTags)
                    catergoryToSelected[s] = new SelectionHolder() { attachment = def };
            }
        }

        private void Apply()
        {
        }
    }
}
