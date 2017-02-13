using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public enum SourceSelection
    {
        Ranged,
        Melee,
        Ammo,
        Minified,
        All
    }

    [StaticConstructorOnStartup]
    public class Dialog_ManageLoadouts : Window
    {
        #region Fields

        private static Texture2D
            _arrowBottom = ContentFinder<Texture2D>.Get("UI/Icons/arrowBottom"),
            _arrowDown = ContentFinder<Texture2D>.Get("UI/Icons/arrowDown"),
            _arrowTop = ContentFinder<Texture2D>.Get("UI/Icons/arrowTop"),
            _arrowUp = ContentFinder<Texture2D>.Get("UI/Icons/arrowUp"),
            _darkBackground = SolidColorMaterials.NewSolidColorTexture(0f, 0f, 0f, .2f),
            _iconEdit = ContentFinder<Texture2D>.Get("UI/Icons/edit"),
            _iconClear = ContentFinder<Texture2D>.Get("UI/Icons/clear"),
            _iconAmmo = ContentFinder<Texture2D>.Get("UI/Icons/ammo"),
            _iconRanged = ContentFinder<Texture2D>.Get("UI/Icons/ranged"),
            _iconMelee = ContentFinder<Texture2D>.Get("UI/Icons/melee"),
            _iconMinified = ContentFinder<Texture2D>.Get("UI/Icons/minified"),
            _iconAll = ContentFinder<Texture2D>.Get("UI/Icons/all"),
            _iconAmmoAdd = ContentFinder<Texture2D>.Get("UI/Icons/ammoAdd"),
            _iconSearch = ContentFinder<Texture2D>.Get("UI/Icons/search"),
            _iconMove = ContentFinder<Texture2D>.Get("UI/Icons/move");

        private static Regex validNameRegex = new Regex("^[a-zA-Z0-9 '\\-]*$");
        private Vector2 _availableScrollPosition = Vector2.zero;
        private float _barHeight = 24f;
        private Vector2 _countFieldSize = new Vector2(40f, 24f);
        private Loadout _currentLoadout;
        private LoadoutSlot _draggedSlot;
        private bool _dragging;
        private string _filter = "";
        private float _iconSize = 16f;
        private float _margin = 6f;
        private float _rowHeight = 30f;
        private Vector2 _slotScrollPosition = Vector2.zero;
        private List<ThingDef> _source;
        private SourceSelection _sourceType = SourceSelection.Ranged;
        private float _topAreaHeight = 30f;

        #endregion Fields

        #region Constructors

        public Dialog_ManageLoadouts(Loadout loadout)
        {
            CurrentLoadout = loadout;
            SetSource(SourceSelection.Ranged);
            doCloseX = true;
            closeOnClickedOutside = true;
            closeOnEscapeKey = true;
            Utility_Loadouts.UpdateColonistCapacities();
        }

        #endregion Constructors

        #region Properties

        public Loadout CurrentLoadout
        {
            get
            {
                return _currentLoadout;
            }
            set
            {
                _currentLoadout = value;
            }
        }

        public LoadoutSlot Dragging
        {
            get
            {
                if (_dragging)
                    return _draggedSlot;
                return null;
            }
            set
            {
                if (value == null)
                    _dragging = false;
                else
                    _dragging = true;
                _draggedSlot = value;
            }
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(700, 700);
            }
        }

        #endregion Properties

        #region Methods

        public override void DoWindowContents(Rect canvas)
        {
            // fix weird zooming bug
            Text.Font = GameFont.Small;

            // SET UP RECTS
            // top buttons
            Rect selectRect = new Rect(0f, 0f, canvas.width * .2f, _topAreaHeight);
            Rect newRect = new Rect(selectRect.xMax + _margin, 0f, canvas.width * .2f, _topAreaHeight);
            Rect deleteRect = new Rect(newRect.xMax + _margin, 0f, canvas.width * .2f, _topAreaHeight);

            // main areas
            Rect nameRect = new Rect(
                0f,
                _topAreaHeight + _margin * 2,
                (canvas.width - _margin) / 2f,
                24f);

            Rect slotListRect = new Rect(
                0f,
                nameRect.yMax + _margin,
                (canvas.width - _margin) / 2f,
                canvas.height - _topAreaHeight - nameRect.height - _barHeight * 2 - _margin * 5);

            Rect weightBarRect = new Rect(slotListRect.xMin, slotListRect.yMax + _margin, slotListRect.width, _barHeight);
            Rect bulkBarRect = new Rect(weightBarRect.xMin, weightBarRect.yMax + _margin, weightBarRect.width, _barHeight);

            Rect sourceButtonRect = new Rect(
                slotListRect.xMax + _margin,
                _topAreaHeight + _margin * 2,
                (canvas.width - _margin) / 2f,
                24f);

            Rect selectionRect = new Rect(
                slotListRect.xMax + _margin,
                sourceButtonRect.yMax + _margin,
                (canvas.width - _margin) / 2f,
                canvas.height - 24f - _topAreaHeight - _margin * 3);

            List<Loadout> loadouts = LoadoutManager.Loadouts;

            // DRAW CONTENTS
            // buttons
            // select loadout
            if (Widgets.ButtonText(selectRect, "CE_SelectLoadout".Translate()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();

                if (loadouts.Count == 0)
                    options.Add(new FloatMenuOption("CE_NoLoadouts".Translate(), null));
                else
                {
                    for (int i = 0; i < loadouts.Count; i++)
                    {
                        int local_i = i;
                        options.Add(new FloatMenuOption(loadouts[i].LabelCap, delegate
                        { CurrentLoadout = loadouts[local_i]; }));
                    }
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }
            // create loadout
            if (Widgets.ButtonText(newRect, "CE_NewLoadout".Translate()))
            {
                Loadout loadout = new Loadout();
                LoadoutManager.AddLoadout(loadout);
                CurrentLoadout = loadout;
            }
            // delete loadout
            if (loadouts.Any(l => l.canBeDeleted) && Widgets.ButtonText(deleteRect, "CE_DeleteLoadout".Translate()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();

                for (int i = 0; i < loadouts.Count; i++)
                {
                    int local_i = i;

                    // don't allow deleting the default loadout
                    if (!loadouts[i].canBeDeleted)
                        continue;
                    options.Add(new FloatMenuOption(loadouts[i].LabelCap,
                        delegate
                        {
                            if (CurrentLoadout == loadouts[local_i])
                                CurrentLoadout = null;
                            loadouts.Remove(loadouts[local_i]);
                        }));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }

            // draw notification if no loadout selected
            if (CurrentLoadout == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.grey;
                Widgets.Label(canvas, "CE_NoLoadoutSelected".Translate());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;

                // and stop further drawing
                return;
            }

            // name
            DrawNameField(nameRect);

            // source selection
            DrawSourceSelection(sourceButtonRect);

            // selection area
            DrawSlotSelection(selectionRect);

            // current slots
            DrawSlotList(slotListRect);

            // bars
            if (CurrentLoadout != null)
            {
                Utility_Loadouts.DrawBar(weightBarRect, CurrentLoadout.Weight, Utility_Loadouts.medianWeightCapacity, "CE_Weight".Translate(), CurrentLoadout.GetWeightTip());
                Utility_Loadouts.DrawBar(bulkBarRect, CurrentLoadout.Bulk, Utility_Loadouts.medianBulkCapacity, "CE_Bulk".Translate(), CurrentLoadout.GetBulkTip());
            }

            // done!
        }

        public void DrawSourceSelection(Rect canvas)
        {
            Rect button = new Rect(canvas.xMin, canvas.yMin + (canvas.height - 24f) / 2f, 24f, 24f);

            // Ranged weapons
            GUI.color = _sourceType == SourceSelection.Ranged ? GenUI.MouseoverColor : Color.white;
            if (Widgets.ButtonImage(button, _iconRanged))
                SetSource(SourceSelection.Ranged);
            TooltipHandler.TipRegion(button, "CE_SourceRangedTip".Translate());
            button.x += 24f + _margin;

            // Melee weapons
            GUI.color = _sourceType == SourceSelection.Melee ? GenUI.MouseoverColor : Color.white;
            if (Widgets.ButtonImage(button, _iconMelee))
                SetSource(SourceSelection.Melee);
            TooltipHandler.TipRegion(button, "CE_SourceMeleeTip".Translate());
            button.x += 24f + _margin;

            // Ammo
            GUI.color = _sourceType == SourceSelection.Ammo ? GenUI.MouseoverColor : Color.white;
            if (Widgets.ButtonImage(button, _iconAmmo))
                SetSource(SourceSelection.Ammo);
            TooltipHandler.TipRegion(button, "CE_SourceAmmoTip".Translate());
            button.x += 24f + _margin;
            
            // Minified
            GUI.color = _sourceType == SourceSelection.Ammo ? GenUI.MouseoverColor : Color.white;
            if (Widgets.ButtonImage(button, _iconMinified))
            	SetSource(SourceSelection.Minified);
            TooltipHandler.TipRegion(button, "CE_SourceMinifiedTip".Translate());
            button.x += 24f + _margin;

            // All
            GUI.color = _sourceType == SourceSelection.All ? GenUI.MouseoverColor : Color.white;
            if (Widgets.ButtonImage(button, _iconAll))
                SetSource(SourceSelection.All);
            TooltipHandler.TipRegion(button, "CE_SourceAllTip".Translate());

            // filter input field
            Rect filter = new Rect(canvas.xMax - 75f, canvas.yMin + (canvas.height - 24f) / 2f, 75f, 24f);
            DrawFilterField(filter);
            TooltipHandler.TipRegion(filter, "CE_SourceFilterTip".Translate());

            // search icon
            button.x = filter.xMin - _margin * 2 - _iconSize;
            GUI.DrawTexture(button, _iconSearch);
            TooltipHandler.TipRegion(button, "CE_SourceFilterTip".Translate());

            // reset color
            GUI.color = Color.white;
        }

        public void FilterSource(string filter)
        {
            // reset source
            SetSource(_sourceType, true);

            // filter
            _source = _source.Where(td => td.label.ToUpperInvariant().Contains(_filter.ToUpperInvariant())).ToList();
        }

        public void SetSource(SourceSelection source, bool preserveFilter = false)
        {
            _source = DefDatabase<ThingDef>.AllDefsListForReading;
            if (!preserveFilter)
                _filter = "";

            switch (source)
            {
                case SourceSelection.Ranged:
                    _source = _source.Where(td => td.IsRangedWeapon && !td.menuHidden).ToList();
                    _sourceType = SourceSelection.Ranged;
                    break;

                case SourceSelection.Melee:
                    _source = _source.Where(td => td.IsMeleeWeapon && !td.menuHidden).ToList();
                    _sourceType = SourceSelection.Melee;
                    break;

                case SourceSelection.Ammo:
                    _source = _source.Where(td => td is AmmoDef).ToList();
                    _sourceType = SourceSelection.Ammo;
                    break;
                
                case SourceSelection.Minified:
                    _source = _source.Where(td => td.Minifiable).ToList();
                    _sourceType = SourceSelection.Minified;
                    break;

                case SourceSelection.All:
                default:
                    _source = _source.Where(td => td.alwaysHaulable && td.thingClass != typeof(Corpse)).ToList();
                    _sourceType = SourceSelection.All;
                    break;
            }

            if (!_source.NullOrEmpty())
                _source = _source.OrderBy(td => td.label).ToList();
        }

        private void DrawCountField(Rect canvas, LoadoutSlot slot)
        {
            if (slot == null)
                return;
            string count = GUI.TextField(canvas, slot.Count.ToString());
            TooltipHandler.TipRegion(canvas, "CE_CountFieldTip".Translate(slot.Count));
            int countInt;
            if (int.TryParse(count, out countInt))
            {
                slot.Count = countInt;
            }
        }

        private void DrawFilterField(Rect canvas)
        {
            string filter = GUI.TextField(canvas, _filter);
            if (filter != _filter)
            {
                _filter = filter;
                FilterSource(_filter);
            }
        }

        private void DrawNameField(Rect canvas)
        {
            string label = GUI.TextField(canvas, CurrentLoadout.label);
            if (validNameRegex.IsMatch(label))
            {
                CurrentLoadout.label = label;
            }
        }

        private void DrawSlot(Rect row, LoadoutSlot slot, bool slotDraggable = true)
        {
            // set up rects
            // dragging handle (square) | label (fill) | count (50px) | delete (iconSize)
            Rect draggingHandle = new Rect(row);
            draggingHandle.width = row.height;

            Rect labelRect = new Rect(row);
            if (slotDraggable)
                labelRect.xMin = draggingHandle.xMax;
            labelRect.xMax = row.xMax - _countFieldSize.x - _iconSize - 2 * _margin;

            Rect countRect = new Rect(
                row.xMax - _countFieldSize.x - _iconSize - 2 * _margin,
                row.yMin + (row.height - _countFieldSize.y) / 2f,
                _countFieldSize.x,
                _countFieldSize.y);

            Rect ammoRect = new Rect(
                countRect.xMin - _iconSize - _margin,
                row.yMin + (row.height - _iconSize) / 2f,
                _iconSize, _iconSize);

            Rect deleteRect = new Rect(countRect.xMax + _margin, row.yMin + (row.height - _iconSize) / 2f, _iconSize, _iconSize);

            // dragging on dragHandle
            if (slotDraggable)
            {
                TooltipHandler.TipRegion(draggingHandle, "CE_DragToReorder".Translate());
                GUI.DrawTexture(draggingHandle, _iconMove);

                if (Mouse.IsOver(draggingHandle) && Input.GetMouseButtonDown(0))
                    Dragging = slot;
            }

            // interactions (main row rect)
            if (!Mouse.IsOver(deleteRect))
            {
                Widgets.DrawHighlightIfMouseover(row);
                TooltipHandler.TipRegion(row, slot.Def.GetWeightAndBulkTip(slot.Count));
            }

            // label
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, slot.Def.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;

            // easy ammo adder, ranged weapons only
            if (slot.Def.IsRangedWeapon)
            {
                // make sure there's an ammoset defined
                AmmoSetDef ammoSet = ((slot.Def.GetCompProperties<CompProperties_AmmoUser>() == null) ? null : slot.Def.GetCompProperties<CompProperties_AmmoUser>().ammoSet);

                bool? temp = !((((ammoSet == null) ? null : ammoSet.ammoTypes)).NullOrEmpty());

                if (temp ?? false)
                {
                    if (Widgets.ButtonImage(ammoRect, _iconAmmoAdd))
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();

                        foreach (ThingDef ammo in ((ammoSet == null) ? null : ammoSet.ammoTypes))
                        {
                            options.Add(new FloatMenuOption(ammo.LabelCap, delegate
                            {
                                CurrentLoadout.AddSlot(new LoadoutSlot(ammo));
                            }));
                        }

                        Find.WindowStack.Add(new FloatMenu(options, "CE_AddAmmoFor".Translate(slot.Def.LabelCap)));
                    }
                }
            }

            // count
            DrawCountField(countRect, slot);

            // delete
            if (Mouse.IsOver(deleteRect))
                GUI.DrawTexture(row, TexUI.HighlightTex);
            if (Widgets.ButtonImage(deleteRect, _iconClear))
                CurrentLoadout.RemoveSlot(slot);
            TooltipHandler.TipRegion(deleteRect, "CE_DeleteFilter".Translate());
        }

        private void DrawSlotList(Rect canvas)
        {
            // set up content canvas
            Rect viewRect = new Rect(0f, 0f, canvas.width, _rowHeight * CurrentLoadout.SlotCount + 1);

            // create some extra height if we're dragging
            if (Dragging != null)
                viewRect.height += _rowHeight;

            // leave room for scrollbar if necessary
            if (viewRect.height > canvas.height)
                viewRect.width -= 16f;

            // darken whole area
            GUI.DrawTexture(canvas, _darkBackground);

            Widgets.BeginScrollView(canvas, ref _slotScrollPosition, viewRect);
            int i = 0;
            float curY = 0f;
            for (; i < CurrentLoadout.SlotCount; i++)
            {
                // create row rect
                Rect row = new Rect(0f, curY, viewRect.width, _rowHeight);
                curY += _rowHeight;

                // if we're dragging, and currently on this row, and this row is not the row being dragged - draw a ghost of the slot here
                if (Dragging != null && Mouse.IsOver(row) && Dragging != CurrentLoadout.Slots[i])
                {
                    // draw ghost
                    GUI.color = new Color(.7f, .7f, .7f, .5f);
                    DrawSlot(row, Dragging);
                    GUI.color = Color.white;

                    // catch mouseUp
                    if (Input.GetMouseButtonUp(0))
                    {
                        CurrentLoadout.MoveSlot(Dragging, i);
                        Dragging = null;
                    }

                    // ofset further slots down
                    row.y += _rowHeight;
                    curY += _rowHeight;
                }

                // alternate row background
                if (i % 2 == 0)
                    GUI.DrawTexture(row, _darkBackground);

                // draw the slot - grey out if draggin this, but only when dragged over somewhere else
                if (Dragging == CurrentLoadout.Slots[i] && !Mouse.IsOver(row))
                    GUI.color = new Color(.6f, .6f, .6f, .4f);
                DrawSlot(row, CurrentLoadout.Slots[i], CurrentLoadout.SlotCount > 1);
                GUI.color = Color.white;
            }

            // if we're dragging, create an extra invisible row to allow moving stuff to the bottom
            if (Dragging != null)
            {
                Rect row = new Rect(0f, curY, viewRect.width, _rowHeight);

                if (Mouse.IsOver(row))
                {
                    // draw ghost
                    GUI.color = new Color(.7f, .7f, .7f, .5f);
                    DrawSlot(row, Dragging);
                    GUI.color = Color.white;

                    // catch mouseUp
                    if (Input.GetMouseButtonUp(0))
                    {
                        CurrentLoadout.MoveSlot(Dragging, CurrentLoadout.Slots.Count - 1);
                        Dragging = null;
                    }
                }
            }

            // cancel drag when mouse leaves the area, or on mouseup.
            if (!Mouse.IsOver(viewRect) || Input.GetMouseButtonUp(0))
                Dragging = null;

            Widgets.EndScrollView();
        }

        private void DrawSlotSelection(Rect canvas)
        {
            GUI.DrawTexture(canvas, _darkBackground);

            if (_source.NullOrEmpty())
                return;

            Rect viewRect = new Rect(canvas);
            viewRect.width -= 16f;
            viewRect.height = _source.Count * _rowHeight;

            Widgets.BeginScrollView(canvas, ref _availableScrollPosition, viewRect.AtZero());
            int startRow = (int)Math.Floor((decimal)(_availableScrollPosition.y / _rowHeight));
            startRow = (startRow < 0) ? 0 : startRow;
            int endRow = startRow + (int)(Math.Ceiling((decimal)(canvas.height / _rowHeight)));
            endRow = (endRow > _source.Count) ? _source.Count : endRow;
            for (int i = startRow; i < endRow; i++)
            {
                // gray out weapons not in stock
                Color baseColor = GUI.color;
                if (Find.VisibleMap.listerThings.AllThings.FindAll(x => x.GetInnerIfMinified().def == _source[i] && !x.def.Minifiable).Count <= 0)
                    GUI.color = Color.gray;

                Rect row = new Rect(0f, i * _rowHeight, canvas.width, _rowHeight);
                Rect labelRect = new Rect(row);
                TooltipHandler.TipRegion(row, _source[i].GetWeightAndBulkTip());

                labelRect.xMin += _margin;
                if (i % 2 == 0)
                    GUI.DrawTexture(row, _darkBackground);

                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, _source[i].LabelCap);
                Text.Anchor = TextAnchor.UpperLeft;

                Widgets.DrawHighlightIfMouseover(row);
                if (Widgets.ButtonInvisible(row))
                {
                    LoadoutSlot slot = new LoadoutSlot(_source[i], 1);
                    CurrentLoadout.AddSlot(slot);
                }
                // revert to original color
                GUI.color = baseColor;
            }
            Widgets.EndScrollView();
        }

        #endregion Methods
    }
}
