using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using CombatExtended.RocketGUI;
using GUIUtility = CombatExtended.RocketGUI.GUIUtility;
using RimWorld;

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
            Rect statRect = inRect.BottomPartPixels(25 * 6 + 10);
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                Text.Font = GameFont.Small;
                Text.CurFontStyle.fontStyle = FontStyle.Bold;
                Widgets.Label(inRect.TopPartPixels(25), $"{weaponDef.label.CapitalizeFirst()}");
            });
            inRect.yMin += 25;
            inRect.yMax -= 25 * 6 + 10 + 5;
            inRect.width = Mathf.Min(inRect.width, inRect.height);
            inRect = inRect.CenteredOnXIn(statRect);
            Widgets.DrawBoxSolid(inRect, Widgets.MenuSectionBGBorderColor);
            Widgets.DrawBoxSolid(inRect.ContractedBy(1), new Color(0.2f, 0.2f, 0.2f));
            GUIUtility.DrawWeaponWithAttachments(inRect, weaponDef, selected, hoveringOver, 0.7f);
            Widgets.DrawBoxSolid(statRect.TopPartPixels(1), Widgets.MenuSectionBGBorderColor);
            DoStatPanel(statRect);
        }

        protected override void DoRightPanel(Rect inRect)
        {
            if (!Mouse.IsOver(inRect))
                hoveringOver = null;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;

            collapsible.Expanded = true;
            collapsible.Begin(inRect, "CE_Attachments_Selection".Translate(), false, false, false);
            int i = 0;
            foreach (string cat in categories)
            {
                if (i++ > 0)
                {
                    collapsible.Gap(1.00f);
                    collapsible.Line(1.0f);
                }
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
            }
            collapsible.End(ref inRect);
            inRect.yMin -= 1;
            Widgets.DrawMenuSection(inRect);
            if (Mouse.IsOver(inRect))
                hoveringOver = null;
        }

        protected override void DoActionPanel(Rect inRect)
        {
            Text.Font = GameFont.Small;
            Rect closeRect = inRect.RightHalf();
            closeRect.xMin += 1.5f;
            Rect applyRect = inRect.LeftHalf();
            applyRect.xMax -= 1.5f;
            if (Widgets.ButtonText(closeRect, "CE_Close".Translate()))
                Close();
            if (Widgets.ButtonText(applyRect, "CE_Apply".Translate()))
            {
                Apply();
                Close();
            }
        }

        private void DoStatPanel(Rect inRect)
        {
            inRect.yMin += 5;
            Rect rect = inRect.TopPartPixels(25);
            CompProperties_AmmoUser ammoProp = weaponDef.GetCompProperties<CompProperties_AmmoUser>();
            if (ammoProp != null)
                DrawLine(ref rect, "magazine capacity", ammoProp.magazineSize, ammoProp.magazineSize + selected.Sum(s => s.GetStatValueAbstract(CE_StatDefOf.MagazineCapacity)));

            DoXmlStat(ref rect, CE_StatDefOf.SwayFactor);
            DoXmlStat(ref rect, CE_StatDefOf.ShotSpread);
            DoXmlStat(ref rect, CE_StatDefOf.SightsEfficiency);
            DoXmlStat(ref rect, CE_StatDefOf.ReloadSpeed);
            DoXmlStat(ref rect, StatDefOf.Mass, sum: true);

            void DoXmlStat(ref Rect rect, StatDef stat, bool sum = false)
            {
                float before = weaponDef.GetStatValueAbstract(stat);
                float after = before;
                foreach (AttachmentDef attachment in selected)
                {
                    float m = attachment.GetStatValueAbstract(stat);
                    if (m != 0)
                    {
                        if (!sum)
                            after *= m;
                        else
                            after += m;
                    }
                }
                DrawLine(ref rect, stat.label, before, after);
            }

            void DrawLine(ref Rect inRect, string label, float before, float after)
            {
                string color = before > after ? "red" : "green";
                string sign = before > after ? "-" : "+";
                string beforeText = $"{String.Format("{0:n}", Math.Round(before, 2))}";
                string afterText = $"<color={color}>({sign}{String.Format("{0:n}", Math.Round(Mathf.Abs(after - before), 2))})</color>";
                Rect rect = inRect;
                inRect.y += 25;
                GUIUtility.ExecuteSafeGUIAction(() =>
                {
                    Text.Font = GameFont.Small;
                    Text.WordWrap = false;
                    Text.Anchor = TextAnchor.UpperLeft;
                    rect.xMin += 15;
                    rect.xMax -= 15;
                    Widgets.Label(rect, label.CapitalizeFirst());
                    Widgets.Label(rect.RightPartPixels(95), beforeText);
                    Widgets.Label(rect.RightPartPixels(50), afterText);
                });
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
