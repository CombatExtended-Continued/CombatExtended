using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Widgets),nameof(Widgets.DefIcon))]
    public static class Harmony_Widgets
    {
        private static readonly List<AttachmentLink> empty = new List<AttachmentLink>();

        public static bool Prefix(Rect rect, Def def, float scale, Color? color)
        {
            if(def is WeaponPlatformDef platform)
            {
                float dx = rect.width * (1 - scale) / 2f;
                rect.xMin += dx;
                rect.xMax -= dx;
                float dy = rect.width * (1 - scale) / 2f;
                rect.yMin += dy;
                rect.yMax -= dy;
                RocketGUI.GUIUtility.DrawWeaponWithAttachments(rect, platform, empty, parts: platform.defaultGraphicParts, null, color);
                return false;
            }
            return true;
        }        
    }
}
