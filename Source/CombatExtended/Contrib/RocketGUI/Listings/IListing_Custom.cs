using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using GUILambda = System.Action<UnityEngine.Rect>;

namespace CombatExtended.RocketGUI
{
    public abstract class IListing_Custom
    {
        public const float ScrollViewWidthDelta = 25f;

        protected Vector2 margins = new Vector2(8, 4);

        protected float inXMin = 0f;

        protected float inXMax = 0f;

        protected float curYMin = 0f;

        protected float inYMin = 0f;

        protected float inYMax = 0f;

        protected float previousHeight = 0f;

        protected bool isOverflowing = false;

        protected Rect contentRect;

        private Rect inRect;

        private bool started = false;

        public readonly bool ScrollViewOnOverflow;

        public Vector2 ScrollPosition = Vector2.zero;

        public Color CollapsibleBGColor = Widgets.MenuSectionBGFillColor;

        public Color CollapsibleBGBorderColor = Widgets.MenuSectionBGBorderColor;

        public bool drawBorder = false;

        public bool drawBackground = false;

        protected struct RectSlice
        {
            public Rect inside;
            public Rect outside;

            public RectSlice(Rect inside, Rect outside)
            {
                this.outside = outside;
                this.inside = inside;
            }
        }

        protected virtual bool Overflowing
        {
            get => isOverflowing;
        }

        protected virtual float insideWidth
        {
            get => (inXMax - inXMin) - margins.x * 2f;
        }

        public virtual Vector2 Margins
        {
            get => this.margins;
            set => this.margins = value;
        }

        public Rect Rect
        {
            get => new Rect(inXMin, curYMin, inXMax - inXMin, inYMax - curYMin);
            set
            {
                this.inXMin = value.xMin;
                this.inXMax = value.xMax;
                this.curYMin = value.yMin;
                this.inYMin = value.yMin;
                this.inYMax = value.yMax;
            }
        }

        public IListing_Custom(bool scrollViewOnOverflow = true, bool drawBorder = false, bool drawBackground = false)
        {
            this.ScrollViewOnOverflow = scrollViewOnOverflow;
            this.drawBorder = drawBorder;
            this.drawBackground = drawBackground;
        }

        protected virtual void Begin(Rect inRect, bool scrollViewOnOverflow = true)
        {
            this.inRect = inRect;
            if (ScrollViewOnOverflow && started && inRect.height < previousHeight)
            {
                GUIUtility.ExecuteSafeGUIAction(() =>
                {
                    isOverflowing = true;
                    GUIUtility.StashGUIState();
                    GUI.color = Color.white;
                    contentRect = new Rect(0f, 0f, inRect.width - ScrollViewWidthDelta, previousHeight);
                    this.inYMin = contentRect.yMin;
                    this.Rect = contentRect;
                    Widgets.BeginScrollView(inRect, ref ScrollPosition, contentRect);
                    GUIUtility.RestoreGUIState();
                });
            }
            else
            {
                this.inYMin = inRect.yMin;
                this.Rect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            }
        }

        protected virtual void Start()
        {
            GUIUtility.StashGUIState();
            Text.Font = GameFont.Tiny;
            Text.CurFontStyle.fontStyle = FontStyle.Normal;
        }

