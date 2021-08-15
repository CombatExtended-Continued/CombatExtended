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
    /// <summary>
    /// A container used for rendering the attachment editor in any place.
    /// </summary>
    public class Fragment_AttachmentEditor
    {        
        public readonly WeaponPlatform weapon;
        public readonly WeaponPlatformDef weaponDef;

        private static List<StatDef> displayStats = null;
        private static List<StatDef> positiveStats = null;
        private static List<StatDef> negativeStats = null;

        private AttachmentDef hoveringOver = null;
        private List<ThingDefCountClass> cost = new List<ThingDefCountClass>();
        private List<string> tags = new List<string>();

        private Dictionary<StatDef, float> statBases = new Dictionary<StatDef, float>();
        private Dictionary<StatDef, float> stats = new Dictionary<StatDef, float>();

        private Dictionary<AttachmentLink, bool> attachedByAt = new Dictionary<AttachmentLink, bool>();
        private Dictionary<AttachmentLink, bool> additionByAt = new Dictionary<AttachmentLink, bool>();
        private Dictionary<AttachmentLink, bool> removalByAt = new Dictionary<AttachmentLink, bool>();

        private List<WeaponPlatformDef.WeaponGraphicPart> visibleDefaultParts = new List<WeaponPlatformDef.WeaponGraphicPart>();

        private List<AttachmentLink> links = new List<AttachmentLink>();
        private List<AttachmentLink> target = new List<AttachmentLink>();        

        private Dictionary<string, List<AttachmentLink>> linksByTag = new Dictionary<string, List<AttachmentLink>>();

        private readonly Listing_Collapsible collapsible_radios = new Listing_Collapsible(true, true);
        private readonly Listing_Collapsible collapsible_stats = new Listing_Collapsible(true, true);
        private readonly Listing_Collapsible collapsible_center = new Listing_Collapsible(true, true);        

        public List<AttachmentLink> CurConfig
        {
            get
            {
                return links.Where(l => additionByAt[l] || (attachedByAt[l] && !removalByAt[l])).ToList();
            }
        }

        public List<AttachmentLink> CurAdditions
        {
            get
            {
                return links.Where(t => !attachedByAt[t] && additionByAt[t]).ToList();
            }
        }

        public List<AttachmentLink> CurDeletions
        {
            get
            {
                return links.Where(t => removalByAt[t] && attachedByAt[t]).ToList();
            }
        }

        /// <summary>
        /// Initialize the editor to use a hyposetical weapon with quality of normal as the base for stat calculations
        /// </summary>
        /// <param name="weaponDef"></param>
        /// <param name="config"></param>
        public Fragment_AttachmentEditor(WeaponPlatformDef weaponDef, List<AttachmentLink> config)
        {
            this.InitializeFragment();
            this.links = weaponDef.attachmentLinks;
            this.weapon = null;
            this.weaponDef = weaponDef;

            foreach (AttachmentLink link in weaponDef.attachmentLinks)
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
            foreach (AttachmentLink link in config)
            {
                this.attachedByAt[link] = true;
                this.AddAttachment(link, update: false);
            }
            foreach (StatDef stat in displayStats)
                statBases[stat] = weaponDef.GetWeaponStatAbstractWith(stat, config);
            this.Update();
        }

        /// <summary>
        /// Initialize the editor base on a provieded weapon
        /// </summary>
        /// <param name="weapon"></param>
        public Fragment_AttachmentEditor(WeaponPlatform weapon)
        {
            this.InitializeFragment();
            this.links = weapon.Platform.attachmentLinks;
            this.weapon = weapon;
            this.weaponDef = weapon.Platform;

            foreach (AttachmentLink link in weaponDef.attachmentLinks)
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
            foreach (StatDef stat in displayStats)
                statBases[stat] = weapon.GetWeaponStatWith(stat, null, true);
            this.Update();
        }

        /// <summary>
        /// Initialize the fragment.
        /// </summary>
        private void InitializeFragment()
        {
            collapsible_center.CollapsibleBGBorderColor = Color.gray;
            collapsible_center.Margins = new Vector2(3, 0);
            collapsible_stats.CollapsibleBGBorderColor = Color.gray;
            collapsible_stats.Margins = new Vector2(3, 0);
            collapsible_radios.CollapsibleBGBorderColor = Color.gray;
            collapsible_radios.Margins = new Vector2(3, 0);
            if (displayStats == null)
            {
                displayStats = new List<StatDef>()
                {
                    StatDefOf.MarketValue,
                    StatDefOf.Mass,
                    CE_StatDefOf.Bulk,
                    StatDefOf.RangedWeapon_Cooldown,
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
                    StatDefOf.RangedWeapon_Cooldown,
                    CE_StatDefOf.ReloadSpeed,
                    CE_StatDefOf.Bulk,
                    CE_StatDefOf.MuzzleFlash,
                    CE_StatDefOf.ShotSpread,
                    CE_StatDefOf.SwayFactor,
                };
            }
        }

        /// <summary>
        /// Draw the editor
        /// </summary>
        /// <param name="inRect"></param>
        public void DoContents(Rect inRect)
        {
            float width = inRect.width;
            hoveringOver = null;                      
            DoRightPanel(inRect.RightPartPixels(width * 0.3f).ContractedBy(2));
            inRect.xMax -= width * 0.3f;           
            DoCenterPanel(inRect.RightPartPixels(width * 0.4f).ContractedBy(2));
            inRect.xMax -= width * 0.4f;
            DoLeftPanel(inRect.ContractedBy(2));            
        }

        /// <summary>
        /// Do selection panel
        /// </summary>
        /// <param name="inRect"></param>
        private void DoRightPanel(Rect inRect)
        {
            inRect.xMin += 5;
            collapsible_radios.Expanded = true;
            collapsible_radios.Begin(inRect);
            collapsible_radios.Label("CE_Attachments_Options".Translate(), fontSize: GameFont.Small, anchor: TextAnchor.LowerLeft, color: Color.white, hightlightIfMouseOver: false);
            collapsible_radios.Gap(2);
            collapsible_radios.Label("CE_Attachments_Options_Tip".Translate());
            collapsible_radios.Gap(1);            
            bool started = false;
            bool stop = false;
            foreach (string tag in tags)
            {
                if (started)
                {
                    collapsible_radios.Gap(2.00f);
                }
                collapsible_radios.Gap(3);
                collapsible_radios.Label($"CE_AttachmentSlot_{tag}".Translate(), color: Color.gray, fontSize: GameFont.Small, anchor: TextAnchor.LowerLeft, hightlightIfMouseOver: false);
                collapsible_radios.Line(1.0f);
                collapsible_radios.Gap(2);
                foreach (AttachmentLink link in linksByTag[tag])
                {
                    AttachmentDef attachment = link.attachment;
                    collapsible_radios.Gap(2);
                    collapsible_radios.Lambda(20, (rect) =>
                    {
                        bool visible = (attachedByAt[link] || additionByAt[link]) && !removalByAt[link];
                        bool checkOn = visible;
                        Widgets.DefIcon(rect.LeftPartPixels(20).ContractedBy(2), attachment);
                        rect.xMin += 25;
                        Color color = (attachedByAt[link] && !removalByAt[link]) ? Color.white : (additionByAt[link] ? Color.green : (removalByAt[link] ?  Color.red : Color.white));
                        GUIUtility.CheckBoxLabeled(rect, attachment.label.CapitalizeFirst(), color, ref checkOn, texChecked: Widgets.RadioButOnTex, texUnchecked: Widgets.RadioButOffTex, drawHighlightIfMouseover: false, font: GameFont.Small);                        
                        if (checkOn != visible)
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
        }

        /// <summary>
        /// Do the preview
        /// </summary>
        /// <param name="inRect"></param>
        private void DoCenterPanel(Rect inRect)
        {
            DoLoadoutPanel(inRect.SliceYPixels(40));
            inRect.yMin += 5;
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                Widgets.DrawMenuSection(inRect);
                if (cost.Count != 0)
                {
                    Rect costRect = inRect.BottomPartPixels((cost.Count + 2) * 22f);
                    costRect.xMin += 5;
                    costRect.xMax -= 5;
                    collapsible_center.Expanded = true;
                    collapsible_center.Begin(costRect);
                    collapsible_center.Label($"CE_EditAttachmentsCost".Translate(), color: Color.gray, fontSize: GameFont.Small, anchor: TextAnchor.LowerLeft, hightlightIfMouseOver: false);
                    collapsible_center.Line(1.0f);
                    collapsible_center.Gap(2.00f);
                    foreach (ThingDefCountClass countClass in cost)
                    {
                        collapsible_center.Lambda(20, (rect) =>
                        {
                            Text.Font = GameFont.Small;
                            Text.Anchor = TextAnchor.UpperLeft;
                            Widgets.DefIcon(rect.LeftPartPixels(20).ContractedBy(2), countClass.thingDef);
                            rect.xMin += 25;
                            Widgets.Label(rect, countClass.thingDef.label);
                            Text.Anchor = TextAnchor.UpperRight;
                            Widgets.Label(rect, $"x{countClass.count}");
                        }, useMargins: true);
                        collapsible_center.Gap(2);
                    }
                    collapsible_center.End(ref costRect);
                    inRect.yMax -= (cost.Count + 1.5f) * 20f / 2f;
                }
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
            });
        }

        /// <summary>
        /// Do the stat panel
        /// </summary>
        /// <param name="inRect"></param>
        private void DoLeftPanel(Rect inRect)
        {
            inRect.xMax -= 5;
            collapsible_stats.Expanded = true;
            collapsible_stats.Begin(inRect);
            collapsible_stats.Label("CE_Attachments_Information".Translate(), fontSize: GameFont.Small, anchor: TextAnchor.LowerLeft, color: Color.white, hightlightIfMouseOver: false);
            collapsible_stats.Gap(2);
            collapsible_stats.Label("CE_Attachments_Information_Tip".Translate(), hightlightIfMouseOver: false);
            collapsible_stats.Gap(4);
            collapsible_stats.Label("CE_EditAttachmentsStats".Translate(), fontSize: GameFont.Small, anchor: TextAnchor.LowerLeft, color: Color.gray, hightlightIfMouseOver: false);            
            collapsible_stats.Line(1);
            foreach (StatDef stat in displayStats)
            {
                GUIUtility.ExecuteSafeGUIAction(() =>
                {
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                    collapsible_stats.Gap(2);
                    collapsible_stats.Lambda(stat.label.GetTextHeight(inRect.width * 0.65f - 10), (rect) =>
                    {
                        rect.xMin += 5;
                        TooltipHandler.TipRegion(rect, stat.description);
                        Text.Font = GameFont.Small;
                        Text.Anchor = TextAnchor.UpperLeft;
                        Widgets.LabelFit(rect.LeftPart(0.85f), stat.label.CapitalizeFirst());
                        GUI.color = GetStatColor(stat);
                        Widgets.Label(rect.RightPart(0.15f), $" {((float)Math.Round(statBases[stat] + stats[stat], 2)).ToStringByStyle(stat.toStringStyle)}");
                        GUI.color = Color.white;
                        Text.Anchor = TextAnchor.UpperRight;
                        Widgets.Label(rect.LeftPart(0.85f), $"{((float)Math.Round(statBases[stat], 2)).ToStringByStyle(stat.toStringStyle)} |");
                    }, useMargins: true, hightlightIfMouseOver: true);
                });
            }
            if (hoveringOver == null)
            {
                collapsible_stats.Gap(4);
                collapsible_stats.Label(weaponDef.label, fontSize: GameFont.Small, anchor: TextAnchor.LowerLeft, color: Color.gray);
                collapsible_stats.Line(1);
                collapsible_stats.Gap(2);
                collapsible_stats.Label(weaponDef.DescriptionDetailed, fontSize: GameFont.Small);
            }
            else
            {
                collapsible_stats.Gap(4);
                collapsible_stats.Label(hoveringOver.label, fontSize: GameFont.Small, anchor: TextAnchor.LowerLeft, color: Color.gray);
                collapsible_stats.Line(1);
                collapsible_stats.Gap(2);
                collapsible_stats.Label(hoveringOver.DescriptionDetailed, fontSize: GameFont.Small);
            }
            collapsible_stats.End(ref inRect);
        }

        /// <summary>        
        /// </summary>
        /// <param name="inRect"></param>
        private void DoLoadoutPanel(Rect inRect)
        {
            Widgets.DrawMenuSection(inRect);
        }      

        /// <summary>
        /// Add an attachment to the current selection list. Please avoid spaming updates.
        /// </summary>
        /// <param name="attachmentLink">Link to be addded</param>
        /// <param name="update">Wether to call Update</param>
        private void AddAttachment(AttachmentLink attachmentLink, bool update = true)
        {
            foreach (AttachmentLink other in links)
            {
                if ((attachedByAt[other] || additionByAt[other]) && !removalByAt[other] && !attachmentLink.CompatibleWith(other))
                    RemoveAttachment(other, update: false);
            }
            removalByAt[attachmentLink] = false;
            additionByAt[attachmentLink] = true;
            if (update) Update();
        }

        /// <summary>
        /// Remove attachment from the current selection list. Please avoid spaming updates.
        /// </summary>
        /// <param name="attachmentLink">Link to be removed</param>
        /// <param name="update">Wether to call Update</param>
        private void RemoveAttachment(AttachmentLink attachmentLink, bool update = true)
        {
            additionByAt[attachmentLink] = false;
            if (attachedByAt[attachmentLink]) removalByAt[attachmentLink] = true;
            if (update) Update();
        }

        /// <summary>
        /// Should be called on any change in attachedByAt, removalByAt, additionByAt or stats.
        /// Update the window internel states. This include recaching stats, finding what default graphics parts should be visible.        
        /// </summary>
        private void Update()
        {
            // recache targets for rendering
            target.Clear();
            target.AddRange(links.Where(l => (attachedByAt[l] && !removalByAt[l]) || additionByAt[l]));
            // recache visible graphic parts
            visibleDefaultParts.Clear();
            visibleDefaultParts.AddRange(weaponDef.defaultGraphicParts);
            visibleDefaultParts.RemoveAll(p => target.Any(l => weaponDef.AttachmentRemoves(l.attachment, p)));
            stats.Clear();
            // recache the stats
            foreach (StatDef stat in displayStats)
            {
                float val = weapon != null ? weapon.GetWeaponStatWith(stat, target) : weaponDef.GetWeaponStatAbstractWith(stat, target);
                stats[stat] = val - statBases[stat];
            }
            // recache the cost
            cost.Clear();
            foreach(AttachmentLink link in CurAdditions)
            {
                foreach(ThingDefCountClass countClass in link.attachment.costList)
                {
                    // try not to add the same thing twice
                    ThingDefCountClass counter = cost.FirstOrFallback(c => c.thingDef == countClass.thingDef, null);
                    if(counter == null)
                    {
                        counter = new ThingDefCountClass(countClass.thingDef, 0);
                        cost.Add(counter);
                    }
                    counter.count += countClass.count;
                }
            }
        }

        /// <summary>
        /// Used to obtain the rendering color for a stat row. Used for indicating wether a stat has changed to the best or the worst.
        /// </summary>
        /// <param name="stat"></param>
        /// <returns></returns>
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
