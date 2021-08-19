using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using GUITextState = System.Tuple<string, Verse.GameFont, float, float, float>;

namespace CombatExtended.RocketGUI
{
    public static partial class GUIUtility
    {
        private readonly static Dictionary<GUITextState, float> textHeightCache = new Dictionary<GUITextState, float>(512);

        public static string Fit(this string text, Rect rect)
        {
            float height = GetTextHeight(text, rect.width);
            if (height <= rect.height)
            {
                return text;
            }
            return text.Substring(0, (int)((float)text.Length * height / rect.height)) + "...";
        }

        public static float GetTextHeight(this string text, Rect rect)
        {
            return text != null ? CalcTextHeight(text, rect.width) : 0;
        }

        public static float GetTextHeight(this string text, float width)
        {
            return text != null ? CalcTextHeight(text, width) : 0;
        }

        public static float GetTextHeight(this TaggedString text, float width)
        {
            return text != null ? CalcTextHeight(text, width) : 0;
        }

        public static float CalcTextHeight(string text, float width)
        {
            GUITextState key = GetGUIState(text, width);
            if (textHeightCache.TryGetValue(key, out float height))
            {
                return height;
            }
            return textHeightCache[key] = Text.CalcHeight(text, width);
        }

        private static GUITextState GetGUIState(string text, float width)
        {
            return new GUITextState(
                text,
                Text.Font,
                width,
                Prefs.UIScale,
                Text.CurFontStyle.fontSize);
        }
    }
}
