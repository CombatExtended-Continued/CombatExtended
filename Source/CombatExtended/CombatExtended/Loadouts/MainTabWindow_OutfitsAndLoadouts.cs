using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace CombatExtended;
public enum OutfitWindow
{
    Outfits,
    Loadouts
}

[StaticConstructorOnStartup]
public class MainTabWindow_OutfitsAndLoadouts : MainTabWindow_PawnList
{
    #region Fields

    private static Texture2D _iconClearForced = ContentFinder<Texture2D>.Get("UI/Icons/clear");
    private static Texture2D _iconEdit = ContentFinder<Texture2D>.Get("UI/Icons/edit");
    private float _buttonSize = 16f;
    private float _margin = 6f;
    private const float _spaceBetweenOutfitsAndDrugPolicy = 10f;
    private float _rowHeight = 30f;
    private float _topArea = 45f;

    #endregion Fields

    #region Properties

    public override Vector2 RequestedTabSize
    {
        get
        {
            return new Vector2(1010f, 65f + (float)PawnsCount * _rowHeight + 65f);
        }
    }

    #endregion Properties

    #region Methods

    public override void DoWindowContents(Rect canvas)
    {
        // fix weird zooming bug
        Text.Font = GameFont.Small;

        base.DoWindowContents(canvas);

        // available space
        Rect header = new Rect(165f + 24f + _margin, _topArea - _rowHeight, canvas.width - 165f - 24f - _margin - 16f, _rowHeight);

        // label + buttons for outfit
        Rect outfitRect = new Rect(header.xMin,
                                   header.yMin,
                                   header.width * (1f / 4f) + (_margin + _buttonSize) / 2f,
                                   header.height);
        Rect labelOutfitRect = new Rect(outfitRect.xMin,
                                        outfitRect.yMin,
                                        outfitRect.width - _margin * 3 - _buttonSize * 2,
                                        outfitRect.height)
        .ContractedBy(_margin / 2f);
        Rect editOutfitRect = new Rect(labelOutfitRect.xMax + _margin,
                                       outfitRect.yMin + ((outfitRect.height - _buttonSize) / 2),
                                       _buttonSize,
                                       _buttonSize);
        Rect forcedOutfitRect = new Rect(labelOutfitRect.xMax + _buttonSize + _margin * 2,
                                         outfitRect.yMin + ((outfitRect.height - _buttonSize) / 2),
                                         _buttonSize,
                                         _buttonSize);

        // label + button for drugs
        Rect drugRect = new Rect(outfitRect.xMax,
                                 header.yMin,
                                 header.width * (1f / 4f) - (_margin + _buttonSize) / 2f,
                                 header.height);
        Rect labelDrugRect = new Rect(drugRect.xMin,
                                      drugRect.yMin,
                                      drugRect.width - _margin * 2 - _buttonSize,
                                      drugRect.height)
        .ContractedBy(_margin / 2f);
        Rect editDrugRect = new Rect(labelDrugRect.xMax + _margin,
                                     drugRect.yMin + ((drugRect.height - _buttonSize) / 2),
                                     _buttonSize,
                                     _buttonSize);

        // label + button for loadout
        Rect loadoutRect = new Rect(drugRect.xMax,
                                    header.yMin,
                                    header.width * (1f / 4f) - (_margin + _buttonSize) / 2f,
                                    header.height);
        Rect labelLoadoutRect = new Rect(loadoutRect.xMin,
                                         loadoutRect.yMin,
                                         loadoutRect.width - _margin * 2 - _buttonSize,
                                         loadoutRect.height)
        .ContractedBy(_margin / 2f);
        Rect editLoadoutRect = new Rect(labelLoadoutRect.xMax + _margin,
                                        loadoutRect.yMin + ((loadoutRect.height - _buttonSize) / 2),
                                        _buttonSize,
                                        _buttonSize);

        // weight + bulk indicators
        Rect weightRect = new Rect(loadoutRect.xMax, header.yMin, header.width * (1f / 8f) - _margin, header.height).ContractedBy(_margin / 2f);
        Rect bulkRect = new Rect(weightRect.xMax + _margin, header.yMin, header.width * (1f / 8f) - _margin, header.height).ContractedBy(_margin / 2f);

        // draw headers
        Text.Anchor = TextAnchor.LowerCenter;
        Widgets.Label(labelOutfitRect, "CurrentOutfit".Translate());

        TooltipHandler.TipRegion(editOutfitRect, "CE_EditX".Translate("CE_Outfits".Translate()));
        if (Widgets.ButtonImage(editOutfitRect, _iconEdit))
        {
            Find.WindowStack.Add(new Dialog_ManageOutfits(null));
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Outfits, KnowledgeAmount.Total);
        }

