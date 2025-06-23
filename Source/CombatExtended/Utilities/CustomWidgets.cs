using Verse;
using UnityEngine;
using System;

namespace CombatExtended
{
    public static class CustomWidgets
    {
        private const float ButtonWidth = 30f;
        private const float TextWidth = 50f;
        private const float Padding = 5f;

        public static void DrawIntOptionWithSpinners(Rect rect, string label, string tooltip, ref int value, float minValue, int maxValue, int stepperValue)
        {

            TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;

            Vector2 labelSize = Text.CalcSize(label);
            Rect labelRect = new Rect(rect.x, rect.y, labelSize.x, rect.height);
            Widgets.Label(labelRect, label);

            string buffer = value.ToString();
            Rect textField = new Rect(labelRect.xMax + Padding, rect.y, TextWidth, rect.height);
            Widgets.TextFieldNumeric(textField, ref value, ref buffer, minValue, maxValue);

            float halfHeight = rect.height / 2f;
            Rect spinnerRect = new Rect(textField.xMax + Padding, rect.y, ButtonWidth, halfHeight);
            float step = Event.current.control ? stepperValue * 10 : stepperValue;
            if (Widgets.ButtonText(spinnerRect, "▲"))
            {
                value = (int)Math.Min(value + step, maxValue);
            }
            spinnerRect.y += halfHeight;
            if (Widgets.ButtonText(spinnerRect, "▼"))
            {
                value = (int)Math.Max(value - step, minValue);
            }
            if (Mouse.IsOver(rect))
            {
                if (Event.current.isScrollWheel)
                {
                    if (Event.current.delta.y < 0f) // Scroll Up
                    {
                        value = (int)Math.Min(value + step, maxValue);
                    }
                    else if (Event.current.delta.y > 0f) // Scroll Down
                    {
                        value = (int)Math.Max(value - step, minValue);
                    }
                }
                if (!tooltip.NullOrEmpty())
                {
                    TooltipHandler.TipRegion(rect, tooltip);
                }
                Widgets.DrawHighlight(rect);
            }

            Text.Anchor = anchor;
        }
    }
}
