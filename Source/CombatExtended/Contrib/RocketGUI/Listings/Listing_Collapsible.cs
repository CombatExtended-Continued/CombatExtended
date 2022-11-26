﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO.Ports;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using GUILambda = System.Action<UnityEngine.Rect>;

namespace CombatExtended.RocketGUI
{
    public class Listing_Collapsible : IListing_Custom
    {
        private Group_Collapsible group;

        private bool expanded = false;

        public class Group_Collapsible
        {
            private List<Listing_Collapsible> collapsibles;

            public List<Listing_Collapsible> AllCollapsibles
            {
                get => collapsibles != null ? collapsibles : collapsibles = new List<Listing_Collapsible>();
            }

            public void CollapseAll()
            {
                foreach (Listing_Collapsible collapsible in AllCollapsibles)
                {
                    collapsible.expanded = false;
                }
            }

            public void Register(Listing_Collapsible collapsible)
            {
                AllCollapsibles.Add(collapsible);

                collapsible.expanded = false;
            }
        }

        public Group_Collapsible Group
        {
            get => group;
            set
            {
                group.AllCollapsibles.RemoveAll(c => c == this);
                group = value;
                group.Register(this);
            }
        }

        public bool Expanded
        {
            get => this.expanded;
            set
            {
                this.group.CollapseAll();
                this.expanded = value;
            }
        }

        public Listing_Collapsible(bool expanded = false, bool scrollViewOnOverflow = true, bool drawBorder = false, bool drawBackground = false) : base(scrollViewOnOverflow, drawBorder, drawBackground)
        {
            this.expanded = expanded;
            this.group = new Group_Collapsible();
        }

        public Listing_Collapsible(Group_Collapsible group, bool expanded = false, bool scrollViewOnOverflow = true, bool drawBorder = false, bool drawBackground = false) : base(scrollViewOnOverflow, drawBorder, drawBackground)
        {
            this.expanded = expanded;
            this.group = group;
            this.group.Register(this);
        }

        public virtual void Begin(Rect inRect)
        {
            base.Begin(inRect);
            this.Gap(2);
        }

        public virtual void Begin(Rect inRect, TaggedString title, bool drawInfo = true, bool drawIcon = true, bool hightlightIfMouseOver = true, GameFont fontSize = GameFont.Small, FontStyle fontStyle = FontStyle.Normal)
        {
            base.Begin(inRect);
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                Text.Font = fontSize;
                Text.CurFontStyle.fontStyle = fontStyle;
                Text.Anchor = TextAnchor.MiddleLeft;
                RectSlice slice = Slice(title.GetTextHeight(this.insideWidth - 30f));
                if (hightlightIfMouseOver)
                {
                    Widgets.DrawHighlightIfMouseover(slice.outside);
                }
                Rect titleRect = slice.inside;
                GUIUtility.ExecuteSafeGUIAction(() =>
                {
                    GUI.color = this.CollapsibleBGBorderColor;
                    GUI.color = Color.gray;
                    if (drawInfo)
                    {
                        Text.Font = GameFont.Tiny;
                        Text.Anchor = TextAnchor.MiddleRight;
                        Widgets.Label(titleRect, !expanded ? "Collapsed" : "Expanded");
                    }
                });
                GUIUtility.ExecuteSafeGUIAction(() =>
                {
                    Text.Font = fontSize;
                    Text.CurFontStyle.fontStyle = fontStyle;
                    if (this.drawBorder && this.drawBackground)
                    {
                        Text.CurFontStyle.fontSize = 12;
                    }
                    Text.Anchor = TextAnchor.MiddleLeft;
                    GUI.color = this.CollapsibleBGBorderColor;
                    GUI.color = Color.gray;
                    if (drawIcon)
                    {
                        Widgets.DrawTextureFitted(titleRect.LeftPartPixels(25), expanded ? TexButton.Collapse : TexButton.Reveal, 0.65f);
                        titleRect.xMin += 35;
                    }
                    GUI.color = Color.white;
                    Widgets.Label(titleRect, title);
                });
                if (Widgets.ButtonInvisible(slice.outside))
                {
                    Expanded = !Expanded;
                }
                GUI.color = this.CollapsibleBGBorderColor;
                if (this.drawBorder)
                {
                    Widgets.DrawBox(slice.outside, 1);
                }
            });
            if (Expanded && drawBorder)
            {
                this.Gap(2);
            }
            if (!drawBorder)
            {
                this.Line(1);
            }
            base.Start();
        }

        public void Label(TaggedString text, string tooltip = null, bool invert = false, bool hightlightIfMouseOver = true, GameFont fontSize = GameFont.Tiny, FontStyle fontStyle = FontStyle.Normal, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            if (invert == this.expanded)
            {
                return;
            }
            base.Label(text, GUI.color, tooltip, hightlightIfMouseOver, fontSize, fontStyle, anchor: anchor);
        }

        public void Label(TaggedString text, Color color, string tooltip = null, bool invert = false, bool hightlightIfMouseOver = true, GameFont fontSize = GameFont.Tiny, FontStyle fontStyle = FontStyle.Normal, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            if (invert == this.expanded)
            {
                return;
            }
            base.Label(text, color, tooltip, hightlightIfMouseOver, fontSize, fontStyle, anchor: anchor);
        }

        public bool CheckboxLabeled(TaggedString text, ref bool checkOn, string tooltip = null, bool invert = false, bool disabled = false, bool hightlightIfMouseOver = true, GameFont fontSize = GameFont.Tiny, FontStyle fontStyle = FontStyle.Normal)
        {
            if (invert == this.expanded)
            {
                return false;
            }
            return base.CheckboxLabeled(text, ref checkOn, tooltip, disabled, hightlightIfMouseOver, fontSize, fontStyle);
        }


        public void DropDownMenu<T>(string text, T selection, Func<T, string> labelLambda, Action<T> selectedLambda, IEnumerable<T> options, bool invert = false, bool disabled = false, GameFont fontSize = GameFont.Tiny, FontStyle fontStyle = FontStyle.Normal)
        {
            if (invert == this.expanded)
            {
                return;
            }
            base.DropDownMenu(text, selection, labelLambda, selectedLambda, options, disabled, fontSize: fontSize, fontStyle: fontStyle);
        }

        public void Columns(float height, IEnumerable<GUILambda> lambdas, float gap = 5, bool invert = false, bool useMargins = false, Action fallback = null)
        {
            if (invert == expanded)
            {
                return;
            }
            base.Columns(height, lambdas, gap, useMargins, fallback);
        }

        public void Lambda(float height, GUILambda contentLambda, bool invert = false, bool useMargins = false, bool hightlightIfMouseOver = false, Action fallback = null)
        {
            if (invert == expanded)
            {
                return;
            }
            base.Lambda(height, contentLambda, useMargins, hightlightIfMouseOver, fallback);
        }

        public bool ButtonText(TaggedString text, bool disabled = false, bool invert = false, bool drawBackground = true)
        {
            if (invert == expanded)
            {
                return false;
            }
            return base.ButtonText(text, disabled, drawBackground);
        }

        public void Gap(float height = 9f, bool invert = false)
        {
            if (expanded != invert)
            {
                base.Gap(height);
            }
        }

        public void Line(float thickness, bool invert = false)
        {
            if (expanded != invert)
            {
                base.Line(thickness);
            }
        }

        public override void End(ref Rect inRect)
        {
            base.End(ref inRect);
        }

        protected override RectSlice Slice(float height, bool includeMargins = true)
        {
            return base.Slice(height, includeMargins);
        }
    }
}
