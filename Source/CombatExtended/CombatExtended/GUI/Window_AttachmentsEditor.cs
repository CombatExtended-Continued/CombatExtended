using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using CombatExtended.RocketGUI;
using GUIUtility = CombatExtended.RocketGUI.GUIUtility;
using RimWorld;
using Verse.AI;

namespace CombatExtended
{
    public class Window_AttachmentsEditor : IWindow_AttachmentsEditor
    {
        private const int PANEL_RIGHT_WIDTH = 250;
        private const int PANEL_ACTION_HEIGHT = 50;
        private const int PANEL_INNER_MARGINS = 4;

        struct SelectionHolder
        {
            public AttachmentDef attachment;
        }

        private AttachmentDef hoveringOver = null;

        private readonly List<string> categories;
        
        private readonly HashSet<AttachmentLink> selected = new HashSet<AttachmentLink>();

        private readonly Listing_Collapsible collapsible = new Listing_Collapsible(true, true);

        private readonly Dictionary<string, List<AttachmentDef>> categoryToDef = new Dictionary<string, List<AttachmentDef>>();

        private readonly Dictionary<string, SelectionHolder> catergoryToSelected = new Dictionary<string, SelectionHolder>();

        private readonly AttachmentDef[] availableDefs;

        public Window_AttachmentsEditor(WeaponPlatform platform, Map map) : base(platform, map)
        {
            // sort categories from all weapons
            categories = weaponDef.attachmentLinks.SelectMany(t => t.attachment.slotTags).Distinct().ToList();
            categories.Sort();

            // save available stuff
            availableDefs = platform.Platform.attachmentLinks.Select(a => a.attachment).ToArray();

            // register currently equiped attachments
            foreach (AttachmentDef attachmentDef in platform.TargetConfig)
                this.Add(attachmentDef);

            // cache stuff
            foreach (string s in categories)
                categoryToDef[s] = availableDefs.Where(t => t.slotTags.First() == s)?.ToList() ?? new List<AttachmentDef>();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            RebuildCache();
        }        

        protected override void DoContent(Rect inRect)
        {
            if (weapon?.Destroyed ?? true)
            {
                Close(doCloseSound: true);
                return;
            }
            Rect leftRect = inRect;
            leftRect.xMax -= PANEL_RIGHT_WIDTH - 5;
            Rect rightRect = inRect.RightPartPixels(PANEL_RIGHT_WIDTH);
            Rect actionRect = rightRect.BottomPartPixels(PANEL_ACTION_HEIGHT);
            rightRect.yMax -= PANEL_ACTION_HEIGHT - 5;

            rightRect.xMin += PANEL_INNER_MARGINS;
            rightRect.yMin += PANEL_INNER_MARGINS;
            rightRect.xMax -= PANEL_INNER_MARGINS;
            rightRect.yMax -= PANEL_INNER_MARGINS;
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                this.DoRightPanel(rightRect);
            });

