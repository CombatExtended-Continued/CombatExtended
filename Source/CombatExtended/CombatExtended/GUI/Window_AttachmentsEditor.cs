using System;
using RimWorld;
using Verse;
using CombatExtended.RocketGUI;
using UnityEngine;
using GUIUtility = CombatExtended.RocketGUI.GUIUtility;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;

namespace CombatExtended
{
    public class Window_AttachmentsEditor : Window
    {        
        private const int PANEL_BASE_WIDTH = 450;
        private const int PANEL_BASE_HEIGHT = 450;

        public readonly WeaponPlatform weapon;
        public readonly WeaponPlatformDef weaponDef;

        private static List<StatDef> displayStats = null;
        private static List<StatDef> positiveStats = null;
        private static List<StatDef> negativeStats = null;

        private AttachmentDef hoveringOver = null;

        private Dictionary<StatDef, float> statBases = new Dictionary<StatDef, float>();
        private Dictionary<StatDef, float> stats = new Dictionary<StatDef, float>();
        private Dictionary<AttachmentLink, bool> attachedByAt = new Dictionary<AttachmentLink, bool>();
        private Dictionary<AttachmentLink, bool> additionByAt = new Dictionary<AttachmentLink, bool>();
        private Dictionary<AttachmentLink, bool> removalByAt = new Dictionary<AttachmentLink, bool>();

        private List<WeaponPlatformDef.WeaponGraphicPart> visibleDefaultParts = new List<WeaponPlatformDef.WeaponGraphicPart>();
        private List<AttachmentLink> links = new List<AttachmentLink>();
        private List<AttachmentLink> target = new List<AttachmentLink>();
        private List<string> tags = new List<string>();

        private Dictionary<string, List<AttachmentLink>> linksByTag = new Dictionary<string, List<AttachmentLink>>();

