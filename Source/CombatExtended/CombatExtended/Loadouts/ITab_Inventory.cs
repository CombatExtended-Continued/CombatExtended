using System.Text.RegularExpressions;
using RimWorld;
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
        private const float _standardLineHeight = 22f;
        private static readonly Color _highlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Color _thingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        private Vector2 _scrollPosition = Vector2.zero;

        private float _scrollViewHeight;

        private static List<Thing> workingInvList = new List<Thing>();

        #endregion Fields

        #region Constructors

        public ITab_Inventory() : base()
        {
            size = new Vector2(432f, 550f);
        }


        #endregion Constructors

        #region Properties

        private bool CanControl
        {
            get
            {
                return SelPawnForGear.IsColonistPlayerControlled;
            }
        }

        private Pawn SelPawnForGear
        {
            get
            {
                if (SelPawn != null)
                {
                    return SelPawn;
                }
                Corpse corpse = SelThing as Corpse;
                if (corpse != null)
                {
                    return corpse.InnerPawn;
                }
                throw new InvalidOperationException("Gear tab on non-pawn non-corpse " + SelThing);
            }
        }

        #endregion Properties

        #region Methods

        protected override void FillTab()
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

                Utility_Loadouts.DrawBar(bulkRect, comp.currentBulk, comp.capacityBulk, "CE_Bulk".Translate(), SelPawn.GetBulkTip());
                Utility_Loadouts.DrawBar(weightRect, comp.currentWeight, comp.capacityWeight, "CE_Weight".Translate(), SelPawn.GetWeightTip());
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
            if (SelPawnForGear.apparel != null)
            {
                bool flag = false;
                TryDrawAverageArmor(ref num, viewRect.width, StatDefOf.ArmorRating_Blunt, "ArmorBlunt".Translate(), ref flag);
                TryDrawAverageArmor(ref num, viewRect.width, StatDefOf.ArmorRating_Sharp, "ArmorSharp".Translate(), ref flag);
                TryDrawAverageArmor(ref num, viewRect.width, StatDefOf.ArmorRating_Heat, "ArmorHeat".Translate(), ref flag);
            }
            if (SelPawnForGear.equipment != null)
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Equipment".Translate());
                foreach (ThingWithComps current in SelPawnForGear.equipment.AllEquipmentListForReading)
                {
                    DrawThingRow(ref num, viewRect.width, current);
                }
            }
            if (SelPawnForGear.apparel != null)
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Apparel".Translate());
                foreach (Apparel current2 in from ap in SelPawnForGear.apparel.WornApparel
                                             orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                             select ap)
                {
                    DrawThingRow(ref num, viewRect.width, current2);
                }
            }
            if (SelPawnForGear.inventory != null)
            {
                // get the loadout so we can make a decision to show a button.
                bool showMakeLoadout = false;
                Loadout curLoadout = SelPawnForGear.GetLoadout();
            	if (SelPawnForGear.IsColonist && (curLoadout == null || curLoadout.Slots.NullOrEmpty()) && (SelPawnForGear.inventory.innerContainer.Any() || SelPawnForGear.equipment?.Primary != null))
            		showMakeLoadout = true;
                	
            	if (showMakeLoadout) num+=3; // Make a little room for the button.

                float buttonY = num; // Could be accomplished with seperator being after the button...

                Widgets.ListSeparator(ref num, viewRect.width, "Inventory".Translate());

                // only offer this button if the pawn has no loadout or has the default loadout and there are things/equipment...
                if (showMakeLoadout)
                {
	            	Rect loadoutButtonRect = new Rect(viewRect.width / 2, buttonY, viewRect.width / 2, 26f); // button is half the available width...
	                if(Widgets.ButtonText(loadoutButtonRect, "Make Loadout"))
	                {
	                	Loadout loadout = SelPawnForGear.GenerateLoadoutFromPawn();
	                	LoadoutManager.AddLoadout(loadout);
	                	SelPawnForGear.SetLoadout(loadout);
	                	
	                	// UNDONE ideally we'd open the assign (MainTabWindow_OutfitsAndLoadouts) tab as if the user clicked on it here.
	                	// (ProfoundDarkness) But I have no idea how to do that just yet.  The attempts I made seem to put the RimWorld UI into a bit of a bad state.
	                	//                     ie opening the tab like the dialog below.
	                	//                    Need to understand how RimWorld switches tabs and see if something similar can be done here
	                	//                     (or just remove the unfinished marker).
	                	
	                	// Opening this window is the same way as if from the assign tab so should be correct.
	                	Find.WindowStack.Add(new Dialog_ManageLoadouts(SelPawnForGear.GetLoadout()));
	                	
	                }
                }

                workingInvList.Clear();
                workingInvList.AddRange(SelPawnForGear.inventory.innerContainer);
                for (int i = 0; i < workingInvList.Count; i++)
                {
                    DrawThingRow(ref num, viewRect.width, workingInvList[i].GetInnerIfMinified(), true);
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

        private void DrawThingRow(ref float y, float width, Thing thing, bool showDropButtonIfPrisoner = false)
        {

            Rect rect = new Rect(0f, y, width, _thingRowHeight);
            Widgets.InfoCardButton(rect.width - 24f, y, thing);
            rect.width -= 24f;
            if (CanControl || (SelPawnForGear.Faction == Faction.OfPlayer && SelPawnForGear.RaceProps.packAnimal) || (showDropButtonIfPrisoner && SelPawnForGear.IsPrisonerOfColony))
            {
                Rect dropRect = new Rect(rect.width - 24f, y, 24f, 24f);
                TooltipHandler.TipRegion(dropRect, "DropThing".Translate());
                if (Widgets.ButtonImage(dropRect, TexButton.Drop))
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    InterfaceDrop(thing);
                }
                rect.width -= 24f;
            }
            if (this.CanControl && thing.IngestibleNow && base.SelPawn.RaceProps.CanEverEat(thing))
            {
                Rect rect3 = new Rect(rect.width - 24f, y, 24f, 24f);
                string tipString = thing.def.ingestible.ingestCommandString.NullOrEmpty() ? "ConsumeThing".Translate(thing.LabelShort) : string.Format(thing.def.ingestible.ingestCommandString, thing.LabelShort);
                TooltipHandler.TipRegion(rect3, tipString);
                if (Widgets.ButtonImage(rect3, TexButton.Ingest))
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    this.InterfaceEatThis(thing);
                }
                rect.width -= 24f;
            }
            if (Mouse.IsOver(rect))
            {
                GUI.color = _highlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }

            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y, _thingIconSize, _thingIconSize), thing);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = _thingLabelColor;
            Rect thingLabelRect = new Rect(_thingLeftX, y, rect.width - _thingLeftX, _thingRowHeight);
            string thingLabel = thing.LabelCap;
            if ((thing is Apparel && SelPawnForGear.outfits != null && SelPawnForGear.outfits.forcedHandler.IsForced((Apparel)thing))
                || (SelPawnForGear.inventory != null && SelPawnForGear.HoldTrackerIsHeld(thing)))
            {
                thingLabel = thingLabel + ", " + "ApparelForcedLower".Translate();
            }
            Widgets.Label(thingLabelRect, thingLabel);
            y += _thingRowHeight;

            TooltipHandler.TipRegion(thingLabelRect, thing.GetWeightAndBulkTip());
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
                        if (compInventory != null)
                        {
                            FloatMenuOption equipOption;
                            string eqLabel = GenLabel.ThingLabel(eq.def, eq.Stuff, 1);
                            if (SelPawnForGear.equipment.AllEquipmentListForReading.Contains(eq) && SelPawnForGear.inventory != null)
                            {
                                equipOption = new FloatMenuOption("CE_PutAway".Translate(eqLabel),
                                    new Action(delegate
                                    {
                                        SelPawnForGear.equipment.TryTransferEquipmentToContainer(SelPawnForGear.equipment.Primary, SelPawnForGear.inventory.innerContainer);
                                    }));
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
                                	(SelPawnForGear.story != null && SelPawnForGear.story.WorkTagIsDisabled(WorkTags.Violent))
                                	? null
                                	: new Action(delegate
                                {
                                    compInventory.TrySwitchToWeapon(eq);
                                }));
                            }
                            floatOptionList.Add(equipOption);
                        }
                    }
                    // Drop option
                    Action dropApparel = delegate
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        InterfaceDrop(thing);
                    };
                    Action dropApparelHaul = delegate
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                        InterfaceDropHaul(thing);
                    };
                    if (this.CanControl && thing.IngestibleNow && base.SelPawn.RaceProps.CanEverEat(thing))
                    {
                        Action eatFood = delegate
                        {
                            SoundDefOf.Tick_High.PlayOneShotOnCamera();
                            InterfaceEatThis(thing);
                        };
                        string label = thing.def.ingestible.ingestCommandString.NullOrEmpty() ? "ConsumeThing".Translate(thing.LabelShort) : string.Format(thing.def.ingestible.ingestCommandString, thing.LabelShort);
                        floatOptionList.Add(new FloatMenuOption(label, eatFood));
                    }
                    floatOptionList.Add(new FloatMenuOption("DropThing".Translate(), dropApparel));
                    floatOptionList.Add(new FloatMenuOption("CE_DropThingHaul".Translate(), dropApparelHaul));
                    if (this.CanControl && SelPawnForGear.HoldTrackerIsHeld(thing))
                    {
                    	Action forgetHoldTracker = delegate
                    	{
                    		SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    		SelPawnForGear.HoldTrackerForget(thing);
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
        private void TryDrawAverageArmor(ref float curY, float width, StatDef stat, string label, ref bool separatorDrawn)
        {
            if (SelPawnForGear.RaceProps.body != BodyDefOf.Human)
            {
                return;
            }
            float num = 0f;
            List<Apparel> wornApparel = SelPawnForGear.apparel.WornApparel;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                num += Mathf.Clamp01(wornApparel[i].GetStatValue(stat, true)) * wornApparel[i].def.apparel.HumanBodyCoverage;
            }
            num = Mathf.Clamp01(num);
            if (num > 0.005f)
            {
                if (!separatorDrawn)
                {
                    separatorDrawn = true;
                    Widgets.ListSeparator(ref curY, width, "AverageArmor".Translate());
                }
                Rect rect = new Rect(0f, curY, width, curY + _standardLineHeight);
                Widgets.Label(rect, label);
                rect.xMin += 100f;
                Widgets.Label(rect, num.ToStringPercent());
                curY += _standardLineHeight;
            }
        }

        // RimWorld.ITab_Pawn_Gear
        private void TryDrawComfyTemperatureRange(ref float curY, float width)
        {
            if (SelPawnForGear.Dead)
            {
                return;
            }
            Rect rect = new Rect(0f, curY, width, _standardLineHeight);
            float statValue = SelPawnForGear.GetStatValue(StatDefOf.ComfyTemperatureMin, true);
            float statValue2 = SelPawnForGear.GetStatValue(StatDefOf.ComfyTemperatureMax, true);
            Widgets.Label(rect, string.Concat(new string[]
            {
        "ComfyTemperatureRange".Translate(),
        ": ",
        statValue.ToStringTemperature("F0"),
        " ~ ",
        statValue2.ToStringTemperature("F0")
            }));
            curY += _standardLineHeight;
        }
        // RimWorld.ITab_Pawn_Gear
        private void InterfaceDrop(Thing t)
        {
        	if (SelPawnForGear.HoldTrackerIsHeld(t))
	        	SelPawnForGear.HoldTrackerForget(t);
            ThingWithComps thingWithComps = t as ThingWithComps;
            Apparel apparel = t as Apparel;
            if (apparel != null && this.SelPawnForGear.apparel != null && this.SelPawnForGear.apparel.WornApparel.Contains(apparel))
            {
                this.SelPawnForGear.jobs.TryTakeOrderedJob(new Job(JobDefOf.RemoveApparel, apparel));
            }
            else if (thingWithComps != null && this.SelPawnForGear.equipment != null && this.SelPawnForGear.equipment.AllEquipmentListForReading.Contains(thingWithComps))
            {
                this.SelPawnForGear.jobs.TryTakeOrderedJob(new Job(JobDefOf.DropEquipment, thingWithComps));
            }
            else if (!t.def.destroyOnDrop)
            {
                Thing thing;
                Thing dropThing = t;
                if (t.def.Minifiable)
                	dropThing = this.SelPawnForGear.inventory.innerContainer.FirstOrDefault(x => x.GetInnerIfMinified().ThingID == t.ThingID);
                this.SelPawnForGear.inventory.innerContainer.TryDrop(dropThing, this.SelPawnForGear.Position, this.SelPawnForGear.Map, ThingPlaceMode.Near, out thing, null);
            }
        }

        private void InterfaceDropHaul(Thing t)
        {
        	if (SelPawnForGear.HoldTrackerIsHeld(t))
	        	SelPawnForGear.HoldTrackerForget(t);
            ThingWithComps thingWithComps = t as ThingWithComps;
            Apparel apparel = t as Apparel;
            if (apparel != null && this.SelPawnForGear.apparel != null && this.SelPawnForGear.apparel.WornApparel.Contains(apparel))
            {
                this.SelPawnForGear.jobs.TryTakeOrderedJob(new Job(JobDefOf.RemoveApparel, apparel) { haulDroppedApparel = true });
            }
            else if (thingWithComps != null && this.SelPawnForGear.equipment != null && this.SelPawnForGear.equipment.AllEquipmentListForReading.Contains(thingWithComps))
            {
                this.SelPawnForGear.jobs.TryTakeOrderedJob(new Job(JobDefOf.DropEquipment, thingWithComps));
            }
            else if (!t.def.destroyOnDrop)
            {
                Thing thing;
                SelPawn.inventory.innerContainer.TryDrop(t, SelPawn.Position, SelPawn.Map, ThingPlaceMode.Near, out thing);
            }
        }

        private void InterfaceEatThis(Thing t)
        {
            Job job = new Job(JobDefOf.Ingest, t);
            job.count = Mathf.Min(t.stackCount, t.def.ingestible.maxNumToIngestAtOnce);
            SelPawnForGear.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        #endregion Methods
    }
}