        Widgets.Label(labelDrugRect, "CurrentDrugPolicies".Translate());
        TooltipHandler.TipRegion(editDrugRect, "ManageDrugPolicies".Translate("ButtonAssignDrugs"));
        if (Widgets.ButtonImage(editDrugRect, _iconEdit))
        {
            Find.WindowStack.Add(new Dialog_ManageDrugPolicies(null));
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.DrugPolicies, KnowledgeAmount.Total);
        }

        Widgets.Label(labelLoadoutRect, "CE_CurrentLoadout".Translate());
        TooltipHandler.TipRegion(editLoadoutRect, "CE_EditX".Translate("CE_Loadouts".Translate()));
        if (Widgets.ButtonImage(editLoadoutRect, _iconEdit))
        {
            Find.WindowStack.Add(new Dialog_ManageLoadouts(null));
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_Loadouts, KnowledgeAmount.Total);
        }
        Widgets.Label(weightRect, "CE_Weight".Translate());
        Widgets.Label(bulkRect, "CE_Bulk".Translate());
        Text.Anchor = TextAnchor.UpperLeft;

        // draw the rows
        canvas.yMin += 45f;
        DrawRows(canvas);
    }

    protected override void DrawPawnRow(Rect rect, Pawn p)
    {
        // available space for row
        Rect rowRect = new Rect(rect.x + 165f, rect.y, rect.width - 165f, rect.height);

        // response button rect
        Vector2 responsePos = new Vector2(rowRect.xMin, rowRect.yMin + (rowRect.height - 24f) / 2f);

        // offset rest of row for that button, so we don't have to mess with all the other rect calculations
        rowRect.xMin += 24f + _margin;

        // label + buttons for outfit
        Rect outfitRect = new Rect(rowRect.xMin,
                                   rowRect.yMin,
                                   rowRect.width * (1f / 4f) + (_margin + _buttonSize) / 2f,
                                   rowRect.height);

        Rect labelOutfitRect = new Rect(outfitRect.xMin,
                                        outfitRect.yMin,
                                        outfitRect.width - _margin * 3 - _buttonSize * 2,
                                        outfitRect.height)
        .ContractedBy(_margin / 2f);
        Rect editOutfitRect = new Rect(labelOutfitRect.xMax + _margin,
                                       outfitRect.yMin + ((outfitRect.height - _buttonSize) / 2),
                                       _buttonSize,
                                       _buttonSize);
        Rect forcedOutfitRect = new Rect(labelOutfitRect.xMax + _buttonSize + _margin * 2,
                                         outfitRect.yMin + ((outfitRect.height - _buttonSize) / 2),
                                         _buttonSize,
                                         _buttonSize);

        // drucg policy
        Rect drugRect = new Rect(outfitRect.xMax,
                                 rowRect.yMin,
                                 rowRect.width * (1f / 4f) - (_margin + _buttonSize) / 2f,
                                 rowRect.height);
        Rect labelDrugRect = new Rect(drugRect.xMin,
                                      drugRect.yMin,
                                      drugRect.width - _margin * 2 - _buttonSize,
                                      drugRect.height)
        .ContractedBy(_margin / 2f);
        Rect editDrugRect = new Rect(labelDrugRect.xMax + _margin,
                                     drugRect.yMin + ((drugRect.height - _buttonSize) / 2),
                                     _buttonSize,
                                     _buttonSize);

        // label + button for loadout
        Rect loadoutRect = new Rect(drugRect.xMax,
                                    rowRect.yMin,
                                    rowRect.width * (1f / 4f) - (_margin + _buttonSize) / 2f,
                                    rowRect.height);
        Rect labelLoadoutRect = new Rect(loadoutRect.xMin,
                                         loadoutRect.yMin,
                                         loadoutRect.width - _margin * 3 - _buttonSize * 2,
                                         loadoutRect.height)
        .ContractedBy(_margin / 2f);
        Rect editLoadoutRect = new Rect(labelLoadoutRect.xMax + _margin,
                                        loadoutRect.yMin + ((loadoutRect.height - _buttonSize) / 2),
                                        _buttonSize,
                                        _buttonSize);
        Rect forcedHoldRect = new Rect(labelLoadoutRect.xMax + _buttonSize + _margin * 2,
                                       loadoutRect.yMin + ((loadoutRect.height - _buttonSize) / 2),
                                       _buttonSize,
                                       _buttonSize);

        // fight or flight button
        HostilityResponseModeUtility.DrawResponseButton(responsePos, p);

        // weight + bulk indicators
        Rect weightRect = new Rect(loadoutRect.xMax, rowRect.yMin, rowRect.width * (1f / 8f) - _margin, rowRect.height).ContractedBy(_margin / 2f);
        Rect bulkRect = new Rect(weightRect.xMax + _margin, rowRect.yMin, rowRect.width * (1f / 8f) - _margin, rowRect.height).ContractedBy(_margin / 2f);

        // OUTFITS
        // main button
        if (Widgets.ButtonText(labelOutfitRect, p.outfits.CurrentOutfit.label, true, false))
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (Outfit current in Current.Game.outfitDatabase.AllOutfits)
            {
                // need to create a local copy for delegate
                Outfit localOut = current;
                options.Add(new FloatMenuOption(localOut.label, delegate
                {
                    p.outfits.CurrentOutfit = localOut;
                }, MenuOptionPriority.Default, null, null));
            }
            Find.WindowStack.Add(new FloatMenu(options, optionalTitle, false));
        }

        // edit button
        TooltipHandler.TipRegion(editOutfitRect, "CE_EditX".Translate("CE_outfit".Translate() + " " + p.outfits.CurrentOutfit.label));
        if (Widgets.ButtonImage(editOutfitRect, _iconEdit))
        {
            Text.Font = GameFont.Small;
            Find.WindowStack.Add(new Dialog_ManageOutfits(p.outfits.CurrentOutfit));
        }

        // clear forced button
        if (p.outfits.forcedHandler.SomethingIsForced)
        {
            TooltipHandler.TipRegion(forcedOutfitRect, "ClearForcedApparel".Translate());
            if (Widgets.ButtonImage(forcedOutfitRect, _iconClearForced))
            {
                p.outfits.forcedHandler.Reset();
            }
            TooltipHandler.TipRegion(forcedOutfitRect, new TipSignal(delegate
            {
                string text = "ForcedApparel".Translate() + ":\n";
                foreach (Apparel current2 in p.outfits.forcedHandler.ForcedApparel)
                {
                    text = text + "\n   " + current2.LabelCap;
                }
                return text;
            }, p.GetHashCode() * 612));
        }

        // DRUG POLICY
        // main button
        string textDrug = p.drugs.CurrentPolicy.label;
        if (p.story != null && p.story.traits != null)
        {
            Trait trait = p.story.traits.GetTrait(TraitDefOf.DrugDesire);
            if (trait != null)
            {
                textDrug = textDrug + " (" + trait.Label + ")";
            }
        }
        if (Widgets.ButtonText(labelDrugRect, textDrug, true, false, true))
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (DrugPolicy current in Current.Game.drugPolicyDatabase.AllPolicies)
            {
                DrugPolicy localAssignedDrugs = current;
                list.Add(new FloatMenuOption(current.label, delegate
                {
                    p.drugs.CurrentPolicy = localAssignedDrugs;
                }, MenuOptionPriority.Default, null, null, 0f, null));
            }
            Find.WindowStack.Add(new FloatMenu(list));
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.DrugPolicies, KnowledgeAmount.Total);
        }


        // edit button
        TooltipHandler.TipRegion(editDrugRect, "CE_EditX".Translate("CE_drugs".Translate() + " " + p.drugs.CurrentPolicy.label));
        if (Widgets.ButtonImage(editDrugRect, _iconEdit))
        {
            Text.Font = GameFont.Small;
            Find.WindowStack.Add(new Dialog_ManageDrugPolicies(p.drugs.CurrentPolicy));
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.DrugPolicies, KnowledgeAmount.Total);
        }

        // LOADOUTS
        // main button
        if (Widgets.ButtonText(labelLoadoutRect, p.GetLoadout().LabelCap, true, false))
        {
            LoadoutManager.SortLoadouts();
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (Loadout loadout in LoadoutManager.Loadouts)
            {
                // need to create a local copy for delegate
                Loadout localLoadout = loadout;
                options.Add(new FloatMenuOption(localLoadout.LabelCap, delegate
                {
                    p.SetLoadout(localLoadout);
                }, MenuOptionPriority.Default, null, null));
            }
            Find.WindowStack.Add(new FloatMenu(options, optionalTitle, false));
        }

        // edit button
        TooltipHandler.TipRegion(editLoadoutRect, "CE_EditX".Translate("CE_loadout".Translate() + " " + p.GetLoadout().LabelCap));
        if (Widgets.ButtonImage(editLoadoutRect, _iconEdit))
        {
            Find.WindowStack.Add(new Dialog_ManageLoadouts(p.GetLoadout()));
        }

        // clear forced held button
        if (p.HoldTrackerAnythingHeld())
        {
            TooltipHandler.TipRegion(forcedHoldRect, "ClearForcedApparel".Translate()); // "Clear forced" is sufficient and that's what this is at the moment.
            if (Widgets.ButtonImage(forcedHoldRect, _iconClearForced)) // also can re-use the icon for clearing forced at the moment.
            {
                p.HoldTrackerClear(); // yes this will also delete records that haven't been picked up and thus not shown to the player...
            }
            TooltipHandler.TipRegion(forcedHoldRect, new TipSignal(delegate
            {
                string text = "CE_ForcedHold".Translate() + ":\n";
                foreach (HoldRecord rec in LoadoutManager.GetHoldRecords(p))
                {
                    if (!rec.pickedUp)
                    {
                        continue;
                    }
                    text = text + "\n   " + rec.thingDef.LabelCap + " x" + rec.count;
                }
                return text;
            }, p.GetHashCode() * 613));
        }

        // STATUS BARS
        // fetch the comp
        CompInventory comp = p.TryGetComp<CompInventory>();

        if (comp != null)
        {
            Utility_Loadouts.DrawBar(bulkRect, comp.currentBulk, comp.capacityBulk, "", p.GetBulkTip());
            Utility_Loadouts.DrawBar(weightRect, comp.currentWeight, comp.capacityWeight, "", p.GetWeightTip());
        }
    }

    #endregion Methods
}