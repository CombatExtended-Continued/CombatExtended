using System.Text.RegularExpressions;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CombatExtended
{
    public class ITab_Inventory : ITab_Pawn_Gear
    {
        #region Fields

        private const float _barHeight = 20f;
        private const float _margin = 15f;
        private const float _thingIconSize = 28f;
        private const float _thingLeftX = 36f;
        private const float _thingRowHeight = 28f;
        private const float _topPadding = 20f;
        private static Texture2D _iconEditAttachments;
        private const float _standardLineHeight = 22f;
        private static readonly Color _highlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        private Vector2 _scrollPosition = Vector2.zero;
	private Dictionary<BodyPartRecord, float> sharpArmorCache = new Dictionary<BodyPartRecord, float>();
	private Dictionary<BodyPartRecord, float> bluntArmorCache = new Dictionary<BodyPartRecord, float>();
	private Dictionary<BodyPartRecord, float> heatArmorCache = new Dictionary<BodyPartRecord, float>();
	private int lastArmorTooltipTickSharp = 0;
	private int lastArmorTooltipTickBlunt = 0;
	private int lastArmorTooltipTickHeat = 0;

        private float _scrollViewHeight;

        //private static List<Thing> workingInvList = new List<Thing>();

        #endregion Fields

        #region Constructors

        public ITab_Inventory() : base()
        {
            size = new Vector2(480f, 550f);            
        }


        #endregion Constructors

        #region Properties

        //private bool CanControl
        //{
        //    get
        //    {
        //        Pawn selPawnForGear = this.SelPawnForGear;
        //        return !selPawnForGear.Downed && !selPawnForGear.InMentalState && (selPawnForGear.Faction == Faction.OfPlayer || selPawnForGear.IsPrisonerOfColony) && (!selPawnForGear.IsPrisonerOfColony || !selPawnForGear.Spawned || selPawnForGear.Map.mapPawns.AnyFreeColonistSpawned) && (!selPawnForGear.IsPrisonerOfColony || (!PrisonBreakUtility.IsPrisonBreaking(selPawnForGear) && (selPawnForGear.CurJob == null || !selPawnForGear.CurJob.exitMapOnArrival)));
        //    }
        //}

        //private bool CanControlColonist
        //{
        //    get
        //    {
        //        return this.CanControl && this.SelPawnForGear.IsColonistPlayerControlled;
        //    }
        //}

        //private Pawn SelPawnForGear
        //{
        //    get
        //    {
        //        if (SelPawn != null)
        //        {
        //            return SelPawn;
        //        }
        //        Corpse corpse = SelThing as Corpse;
        //        if (corpse != null)
        //        {
        //            return corpse.InnerPawn;
        //        }
        //        throw new InvalidOperationException("Gear tab on non-pawn non-corpse " + SelThing);
        //    }
        //}

        #endregion Properties

        #region Methods

        public override void OnOpen()
        {            
            _iconEditAttachments ??= ContentFinder<Texture2D>.Get("UI/Icons/gear");

            base.OnOpen();
        }

        public override void FillTab()
        {            
            // get the inventory comp
            CompInventory comp = SelPawn.TryGetComp<CompInventory>();

            // set up rects
            Rect listRect = new Rect(
                _margin,
                _topPadding,
                size.x - 2 * _margin,
                size.y - _topPadding - _margin);

            if (comp != null)
            {
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_InventoryWeightBulk, KnowledgeAmount.FrameDisplayed);

                // adjust rects if comp found
                listRect.height -= (_margin / 2 + _barHeight) * 2;
                Rect weightRect = new Rect(_margin, listRect.yMax + _margin / 2, listRect.width, _barHeight);
                Rect bulkRect = new Rect(_margin, weightRect.yMax + _margin / 2, listRect.width, _barHeight);

                // draw bars
                Utility_Loadouts.DrawBar(bulkRect, comp.currentBulk, comp.capacityBulk, "CE_Bulk".Translate(), SelPawn.GetBulkTip());
                Utility_Loadouts.DrawBar(weightRect, comp.currentWeight, comp.capacityWeight, "CE_Weight".Translate(), SelPawn.GetWeightTip());

                // draw text overlays on bars
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;

                string currentBulk = CE_StatDefOf.CarryBulk.ValueToString(comp.currentBulk, CE_StatDefOf.CarryBulk.toStringNumberSense);
                string capacityBulk = CE_StatDefOf.CarryBulk.ValueToString(comp.capacityBulk, CE_StatDefOf.CarryBulk.toStringNumberSense);
                Widgets.Label(bulkRect, currentBulk + "/" + capacityBulk);

                string currentWeight = comp.currentWeight.ToString("0.#");
                string capacityWeight = CE_StatDefOf.CarryWeight.ValueToString(comp.capacityWeight, CE_StatDefOf.CarryWeight.toStringNumberSense);
                Widgets.Label(weightRect, currentWeight + "/" + capacityWeight);

                Text.Anchor = TextAnchor.UpperLeft;
            }

            // start drawing list (rip from ITab_Pawn_Gear)

            GUI.BeginGroup(listRect);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect outRect = new Rect(0f, 0f, listRect.width, listRect.height);
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, _scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref _scrollPosition, viewRect);
            float num = 0f;
            TryDrawComfyTemperatureRange(ref num, viewRect.width);
            if (ShouldShowOverallArmorCE(SelPawnForGear))
            {
                Widgets.ListSeparator(ref num, viewRect.width, "OverallArmor".Translate());
                TryDrawOverallArmor(ref num, viewRect.width, StatDefOf.ArmorRating_Blunt, "ArmorBlunt".Translate(), " " + "CE_MPa".Translate());
                TryDrawOverallArmor(ref num, viewRect.width, StatDefOf.ArmorRating_Sharp, "ArmorSharp".Translate(), "CE_mmRHA".Translate());
                TryDrawOverallArmor(ref num, viewRect.width, StatDefOf.ArmorRating_Heat, "ArmorHeat".Translate(), "%");
            }
            if (ShouldShowEquipment(SelPawnForGear))
            {
                // get the loadout so we can make a decision to show a button.
                bool showMakeLoadout = false;
                Loadout curLoadout = SelPawnForGear.GetLoadout();
                if (SelPawnForGear.IsColonist && (curLoadout == null || curLoadout.Slots.NullOrEmpty()) && (SelPawnForGear.inventory.innerContainer.Any() || SelPawnForGear.equipment?.Primary != null))
                    showMakeLoadout = true;

                if (showMakeLoadout) num += 3; // Make a little room for the button.

                float buttonY = num; // Could be accomplished with seperator being after the button...

                Widgets.ListSeparator(ref num, viewRect.width, "Equipment".Translate());
                foreach (ThingWithComps current in SelPawnForGear.equipment.AllEquipmentListForReading)
                    DrawThingRowCE(ref num, viewRect.width, current);

                // only offer this button if the pawn has no loadout or has the default loadout and there are things/equipment...
                if (showMakeLoadout)
                {
                    Rect loadoutButtonRect = new Rect(viewRect.width / 2, buttonY, viewRect.width / 2, 26f); // button is half the available width...
                    // Backup GUI color
                    Color color = GUI.color;
                    TextAnchor anchor = Text.Anchor;

                    // Highlight if mouse over
                    GUI.color = Color.cyan;
                    if (Mouse.IsOver(loadoutButtonRect)) GUI.color = Color.white;

                    Text.Anchor = TextAnchor.UpperRight;
                    Widgets.Label(loadoutButtonRect, "CE_MakeLoadout".Translate());

                    if (Widgets.ButtonInvisible(loadoutButtonRect.ContractedBy(2), doMouseoverSound: true))
                    {
                        SyncedAddLoadout(SelPawnForGear);
                    }
                    // Restore GUI color
                    Text.Anchor = anchor;
                    GUI.color = color;
                }
            }
            if (ShouldShowApparel(SelPawnForGear))
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Apparel".Translate());
                foreach (Apparel current2 in from ap in SelPawnForGear.apparel.WornApparel
                                             orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                             select ap)
                {
                    DrawThingRowCE(ref num, viewRect.width, current2);
                }
            }
            if (ShouldShowInventory(SelPawnForGear))
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Inventory".Translate());

                workingInvList.Clear();
                workingInvList.AddRange(SelPawnForGear.inventory.innerContainer);
                for (int i = 0; i < workingInvList.Count; i++)
                {
                    DrawThingRowCE(ref num, viewRect.width, workingInvList[i], true);
                }
            }
            if (Event.current.type == EventType.Layout)
            {
                _scrollViewHeight = num + 30f;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public void DrawThingRowCE(ref float y, float width, Thing thing, bool showDropButtonIfPrisoner = false)
        {

            Rect rect = new Rect(0f, y, width, _thingRowHeight);
            Widgets.InfoCardButton(rect.width - 24f, y, thing.GetInnerIfMinified());
            rect.width -= 24f;
            if (CanControl || (SelPawnForGear.Faction == Faction.OfPlayer && SelPawnForGear.RaceProps.packAnimal) || (showDropButtonIfPrisoner && SelPawnForGear.IsPrisonerOfColony))
            {
                var dropForbidden = SelPawnForGear.IsItemQuestLocked(thing);
                Color color = dropForbidden ? Color.grey : Color.white;
                Color mouseoverColor = dropForbidden ? Color.grey : GenUI.MouseoverColor;
                Rect dropRect = new Rect(rect.width - 24f, y, 24f, 24f);
                TooltipHandler.TipRegion(dropRect, dropForbidden ? "DropThingLocked".Translate() : "DropThing".Translate());
                if (Widgets.ButtonImage(dropRect, TexButton.Drop, color, mouseoverColor) && !dropForbidden)
                {                   
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    SyncedInterfaceDrop(thing);
                }
                rect.width -= 24f;
            }
            if (CanControlColonist)
            {
                if ((thing.def.IsNutritionGivingIngestible || thing.def.IsNonMedicalDrug) && thing.IngestibleNow && base.SelPawn.WillEat(thing, null) && (!SelPawnForGear.IsTeetotaler() || !thing.def.IsNonMedicalDrug))
                {
                    Rect rect3 = new Rect(rect.width - 24f, y, 24f, 24f);
                    TooltipHandler.TipRegion(rect3, "ConsumeThing".Translate(thing.LabelNoCount, thing));
                    if (Widgets.ButtonImage(rect3, TexButton.Ingest))
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                        SyncedInterfaceIngest(thing);
                    }
                    rect.width -= 24f;
                }
            }
            if (thing == SelPawn.equipment?.Primary && thing is WeaponPlatform platform)
            {
                Rect rect3 = new Rect(rect.width - 24f, y, 24f, 24f);
                TooltipHandler.TipRegion(rect3, "CE_EditWeapon".Translate());
                if (Widgets.ButtonImage(rect3, _iconEditAttachments))
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    if (!Find.WindowStack.IsOpen<Window_AttachmentsEditor>())
                        Find.WindowStack.Add(new Window_AttachmentsEditor(platform));
                    this.CloseTab();
                }
                rect.width -= 24f;
            }
            Rect rect4 = rect;
            rect4.xMin = rect4.xMax - 60f;
            CaravanThingsTabUtility.DrawMass(thing, rect4);
            rect.width -= 60f;
            if (Mouse.IsOver(rect))
            {
                GUI.color = _highlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                if (thing is WeaponPlatform weapon)
                {
                    RocketGUI.GUIUtility.DrawWeaponWithAttachments(new Rect(4f, y, _thingIconSize, _thingIconSize), weapon);
                }
                else
                {
                    Widgets.ThingIcon(new Rect(4f, y, _thingIconSize, _thingIconSize), thing, 1f);
                }
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ThingLabelColor;
            Rect thingLabelRect = new Rect(_thingLeftX, y, rect.width - _thingLeftX, _thingRowHeight);
            string thingLabel = thing.LabelCap;
            if ((thing is Apparel && SelPawnForGear.outfits != null && SelPawnForGear.outfits.forcedHandler.IsForced((Apparel)thing))
                || (SelPawnForGear.inventory != null && SelPawnForGear.HoldTrackerIsHeld(thing)))
            {
                thingLabel = thingLabel + ", " + "ApparelForcedLower".Translate();
            }

            Text.WordWrap = false;
            Widgets.Label(thingLabelRect, thingLabel.Truncate(thingLabelRect.width, null));
            Text.WordWrap = true;
            string text2 = string.Concat(new object[]
            {
                thing.LabelCap,
                "\n",
                thing.DescriptionDetailed,
                "\n",
                thing.GetWeightAndBulkTip()
            });
            if (thing.def.useHitPoints)
            {
                string text3 = text2;
                text2 = string.Concat(new object[]
                {
                    text3,
                    "\n",
                    "HitPointsBasic".Translate().CapitalizeFirst(),
                    ": ",
                    thing.HitPoints,
                    " / ",
                    thing.MaxHitPoints
                });
            }
            TooltipHandler.TipRegion(thingLabelRect, text2);
            y += 28f;

            // RMB menu
            if (Widgets.ButtonInvisible(thingLabelRect) && Event.current.button == 1)
            {
                List<FloatMenuOption> floatOptionList = new List<FloatMenuOption>();
                floatOptionList.Add(new FloatMenuOption("ThingInfo".Translate(), delegate
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(thing));
                }, MenuOptionPriority.Default, null, null));
                if (CanControl)
                {
                    // Equip option
                    ThingWithComps eq = thing as ThingWithComps;
                    if (eq != null && eq.TryGetComp<CompEquippable>() != null)
                    {
                        CompInventory compInventory = SelPawnForGear.TryGetComp<CompInventory>();
                        CompBiocodable compBiocoded = eq.TryGetComp<CompBiocodable>();
                        if (compInventory != null)
                        {
                            FloatMenuOption equipOption;
                            string eqLabel = GenLabel.ThingLabel(eq.def, eq.Stuff, 1);
                            if (compBiocoded != null && compBiocoded.Biocoded && compBiocoded.CodedPawn != SelPawnForGear)
                            {
                                equipOption = new FloatMenuOption("CannotEquip".Translate(eqLabel) + ": " + "BiocodedCodedForSomeoneElse".Translate(), null);
                            }
                            else if (SelPawnForGear.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanEquip(eq, SelPawnForGear))
                            {
                                var forbiddenEquipOrPutAway = SelPawnForGear.equipment.AllEquipmentListForReading.Contains(eq) ? "CE_CannotPutAway".Translate(eqLabel) : "CannotEquip".Translate(eqLabel);
                                equipOption = new FloatMenuOption(forbiddenEquipOrPutAway + ": " + "CE_CannotChangeEquipment".Translate(), null);
                            }
                            else if (SelPawnForGear.equipment.AllEquipmentListForReading.Contains(eq) && SelPawnForGear.inventory != null)
                            {
                                equipOption = new FloatMenuOption("CE_PutAway".Translate(eqLabel),
                                    () => SyncedTryTransferEquipmentToContainer(SelPawnForGear));
                            }
                            else if (!SelPawnForGear.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                            {
                                equipOption = new FloatMenuOption("CannotEquip".Translate(eqLabel), null);
                            }
                            else
                            {
                                string equipOptionLabel = "Equip".Translate(eqLabel);
                                if (eq.def.IsRangedWeapon && SelPawnForGear.story != null && SelPawnForGear.story.traits.HasTrait(TraitDefOf.Brawler))
                                {
                                    equipOptionLabel = equipOptionLabel + " " + "EquipWarningBrawler".Translate();
                                }
                                equipOption = new FloatMenuOption(
                                    equipOptionLabel,
                                    (SelPawnForGear.story != null && SelPawnForGear.WorkTagIsDisabled(WorkTags.Violent))
                                    ? null
                                    : new Action(() => SyncedTrySwitchToWeapon(compInventory, eq)));
                            }
                            floatOptionList.Add(equipOption);
                        }
                    }
                    //Reload apparel option
                    var worn_apparel = SelPawnForGear?.apparel?.WornApparel;
                    foreach (var apparel in worn_apparel)
                    {
                        var compReloadable = apparel.TryGetComp<CompReloadable>();
                        if (compReloadable != null && compReloadable.AmmoDef == thing.def && compReloadable.NeedsReload(true))
                        {
                            if (!SelPawnForGear.Drafted)    //TODO-1.2 This should be doable for drafted pawns as well, but the job does nothing. Figure out what's wrong and remove this condition.
                            {
                                FloatMenuOption reloadApparelOption = new FloatMenuOption(
                                "CE_ReloadApparel".Translate(apparel.Label, thing.Label),
                                new Action(delegate
                                {
                                    //var reloadJob = JobMaker.MakeJob(JobDefOf.Reload, apparel, thing);
                                    //SelPawnForGear.jobs.StartJob(reloadJob, JobCondition.InterruptForced, null, SelPawnForGear.CurJob?.def != reloadJob.def, true);

                                    SyncedReloadApparel(SelPawnForGear, apparel, thing);
                                })
                            );
                                floatOptionList.Add(reloadApparelOption);
                            }

                        }
                    }
                    // Consume option
                    if (CanControl && thing.IngestibleNow && base.SelPawn.RaceProps.CanEverEat(thing))
                    {
                        Action eatFood = delegate
                        {
                            SoundDefOf.Tick_High.PlayOneShotOnCamera();
                            SyncedInterfaceIngest(thing);
                        };
                        string label = thing.def.ingestible.ingestCommandString.NullOrEmpty() ? (string)"ConsumeThing".Translate(thing.LabelShort, thing) : string.Format(thing.def.ingestible.ingestCommandString, thing.LabelShort);
                        if (SelPawnForGear.IsTeetotaler() && thing.def.IsNonMedicalDrug)
                        {
                            floatOptionList.Add(new FloatMenuOption(label + ": " + TraitDefOf.DrugDesire.degreeDatas.Where(x => x.degree == -1).First()?.label, null));
                        }
                        else
                        {
                            floatOptionList.Add(new FloatMenuOption(label, eatFood));
                        }
                    }
                    // Drop, and drop&haul options
                    if (SelPawnForGear.IsItemQuestLocked(eq))
                    {
                        floatOptionList.Add(new FloatMenuOption("CE_CannotDropThing".Translate() + ": " + "DropThingLocked".Translate(), null));
                        floatOptionList.Add(new FloatMenuOption("CE_CannotDropThingHaul".Translate() + ": " + "DropThingLocked".Translate(), null));
                    }
                    else
                    {
                        floatOptionList.Add(new FloatMenuOption("DropThing".Translate(), new Action(delegate
                        {
                            SoundDefOf.Tick_High.PlayOneShotOnCamera();
                            SyncedInterfaceDrop(thing);
                        })));
                        floatOptionList.Add(new FloatMenuOption("CE_DropThingHaul".Translate(), new Action(delegate
                        {
                            SoundDefOf.Tick_High.PlayOneShotOnCamera();
                            SyncedInterfaceDropHaul(thing);
                        })));
                    }
                    // Stop holding in inventory option
                    if (CanControl && SelPawnForGear.HoldTrackerIsHeld(thing))
                    {
                        Action forgetHoldTracker = delegate
                        {
                            SoundDefOf.Tick_High.PlayOneShotOnCamera();
                            SyncedHoldTrackerForget(SelPawnForGear, thing);
                        };
                        floatOptionList.Add(new FloatMenuOption("CE_HoldTrackerForget".Translate(), forgetHoldTracker));
                    }
                }
                FloatMenu window = new FloatMenu(floatOptionList, thing.LabelCap, false);
                Find.WindowStack.Add(window);
            }
            // end menu
        }

        // RimWorld.ITab_Pawn_Gear
        private void TryDrawOverallArmor(ref float curY, float width, StatDef stat, string label, string unit)
        {
	    Dictionary<BodyPartRecord, float> armorCache;
	    int thisTick = Find.TickManager.TicksAbs;
	    bool shouldRebuildCache = false;
	    if (stat == StatDefOf.ArmorRating_Blunt) {
		armorCache = bluntArmorCache;
		shouldRebuildCache = thisTick != lastArmorTooltipTickBlunt;
		lastArmorTooltipTickBlunt = thisTick;
	    }
	    else if (stat == StatDefOf.ArmorRating_Sharp) {
		armorCache = sharpArmorCache;
		shouldRebuildCache = thisTick != lastArmorTooltipTickSharp;
		lastArmorTooltipTickSharp = thisTick;
	    }
	    else if (stat == StatDefOf.ArmorRating_Heat) {
		armorCache = heatArmorCache;
		shouldRebuildCache = thisTick != lastArmorTooltipTickHeat;
		lastArmorTooltipTickHeat = thisTick;
	    }
	    else {
		Log.Error("Trying to get armor tooltip for unsupported armor type: "+stat);
		return;
	    }
	    if (shouldRebuildCache) {
		RebuildArmorCache(armorCache, stat);
	    }
	    TryDrawOverallArmor(armorCache, ref curY, width, stat, label, unit);

	}

	private void RebuildArmorCache(Dictionary<BodyPartRecord, float> armorCache, StatDef stat)
	{
	    armorCache.Clear();
	    float naturalArmor = SelPawnForGear.GetStatValue(stat);
	    List<Apparel> wornApparel = SelPawnForGear.apparel?.WornApparel ?? null;
	    foreach (BodyPartRecord part in SelPawnForGear.RaceProps.body.AllParts)
	    {
		if (part.depth == BodyPartDepth.Outside && (part.coverage >= 0.1 || (part.def == BodyPartDefOf.Eye || part.def == BodyPartDefOf.Neck)))
		{
		    float armorValue = part.IsInGroup(CE_BodyPartGroupDefOf.CoveredByNaturalArmor) ? naturalArmor : 0f;
		    if (wornApparel != null) {
			foreach(var apparel in wornApparel) {
			    if (apparel.def.apparel.CoversBodyPart(part)) {
				armorValue += apparel.PartialStat(stat, part);
			    }
			}
		    }
		    armorCache[part] = armorValue;
		}
	    }
	}

	private void TryDrawOverallArmor(Dictionary<BodyPartRecord, float> armorCache, ref float curY, float width, StatDef stat, string label, string unit)
	{
	    Rect rect = new Rect(0f, curY, width, _standardLineHeight);
	    string text = "";
	    float averageArmor = 0;
	    float bodyCoverage = 0;
	    foreach (var bodyPartValue in armorCache)
	    {
		BodyPartRecord part = bodyPartValue.Key;
		float armorValue = bodyPartValue.Value;
		averageArmor += armorValue * part.coverage;
		bodyCoverage += part.coverage;
		text += part.LabelCap + ": ";
		text += formatArmorValue(armorValue, unit) + "\n";
            }
	    averageArmor /= bodyCoverage;
	    
	    TooltipHandler.TipRegion(rect, text);

	    Widgets.Label(rect, label.Truncate(200f, null));
	    rect.xMin += 200;
	    Widgets.Label(rect, formatArmorValue(averageArmor, unit));
	    curY += _standardLineHeight;
        }

        private void InterfaceDropHaul(Thing t)
        {
            if (SelPawnForGear.HoldTrackerIsHeld(t))
                SelPawnForGear.HoldTrackerForget(t);
            ThingWithComps thingWithComps = t as ThingWithComps;
            Apparel apparel = t as Apparel;
            if (apparel != null && SelPawnForGear.apparel != null && SelPawnForGear.apparel.WornApparel.Contains(apparel))
            {
                Job job = JobMaker.MakeJob(JobDefOf.RemoveApparel, apparel);
                job.haulDroppedApparel = true;
                SelPawnForGear.jobs.TryTakeOrderedJob(job);
            }
            else if (thingWithComps != null && SelPawnForGear.equipment != null && SelPawnForGear.equipment.AllEquipmentListForReading.Contains(thingWithComps))
            {
                SelPawnForGear.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.DropEquipment, thingWithComps));
            }
            else if (!t.def.destroyOnDrop)
            {
                Thing thing;
                SelPawn.inventory.innerContainer.TryDrop(t, SelPawn.Position, SelPawn.Map, ThingPlaceMode.Near, out thing);
            }
        }

        [Compatibility.Multiplayer.SyncMethod(syncContext = 2)] // 2 is map selected
        private void SyncedInterfaceIngest(Thing t) => InterfaceIngest(t);

        [Compatibility.Multiplayer.SyncMethod(syncContext = 2)]
        private void SyncedInterfaceDropHaul(Thing t) => InterfaceDropHaul(t);

        private new void InterfaceIngest(Thing t)
        {
            Job job = JobMaker.MakeJob(JobDefOf.Ingest, t);
            job.count = Mathf.Min(t.stackCount, t.def.ingestible.maxNumToIngestAtOnce);
            job.count = Mathf.Min(job.count, FoodUtility.WillIngestStackCountOf(SelPawnForGear, t.def, t.GetStatValue(StatDefOf.Nutrition, true)));
            SelPawnForGear.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        private bool ShouldShowOverallArmorCE(Pawn p)
        {
            return p.RaceProps.Humanlike || ShouldShowApparel(p) || p.GetStatValue(StatDefOf.ArmorRating_Sharp, true) > 0f || p.GetStatValue(StatDefOf.ArmorRating_Blunt, true) > 0f || p.GetStatValue(StatDefOf.ArmorRating_Heat, true) > 0f;
        }

        private string formatArmorValue(float value, string unit)
        {
            var asPercent = unit.Equals("%");
            if (asPercent)
            {
                value *= 100f;
            }
            return value.ToStringByStyle(asPercent ? ToStringStyle.FloatMaxOne : ToStringStyle.FloatMaxTwo) + unit;
        }

        [Compatibility.Multiplayer.SyncMethod(syncContext = 2)]
        private void SyncedInterfaceDrop(Thing thing) => InterfaceDrop(thing);

        [Compatibility.Multiplayer.SyncMethod]
        private static void SyncedTryTransferEquipmentToContainer(Pawn p) 
            => p.equipment.TryTransferEquipmentToContainer(p.equipment.Primary, p.inventory.innerContainer);

        [Compatibility.Multiplayer.SyncMethod]
        private static void SyncedTrySwitchToWeapon(CompInventory compInventory, ThingWithComps eq) 
            => compInventory.TrySwitchToWeapon(eq);

        [Compatibility.Multiplayer.SyncMethod]
        private static void SyncedReloadApparel(Pawn p, Apparel apparel, Thing thing) 
            => p.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Reload, apparel, thing));

        [Compatibility.Multiplayer.SyncMethod]
        private static void SyncedHoldTrackerForget(Pawn p, Thing thing) 
            => p.HoldTrackerForget(thing);

        [Compatibility.Multiplayer.SyncMethod]
        private static void SyncedAddLoadout(Pawn p)
        {
            Loadout loadout = p.GenerateLoadoutFromPawn();
            LoadoutManager.AddLoadout(loadout);
            p.SetLoadout(loadout);

            if (Compatibility.Multiplayer.IsExecutingCommandsIssuedBySelf)
                // Opening this window is the same way as if from the assign tab so should be correct.
                Find.WindowStack.Add(new Dialog_ManageLoadouts(p.GetLoadout()));
        }

        #endregion Methods
    }
}