        protected virtual void Label(TaggedString text, Color color, string tooltip = null, bool hightlightIfMouseOver = true, GameFont fontSize = GameFont.Tiny, FontStyle fontStyle = FontStyle.Normal, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                RectSlice slice = Slice(text.GetTextHeight(this.insideWidth));
                if (hightlightIfMouseOver)
                {
                    Widgets.DrawHighlightIfMouseover(slice.outside);
                }
                GUI.color = color;
                Text.Anchor = anchor;
                Text.Font = fontSize;
                Text.CurFontStyle.fontStyle = fontStyle;
                Widgets.Label(slice.inside, text);
                if (tooltip != null)
                {
                    TooltipHandler.TipRegion(slice.outside, tooltip);
                }
            });
        }

        protected virtual bool CheckboxLabeled(TaggedString text, ref bool checkOn, string tooltip = null, bool disabled = false, bool hightlightIfMouseOver = true, GameFont fontSize = GameFont.Tiny, FontStyle fontStyle = FontStyle.Normal)
        {
            bool changed = false;
            bool checkOnInt = checkOn;
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                Text.Font = fontSize;
                Text.CurFontStyle.fontStyle = fontStyle;
                RectSlice slice = Slice(text.GetTextHeight(insideWidth - 23f));
                if (hightlightIfMouseOver)
                {
                    Widgets.DrawHighlightIfMouseover(slice.outside);
                }
                GUIUtility.CheckBoxLabeled(slice.inside, text, ref checkOnInt, disabled: disabled, iconWidth: 23f, drawHighlightIfMouseover: false);
                if (tooltip != null)
                {
                    TooltipHandler.TipRegion(slice.outside, tooltip);
                }
            });
            if (checkOnInt != checkOn)
            {
                checkOn = checkOnInt;
                changed = true;
            }
            return changed;
        }

        protected virtual void Columns(float height, IEnumerable<GUILambda> lambdas, float gap = 5, bool useMargins = false, Action fallback = null)
        {
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                if (lambdas.Count() == 1)
                {
                    Lambda(height, lambdas.First(), useMargins, false, fallback);
                    return;
                }
                Rect rect = useMargins ? Slice(height).inside : Slice(height).outside;
                Rect[] columns = rect.Columns(lambdas.Count(), gap);
                int i = 0;
                foreach (GUILambda lambda in lambdas)
                {
                    GUIUtility.ExecuteSafeGUIAction(() =>
                    {
                        lambda(columns[i++]);
                    }, fallback);
                }
            });
        }

        protected virtual void DropDownMenu<T>(TaggedString text, T selection, Func<T, string> labelLambda, Action<T> selectedLambda, IEnumerable<T> options, bool disabled = false, GameFont fontSize = GameFont.Tiny, FontStyle fontStyle = FontStyle.Normal)
        {
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                string selectedText = labelLambda(selection);

                Text.Font = fontSize;
                Text.CurFontStyle.fontStyle = fontStyle;

                Rect rect = Slice(selectedText.GetTextHeight(insideWidth - 23f)).inside;
                Rect[] columns = rect.Columns(2, 5);

                Widgets.Label(columns[0], text);

                if (Widgets.ButtonText(columns[1], selectedText, active: !disabled))
                {
                    GUIUtility.DropDownMenu(labelLambda, selectedLambda, options);
                }
            });
        }

        protected virtual void Lambda(float height, GUILambda contentLambda, bool useMargins = false, bool hightlightIfMouseOver = true, Action fallback = null)
        {
            RectSlice slice = Slice(height);
            if (hightlightIfMouseOver)
            {
                Widgets.DrawHighlightIfMouseover(slice.outside);
            }
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                contentLambda(useMargins ? slice.inside : slice.outside);
            }, fallback);
        }

        protected virtual void Gap(float height = 9f)
        {
            Slice(height, includeMargins: false);
        }

        protected virtual void Line(float thickness)
        {            
            Widgets.DrawBoxSolid(!drawBorder ? this.Slice(thickness, includeMargins: true).inside : this.Slice(thickness, includeMargins: false).outside, this.CollapsibleBGBorderColor);            
        }

        protected virtual bool ButtonText(TaggedString text, bool disabled = false, bool drawBackground = false)
        {
            bool clicked = false;
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                Text.Font = GameFont.Small;
                Text.CurFontStyle.fontStyle = FontStyle.Normal;
                RectSlice slice = Slice(text.GetTextHeight(insideWidth) + 4, includeMargins: true);
                if (!drawBackground)
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    GUI.color = Mouse.IsOver(slice.inside) ? Color.white : Color.cyan;
                }
                clicked = Widgets.ButtonText(slice.inside, text, drawBackground);
            });
            return clicked;
        }

        public virtual void End(ref Rect inRect)
        {
            Gap(height: 5);
            GUI.color = this.CollapsibleBGBorderColor;
            if(drawBorder)
                Widgets.DrawBox(new Rect(inXMin, inYMin, inXMax - inXMin, curYMin - inYMin));

            started = true;
            previousHeight = Mathf.Abs(inYMin - curYMin);
            if (isOverflowing)
            {
                Widgets.EndScrollView();
                if (started && inRect.height < previousHeight)
                {
                    GUI.color = this.CollapsibleBGBorderColor;
                    Widgets.DrawBox(new Rect(inRect.xMin, inRect.yMin, inRect.width - 25f, 1));
                    Widgets.DrawBox(new Rect(inRect.xMin, inRect.yMax - 1, inRect.width - 25f, 1));
                }
                inRect.yMin = Mathf.Min(curYMin + this.inRect.yMin, this.inRect.yMax);
            }
            else
            {
                inRect.yMin = curYMin;
            }
            isOverflowing = false;
            GUIUtility.RestoreGUIState();
        }

        protected virtual RectSlice Slice(float height, bool includeMargins = true)
        {
            Rect outside = new Rect(inXMin, curYMin, inXMax - inXMin, includeMargins ? height + margins.y : height);
            Rect inside = new Rect(outside);
            if (includeMargins)
            {
                inside.xMin += margins.x * 2;
                inside.xMax -= margins.x;
                inside.yMin += margins.y / 2f;
                inside.yMax -= margins.y / 2f;
            }
            this.curYMin += includeMargins ? height + margins.y : height;
            if (drawBackground)
                Widgets.DrawBoxSolid(outside, CollapsibleBGColor);
            return new RectSlice(inside, outside);
        }
    }
}
