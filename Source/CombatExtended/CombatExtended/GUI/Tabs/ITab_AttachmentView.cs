﻿using System;
using RimWorld;
using CombatExtended.RocketGUI;
using UnityEngine;
using GUIUtility = CombatExtended.RocketGUI.GUIUtility;
using Verse;
using System.Linq;

namespace CombatExtended
{
    public class ITab_AttachmentView : ITab
    {
        private AttachmentLink highlighted = null;
        private static readonly Listing_Collapsible collapsible = new Listing_Collapsible();

        public override bool IsVisible => SelThing is WeaponPlatform;        

        public WeaponPlatform Weapon
        {
            get
            {
                return SelThing as WeaponPlatform;
            }
        }

        public ITab_AttachmentView()
        {            
            size = new Vector2(460f, 450f);            
            labelKey = "TabAttachments";
            tutorTag = "Attachments";
            // prepare our theme
            collapsible.CollapsibleBGBorderColor = Color.gray;
            collapsible.Margins = new Vector2(3, 0);
        }

        public override void FillTab()
        {
            collapsible.Expanded = true;
            highlighted = null;
            Text.Font = GameFont.Small;            
            Rect rect = new Rect(0f, 20f, size.x, size.y - 20f).ContractedBy(10);           
            GUIUtility.ExecuteSafeGUIAction(() =>
            {                                
                DoContent(rect);                
            });            
        }

        /// <summary>
        /// Draw the actual tab
        /// </summary>
        /// <param name="inRect"></param>
        private void DoContent(Rect inRect)
        {            
            collapsible.Begin(inRect);            
            collapsible.Label(Weapon.def.DescriptionDetailed);            
            Rect weaponRect = inRect;
            collapsible.Lambda(100, (rect) => { weaponRect = rect; });
            collapsible.Lambda(20, (rect)=>
            {
                Text.Font = GameFont.Small;                               
                Text.Anchor = TextAnchor.LowerRight;
                bool windowOpen = Find.WindowStack.IsOpen<Window_AttachmentsEditor>();
                GUI.color = windowOpen ? Color.gray : Mouse.IsOver(rect) ? Color.white : Color.cyan;
                Widgets.Label(rect, "CE_AttachmentsEdit".Translate());
                if (!windowOpen && Widgets.ButtonInvisible(rect))                
                    Find.WindowStack.Add(new Window_AttachmentsEditor(Weapon));                
                Text.Anchor = TextAnchor.LowerLeft;
                GUI.color = Color.gray;
                Widgets.Label(rect, "CE_Attachments".Translate());
            }, useMargins: true);            
            collapsible.Line(1);
            collapsible.Gap(1);
            AttachmentLink[] links = Weapon.CurLinks;
            for(int i = 0;i < links.Length; i++)
            {                
                AttachmentLink link = links[i];
                collapsible.Lambda(28, (rect) =>
                {                    
                    Widgets.DefLabelWithIcon(rect, link.attachment);
                    Widgets.InfoCardButton(rect.RightPartPixels(rect.height).ContractedBy(1), link.attachment);
                    GUIUtility.ExecuteSafeGUIAction(() =>
                    {
                        GUI.color = Color.gray;
                        Text.Anchor = TextAnchor.MiddleRight;
                        Widgets.Label(rect.LeftPartPixels(rect.width - rect.height - 5), link.attachment.slotTags[0]);
                    });
                    if (Mouse.IsOver(rect))
                        highlighted = link;
                }, useMargins: true);                
            }
            collapsible.Gap(4);
            collapsible.Label("CE_AttachmentsAdditions".Translate(), Color.gray, fontSize: GameFont.Small, anchor: TextAnchor.LowerLeft);
            collapsible.Line(1);            
            foreach(AttachmentDef attachment in Weapon.AdditionList)
            {                
                collapsible.Lambda(28, (rect) =>
                {
                    Widgets.DefLabelWithIcon(rect, attachment);
                    Widgets.InfoCardButton(rect.RightPartPixels(rect.height).ContractedBy(1), attachment);
                    GUIUtility.ExecuteSafeGUIAction(() =>
                    {
                        GUI.color = Color.gray;
                        Text.Anchor = TextAnchor.MiddleRight;
                        Widgets.Label(rect.LeftPartPixels(rect.width - rect.height - 5), attachment.slotTags[0]);
                    });
                    if (Mouse.IsOver(rect))
                        highlighted = Weapon.Platform.attachmentLinks.First(l => l.attachment == attachment);
                }, useMargins: true);
            }
            collapsible.Gap(4);
            collapsible.Label("CE_AttachmentsRemovals".Translate(), Color.gray, fontSize: GameFont.Small, anchor: TextAnchor.LowerLeft);
            collapsible.Line(1);
            foreach (AttachmentDef attachment in Weapon.RemovalList)
            {
                collapsible.Lambda(28, (rect) =>
                {
                    Widgets.DefLabelWithIcon(rect, attachment);
                    Widgets.InfoCardButton(rect.RightPartPixels(rect.height).ContractedBy(1), attachment);
                    GUIUtility.ExecuteSafeGUIAction(() =>
                    {
                        GUI.color = Color.gray;
                        Text.Anchor = TextAnchor.MiddleRight;
                        Widgets.Label(rect.LeftPartPixels(rect.width - rect.height - 5), attachment.slotTags[0]);
                    });
                    if (Mouse.IsOver(rect))
                        highlighted = Weapon.Platform.attachmentLinks.First(l => l.attachment == attachment);
                }, useMargins: true);
            }
            weaponRect.width = Mathf.Min(weaponRect.height, weaponRect.width);
            weaponRect = weaponRect.CenteredOnXIn(inRect);
            GUIUtility.DrawWeaponWithAttachments(weaponRect.ExpandedBy(10), weapon: Weapon, highlighted);
            collapsible.End(ref inRect);            
        }
    }
}
