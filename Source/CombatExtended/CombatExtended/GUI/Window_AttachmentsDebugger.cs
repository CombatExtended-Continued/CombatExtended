using System;
using System.Collections.Generic;
using System.Linq;
using CombatExtended.RocketGUI;
using UnityEngine;
using Verse;
using GUIUtility = CombatExtended.RocketGUI.GUIUtility;

namespace CombatExtended
{
    public class Window_AttachmentsDebugger : Window
    {
        private const int PANEL_RIGHT_WIDTH = 400;

        public readonly WeaponPlatformDef weaponDef;

        private List<AttachmentLink> links = null;

        private Dictionary<AttachmentLink, bool> hidden = new Dictionary<AttachmentLink, bool>();

        private Dictionary<AttachmentLink, bool> fake = new Dictionary<AttachmentLink, bool>();

        private readonly Listing_Collapsible collapsible = new Listing_Collapsible(true, true);

        public override Vector2 InitialSize => new Vector2(1000, 800);

        private string searchText = "";

        public Window_AttachmentsDebugger(WeaponPlatformDef weaponDef)
        {
            this.links = weaponDef.attachmentLinks.ToList();
            this.layer = WindowLayer.Super;            
            this.resizer = new WindowResizer();
            this.forcePause = true;            
            this.doCloseButton = false;
            this.doCloseX = false;           
            this.weaponDef = (WeaponPlatformDef)weaponDef;
            foreach (AttachmentLink link in links)
            {
                this.hidden.Add(link, true);
                this.fake.Add(link, false);
            }
            foreach (AttachmentDef def in DefDatabase<AttachmentDef>.AllDefs)
            {
                if (links.Any(l => l.attachment == def))
                    continue;
                AttachmentLink link = new AttachmentLink();
                link.attachment = def;
                link.PrepareTexture(weaponDef);
                links.Add(link);
                this.fake.Add(link, true);
                this.hidden.Add(link, true);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Exception error = null;
            try
            {
                GUIUtility.StashGUIState();
                DoContent(inRect);
            }
            catch (Exception er)
            {
                error = er;
            }
            finally
            {
                GUIUtility.RestoreGUIState();
                if (error != null)
                    throw error;
            }
        }

        public override void Close(bool doCloseSound = true)
        {
            base.Close(doCloseSound);
            this.weaponDef.attachmentLinks = links.Where(l => weaponDef.attachmentLinks.Contains(l)).ToList();
            foreach (AttachmentLink link in this.weaponDef.attachmentLinks)
                link.PrepareTexture(this.weaponDef);
        }

        private int _counter = 0;

        public override void WindowOnGUI()
        {
            base.WindowOnGUI();
            if (_counter++ % 60 == 0)
            {
                this.weaponDef.attachmentLinks = links.Where(l => weaponDef.attachmentLinks.Contains(l)).ToList();
                foreach (AttachmentLink link in this.weaponDef.attachmentLinks)
                    link.PrepareTexture(this.weaponDef);
            }
        }

        private void DoContent(Rect inRect)
        {
            Rect right = inRect.RightPartPixels(PANEL_RIGHT_WIDTH).ContractedBy(2);
            DoRightPanel(right);

            Rect left = inRect.LeftPartPixels(inRect.width - PANEL_RIGHT_WIDTH).ContractedBy(2);
            Widgets.DrawMenuSection(left);
            DoLeftPanel(left);
        }

        private void DoRightPanel(Rect inRect)
        {
            searchText = Widgets.TextField(inRect.TopPartPixels(20), searchText) ?? "";
            string searchString = searchText.Trim().ToLower();
            inRect.yMin += 25;
            collapsible.Begin(inRect, "Offset simulator", false, false);
            bool suggestionsStarted = false;
            foreach (AttachmentLink link in links)
            {
                if (true
                    && !searchString.NullOrEmpty()
                    && !link.attachment.label.ToLower().Contains(searchString))
                    continue;
                if(!suggestionsStarted && fake[link])
                {
                    suggestionsStarted = true;
                    collapsible.Label("<color=red>Warning</color>", fontSize: GameFont.Small, fontStyle: FontStyle.Bold);
                    collapsible.Label("Attachments below are not from this weapon", fontSize: GameFont.Small);
                    collapsible.Gap(10);
                }
                bool showen = !this.hidden[link];
                collapsible.CheckboxLabeled($"{link.attachment.label}", ref showen, fontSize: GameFont.Small);
                this.hidden[link] = !showen;
                if (showen)
                {
                    collapsible.Gap(2);
                    collapsible.Label($"current drawOffset value:", fontSize: GameFont.Tiny);
                    collapsible.Lambda(18, (rect) =>
                     {
                         Text.Font = GameFont.Tiny;
                         GUI.color = Color.green;
                         Widgets.TextField(rect, $"({Math.Round(link.drawOffset.x, 3)},{Math.Round(link.drawOffset.y, 3)})");
                     }, useMargins: true);
                    collapsible.Gap(2);
                    collapsible.Lambda(20, (rect) =>
                    {
                        Text.Font = GameFont.Tiny;
                        link.drawOffset.x = Widgets.HorizontalSlider(rect, link.drawOffset.x, -0.75f, 0.75f, true, $"drawOffset.x={Math.Round(link.drawOffset.x, 3)}");
                    }, useMargins: true);
                    collapsible.Gap(2);
                    collapsible.Lambda(20, (rect) =>
                    {
                        Text.Font = GameFont.Tiny;
                        link.drawOffset.y = Widgets.HorizontalSlider(rect, link.drawOffset.y, -0.75f, 0.75f, true, $"drawOffset.y={Math.Round(link.drawOffset.y, 3)}");
                    }, useMargins: true);
                    collapsible.Gap(2);
                    collapsible.Label($"current drawScale value:", fontSize: GameFont.Tiny);
                    collapsible.Lambda(18, (rect) =>
                    {
                        Text.Font = GameFont.Tiny;
                        GUI.color = Color.green;
                        Widgets.TextField(rect, $"{link.drawScale}");
                    }, useMargins: true);
                    collapsible.Gap(2);
                    collapsible.Lambda(20, (rect) =>
                    {
                        Text.Font = GameFont.Tiny;
                        link.drawScale = Widgets.HorizontalSlider(rect, link.drawScale, 0.6f, 1.6f, true, $"drawScale={Math.Round(link.drawScale, 3)}");
                    }, useMargins: true);
                }
                collapsible.Line(1);
            }
            collapsible.End(ref inRect);
        }

        private void DoLeftPanel(Rect inRect)
        {
            DoPreview(inRect);
        }        

        private void DoPreview(Rect inRect)
        {
            Rect rect = inRect;
            rect.width = Mathf.Min(inRect.width, inRect.height);
            rect.height = rect.width;
            rect = rect.CenteredOnXIn(inRect);
            rect = rect.CenteredOnYIn(inRect);            
            Widgets.DrawBoxSolid(rect, Widgets.MenuSectionBGBorderColor);
            Widgets.DrawBoxSolid(rect.ContractedBy(1), new Color(0.2f, 0.2f, 0.2f));
            GUIUtility.DrawWeaponWithAttachments(inRect, weaponDef, links.Where(l => !hidden[l]).ToHashSet(), null, 0.7f);
        }        
    }
}