            leftRect.xMin += PANEL_INNER_MARGINS;
            leftRect.yMin += PANEL_INNER_MARGINS;
            leftRect.xMax -= PANEL_INNER_MARGINS;
            leftRect.yMax -= PANEL_INNER_MARGINS;
            Widgets.DrawMenuSection(leftRect);
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                this.DoLeftPanel(leftRect);
            });

            actionRect.xMin += PANEL_INNER_MARGINS;
            actionRect.yMin += PANEL_INNER_MARGINS;
            actionRect.xMax -= PANEL_INNER_MARGINS;
            actionRect.yMax -= PANEL_INNER_MARGINS;
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                this.DoActionPanel(actionRect);
            });
        }

        private void DoLeftPanel(Rect inRect)
        {
            inRect.yMin += 6;
            Rect statRect = inRect.BottomPartPixels(25 * 6 + 10);
            inRect.yMax -= 25 * 6 + 10 + 5;
            inRect.width = Mathf.Min(inRect.width, inRect.height);
            inRect = inRect.CenteredOnXIn(statRect);
            Widgets.DrawBoxSolid(inRect, Widgets.MenuSectionBGBorderColor);
            Widgets.DrawBoxSolid(inRect.ContractedBy(1), new Color(0.2f, 0.2f, 0.2f));
            GUIUtility.DrawWeaponWithAttachments(inRect, weaponDef, selected, hoveringOver, 0.7f);
            if (Prefs.DevMode && Widgets.ButtonText(inRect.TopPartPixels(20).LeftPartPixels(75), "edit offsets") && !Find.WindowStack.IsOpen<Window_AttachmentsDebugger>())
                Find.WindowStack.Add(new Window_AttachmentsDebugger(weaponDef));
            Widgets.DrawBoxSolid(statRect.TopPartPixels(1), Widgets.MenuSectionBGBorderColor);
            DoStatPanel(statRect);
        }

        private void DoRightPanel(Rect inRect)
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
                collapsible.Label($"CE_AttachmentSlot_{cat}".Translate(), fontSize: GameFont.Small, fontStyle: FontStyle.Bold);
                collapsible.Gap(3);

                foreach (AttachmentDef attachment in categoryToDef[cat])
                {
                    collapsible.Lambda(22, (rect) =>
                    {
                        bool checkOn = attachment.slotTags.All(s => catergoryToSelected[s].attachment == attachment);
                        bool checkOnPrev = checkOn;
                        if (checkOn && !weapon.attachments.Any(a => a.def == attachment))
                            Widgets.ButtonImageFitted(rect.LeftPartPixels(22).ContractedBy(3), TexButton.Plus, Color.green);
                        if (!checkOn && weapon.attachments.Any(a => a.def == attachment))
                            Widgets.ButtonImageFitted(rect.LeftPartPixels(22).ContractedBy(3), TexButton.Minus, Color.red);
                        rect.xMin += 24;
                        Widgets.DefIcon(rect.LeftPartPixels(22).ContractedBy(2), attachment);
                        rect.xMin += 24;
                        GUIUtility.CheckBoxLabeled(rect, attachment.label.CapitalizeFirst(), ref checkOn, texChecked: Widgets.RadioButOnTex, texUnchecked: Widgets.RadioButOffTex, drawHighlightIfMouseover: false, font: GameFont.Small);
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

        private void DoActionPanel(Rect inRect)
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
            inRect.xMin += 5;
            inRect.xMax -= 5;
            DrawLeft(inRect.LeftPartPixels(inRect.width / 2 - 2.5f));
            DrawRight(inRect.RightPartPixels(inRect.width / 2 - 2.5f));

            void DrawLeft(Rect inRect)
            {
                inRect.yMin += 5;
                Rect rect = inRect.TopPartPixels(25);
                CompProperties_AmmoUser ammoProp = weaponDef.GetCompProperties<CompProperties_AmmoUser>();
                if (ammoProp != null)
                    DrawLine(ref rect, "magazine capacity", ammoProp.magazineSize, ammoProp.magazineSize + selected.Sum(s => s.attachment.GetStatValueAbstract(CE_StatDefOf.MagazineCapacity)));

                DoXmlStat(ref rect, CE_StatDefOf.SwayFactor);
                DoXmlStat(ref rect, CE_StatDefOf.ShotSpread);
                DoXmlStat(ref rect, CE_StatDefOf.SightsEfficiency);
                DoXmlStat(ref rect, CE_StatDefOf.ReloadSpeed);
                DoXmlStat(ref rect, StatDefOf.Mass, sum: true);
            }
            void DrawRight(Rect inRect)
            {
                inRect.yMin += 5;
                Rect rect = inRect.TopPartPixels(25);
                CompProperties_AmmoUser ammoProp = weaponDef.GetCompProperties<CompProperties_AmmoUser>();
                if (ammoProp != null)
                    DrawLine(ref rect, "magazine capacity", ammoProp.magazineSize, ammoProp.magazineSize + selected.Sum(s => s.attachment.GetStatValueAbstract(CE_StatDefOf.MagazineCapacity)));

                DoXmlStat(ref rect, CE_StatDefOf.SwayFactor);
                DoXmlStat(ref rect, CE_StatDefOf.ShotSpread);
                DoXmlStat(ref rect, CE_StatDefOf.SightsEfficiency);
                DoXmlStat(ref rect, CE_StatDefOf.ReloadSpeed);
                DoXmlStat(ref rect, StatDefOf.Mass, sum: true);
            }
        }

        private void DoXmlStat(ref Rect rect, StatDef stat, bool sum = false)
        {
            float before = weaponDef.GetStatValueAbstract(stat);
            float after = before;            
            stat.TransformValue(this.weaponDef.attachmentLinks.Where(t => selected.Contains(t)).ToList(), ref after);
            DrawLine(ref rect, stat.label, before, after);
        }

        private void DrawLine(ref Rect inRect, string label, float before, float after)
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
                Widgets.Label(rect.RightPartPixels(85), beforeText);
                Widgets.Label(rect.RightPartPixels(50), afterText);
            });
        }

        private void Add(AttachmentDef def)
        {
            selected.RemoveWhere(d => d.attachment.slotTags.Any(t => def.slotTags.Contains(t)));
            selected.Add(weaponDef.attachmentLinks.First(t => t.attachment == def));
            RebuildCache();            
        }

        private void Remove(AttachmentDef def)
        {
            selected.RemoveWhere(d => d.attachment.slotTags.Any(t => def.slotTags.Contains(t)));            
            RebuildCache();
        }

        private void RebuildCache()
        {
            catergoryToSelected.Clear();
            foreach (string s in categories)
                catergoryToSelected[s] = new SelectionHolder() { attachment = null };
            foreach (AttachmentLink link in selected)
            {                
                foreach (string s in link.attachment.slotTags)
                    catergoryToSelected[s] = new SelectionHolder() { attachment = link.attachment };
            }
        }

        private void Apply()
        {
            // Update the current config
            weapon.TargetConfig = selected.Select(l => l.attachment).ToList();
            weapon.UpdateConfiguration();

            if (Prefs.DevMode && DebugSettings.godMode)
            {
                weapon.attachments.ClearAndDestroyContents();
                foreach (AttachmentLink link in selected)                    
                {                    
                    Thing attachment = ThingMaker.MakeThing(link.attachment);
                    weapon.attachments.TryAdd(attachment);
                }
                weapon.UpdateConfiguration();
                return;
            }

            // Try start the update job 
            Job job = weapon.Wielder.thinker.GetMainTreeThinkNode<JobGiver_ModifyWeapon>().TryGiveJob(weapon.Wielder);
            weapon.Wielder.jobs.StartJob(job, JobCondition.InterruptForced);
        }
    }
}