        private readonly Listing_Collapsible collapsible_radios = new Listing_Collapsible(true, true);
        private readonly Listing_Collapsible collapsible_stats = new Listing_Collapsible(true, true);

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(1000, 575);
            }
        }

        public Window_AttachmentsEditor(WeaponPlatform weapon, Map map)
        {
            if(displayStats == null)
            {
                displayStats = new List<StatDef>()
                {
                    StatDefOf.Mass,
                    StatDefOf.MarketValue,
                    CE_StatDefOf.Bulk,
                    CE_StatDefOf.SightsEfficiency,
                    CE_StatDefOf.NightVisionEfficiency_Weapon,
                    CE_StatDefOf.ReloadSpeed,
                    CE_StatDefOf.MuzzleFlash,
                    CE_StatDefOf.MagazineCapacity,
                    CE_StatDefOf.ShotSpread,
                    CE_StatDefOf.SwayFactor,
                };
                positiveStats = new List<StatDef>()
                {                   
                    StatDefOf.MarketValue,                    
                    CE_StatDefOf.SightsEfficiency,
                    CE_StatDefOf.NightVisionEfficiency_Weapon,                                    
                    CE_StatDefOf.MagazineCapacity,                                        
                };
                negativeStats = new List<StatDef>()
                {
                    StatDefOf.Mass,
                    CE_StatDefOf.ReloadSpeed,
                    CE_StatDefOf.Bulk,                    
                    CE_StatDefOf.MuzzleFlash,                    
                    CE_StatDefOf.ShotSpread,
                    CE_StatDefOf.SwayFactor,
                };                
            }
            this.links = weapon.Platform.attachmentLinks;
            this.weapon = weapon;
            this.weaponDef = weapon.Platform;
            this.layer = WindowLayer.Dialog;
            this.resizer = new WindowResizer();
            this.forcePause = true;
            this.doCloseButton = false;
            this.doCloseX = false;
            this.draggable = true;            

            foreach(AttachmentLink link in weaponDef.attachmentLinks)
            {
                string tag = link.attachment.slotTags.First();
                if (!this.linksByTag.TryGetValue(tag, out List<AttachmentLink> tagLinks))
                {
                    this.tags.Add(tag);
                    this.linksByTag[tag] = new List<AttachmentLink>();
                }
                this.linksByTag[tag].Add(link);
                this.attachedByAt[link] = false;
                this.additionByAt[link] = false;
                this.removalByAt[link] = false;
            }
            this.tags.SortBy(x => x);
            foreach (AttachmentLink link in weapon.CurLinks)
            {                
                this.attachedByAt[link] = true;
                this.AddAttachment(link, update: false);
            }
            foreach(StatDef stat in displayStats)           
                statBases[stat] =  weapon.GetStatWithAbstractAttachments(stat, null, true);
            
            this.Update();
        }

        public override void DoWindowContents(Rect inRect)
        {
            GUIUtility.ExecuteSafeGUIAction(() =>
            {                
                Rect titleRect = inRect;
                Text.Font = GameFont.Medium;
                titleRect.xMin += 5;
                titleRect.height = 35;
                Widgets.DefIcon(titleRect.LeftPartPixels(titleRect.height).ContractedBy(2), weaponDef, scale: 1.7f);
                titleRect.xMin += titleRect.height + 10;
                Widgets.Label(titleRect, "CE_EditAttachments".Translate() + $": {weaponDef.label.CapitalizeFirst()}");
            });
            inRect.yMin += 40;
            Rect rect = inRect;
            rect.height = PANEL_BASE_HEIGHT;
            hoveringOver = null;
            DoRightPanel(rect.RightPartPixels((inRect.width - PANEL_BASE_WIDTH) / 2f).ContractedBy(2));            
            rect.xMax -= (inRect.width - PANEL_BASE_WIDTH) / 2f;
            DoCenterPanel(rect.RightPartPixels(PANEL_BASE_WIDTH).ContractedBy(2));
            rect.xMax -= PANEL_BASE_WIDTH;
            DoLeftPanel(rect.ContractedBy(2));
            inRect.yMin += PANEL_BASE_HEIGHT + 5;           
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                Text.Font = GameFont.Small;
                Rect actionRect = inRect;
                actionRect.yMin += 5;
                actionRect.width = 300;
                actionRect = actionRect.CenteredOnXIn(inRect);                
                GUI.color = Color.red;
                if (Widgets.ButtonText(actionRect.RightPartPixels(146), "CE_Close".Translate()))
                {
                    this.Close();
                }
                GUI.color = Color.white;
                if (Widgets.ButtonText(actionRect.LeftPartPixels(146), "CE_Apply".Translate()))
                {
                    this.Apply();
                    this.Close();
                }
            });
        }

        private void DoRightPanel(Rect inRect)
        {
            collapsible_radios.Expanded = true;
            collapsible_radios.Begin(inRect, "CE_Attachments_Selection".Translate(), false, false);
            bool started = false;
            bool stop = false;
            foreach(string tag in tags)
            {
                if (started)
                {
                    collapsible_radios.Gap(2.00f);
                    collapsible_radios.Line(1.0f);
                }
                collapsible_radios.Gap(3);
                collapsible_radios.Lambda(24, (rect) =>
                {                                     
                    GUIUtility.ExecuteSafeGUIAction(() =>
                    {
                        Text.Font = GameFont.Small;
                        Text.CurFontStyle.fontSize = 12;
                        Widgets.Label(rect, $"CE_AttachmentSlot_{tag}".Translate());
                    });
                }, useMargins: true);                
                foreach (AttachmentLink link in linksByTag[tag])
                {
                    AttachmentDef attachment = link.attachment;
                    collapsible_radios.Lambda(20, (rect) =>
                    {                        
                        bool visible = (attachedByAt[link] || additionByAt[link]) && !removalByAt[link];
                        bool checkOn = visible;                        
                        Widgets.DefIcon(rect.LeftPartPixels(20).ContractedBy(2), attachment);
                        rect.xMin += 25;
                        GUIUtility.CheckBoxLabeled(rect, attachment.label.CapitalizeFirst(), ref checkOn, texChecked: Widgets.RadioButOnTex, texUnchecked: Widgets.RadioButOffTex, drawHighlightIfMouseover: false, font: GameFont.Small);
                        if(checkOn != visible)
                        {
                            if (checkOn)
                            {
                                AddAttachment(link);
                            }
                            else
                            {
                                RemoveAttachment(link);
                            }
                            stop = true;
                        }
                        if (Mouse.IsOver(rect.ExpandedBy(2)))
                        {
                            hoveringOver = attachment;
                            TooltipHandler.TipRegion(rect, attachment.description.CapitalizeFirst());
                        }                        
                    }, useMargins: true, hightlightIfMouseOver: true);
                    collapsible_radios.Gap(2);
                    if (stop) break;
                }
                started = true;
                if (stop) break;
            }
            collapsible_radios.End(ref inRect);
            inRect.yMin -= 1;
            Widgets.DrawMenuSection(inRect);
        }

        private void DoCenterPanel(Rect inRect)
        {
            Widgets.DrawMenuSection(inRect);
            Rect rect = inRect;            
            rect.width = Mathf.Min(rect.width, rect.height);
            rect.height = rect.width;
            rect = rect.CenteredOnXIn(inRect);            
            GUIUtility.DrawWeaponWithAttachments(rect.ContractedBy(10), weaponDef, target.ToHashSet(), parts: visibleDefaultParts, hoveringOver);
            inRect.yMin += rect.height;
            if (Prefs.DevMode && DebugSettings.godMode)
            {
                GUIUtility.ExecuteSafeGUIAction(() =>
                {
                    GUI.color = Color.cyan;
                    if (Widgets.ButtonText(rect.TopPartPixels(20).LeftPartPixels(75), "edit offsets"))
                        Find.WindowStack.Add(new Window_AttachmentsDebugger(weaponDef));
                });
            }
        }

        private void DoLeftPanel(Rect inRect)
        {
            collapsible_stats.Expanded = true;
            collapsible_stats.Begin(inRect, weaponDef.label, false, false);
            foreach (StatDef stat in displayStats)
            {
                collapsible_stats.Lambda(18, (rect) =>
                {
                    TooltipHandler.TipRegion(rect, stat.description);
                    Text.Font = GameFont.Small;
                    Text.CurFontStyle.fontSize = 12;
                    Text.Anchor = TextAnchor.UpperLeft;                    
                    Widgets.Label(rect.LeftPart(0.75f), stat.label);
                    GUI.color = GetStatColor(stat);                    
                    Widgets.Label(rect.RightPart(0.25f), $" {((float)Math.Round(statBases[stat] + stats[stat], 2)).ToStringByStyle(stat.toStringStyle)}");
                    GUI.color = Color.white;
                    Text.Anchor = TextAnchor.UpperRight;                    
                    Widgets.Label(rect.LeftPart(0.75f), $"{((float)Math.Round(statBases[stat], 2)).ToStringByStyle(stat.toStringStyle)} |");
                }, useMargins: true, hightlightIfMouseOver: true);
            }
            collapsible_stats.End(ref inRect);
            inRect.yMin -= 1;
            Widgets.DrawMenuSection(inRect);
        }

        private void Apply()
        {
            List<AttachmentLink> selected = links.Where(l => additionByAt[l] || (attachedByAt[l] && !removalByAt[l])).ToList();
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
            }            
            Job job = weapon.Wielder.thinker.GetMainTreeThinkNode<JobGiver_ModifyWeapon>().TryGiveJob(weapon.Wielder);
            if(job != null)
                weapon.Wielder.jobs.StartJob(job, JobCondition.InterruptForced);            
        }

        private void AddAttachment(AttachmentLink attachmentLink, bool update = true)
        {                      
            foreach (AttachmentLink other in links)
            {
                if ((attachedByAt[other] || additionByAt[other]) && !removalByAt[other] && !attachmentLink.CompatibleWith(other))
                    RemoveAttachment(other, update: false);
            }           
            removalByAt[attachmentLink] = false;
            additionByAt[attachmentLink] = true;            
            if(update) Update();
        }

        private void RemoveAttachment(AttachmentLink attachmentLink, bool update = true)
        {
            additionByAt[attachmentLink] = false;
            if (attachedByAt[attachmentLink]) removalByAt[attachmentLink] = true;
            if (update) Update();
        }        

        private void Update()
        {            
            target.Clear();
            target.AddRange(links.Where(l => (attachedByAt[l] && !removalByAt[l]) || additionByAt[l]));
            visibleDefaultParts.Clear();
            visibleDefaultParts.AddRange(weaponDef.defaultGraphicParts);
            visibleDefaultParts.RemoveAll(p => target.Any(l => weaponDef.AttachmentRemoves(l.attachment, p)));
            stats.Clear();
            foreach (StatDef stat in displayStats)
            {
                float val = weapon.GetStatWithAbstractAttachments(stat, target);                
                stats[stat] = val - statBases[stat];
            }
        }
        
        private Color GetStatColor(StatDef stat)
        {
            float diff = stats[stat];
            if (positiveStats.Contains(stat))            
                return diff > 0 ? Color.green : (diff == 0 ? Color.white : Color.red);
            if (negativeStats.Contains(stat))
                return diff > 0 ? Color.red : (diff == 0 ? Color.white : Color.green);
            throw new NotImplementedException();
        }
    }
}
