using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatExtended.RocketGUI
{
    public static partial class GUIUtility
    {
        private struct FontState
        {
            public GameFont font;
            public GUIStyle curStyle;

            public FontState(GameFont font)
            {
                var a = Text.Font;
                Text.Font = font;
                this.font = font;
                this.curStyle = new GUIStyle(Text.CurFontStyle);
                Text.Font = a;
            }

            public void Restore()
            {
                Text.Font = font;
                Text.CurFontStyle.fontSize = curStyle.fontSize;
                Text.CurFontStyle.fontStyle = curStyle.fontStyle;
                Text.CurFontStyle.alignment = curStyle.alignment;
            }
        }

        private struct GUIState
        {
            public GameFont gameFont;
            public FontState[] fonts;
            public Color color;
            public Color contentColor;
            public Color backgroundColor;
            public bool wordWrap;

            public static GUIState Copy()
            {
                return new GUIState()
                {
                    gameFont = Text.Font,
                    fonts = new FontState[3] {
                        new FontState(GameFont.Tiny),
                        new FontState(GameFont.Small),
                        new FontState(GameFont.Medium),
                    },
                    color = GUI.color,
                    contentColor = GUI.contentColor,
                    backgroundColor = GUI.backgroundColor,
                    wordWrap = Text.WordWrap,
                };
            }

            public void Restore()
            {
                for (int i = 0; i < 3; i++)
                {
                    fonts[i].Restore();
                }
                Text.Font = gameFont;
                GUI.color = color;
                GUI.contentColor = contentColor;
                GUI.backgroundColor = backgroundColor;
                Text.WordWrap = wordWrap;
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private static int depth = 0;
        private static GUIState initialState;

        public static void StashGUIState()
        {
            if (depth == 0)
            {
                initialState = GUIState.Copy();
            }
            depth++;
        }

        public static void RestoreGUIState()
        {
            initialState.Restore();
            depth--;
        }

        public static void ClearGUIState()
        {
            depth = 0;
            initialState.Restore();
        }
    }
}
