﻿using System;
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
        public static Exception ExecuteSafeGUIAction(Action function, Action fallbackAction = null, bool catchExceptions = false)
        {
            StashGUIState();
            Exception exception = null;
            try
            {
                function.Invoke();
            }
            catch (Exception er)
            {
                Log.Error($"ROCKETMAN:UI error in ExecuteSafeGUIAction {er}");
                exception = er;
            }
            finally
            {
                RestoreGUIState();
            }
            if (exception != null && !catchExceptions)
            {
                if (fallbackAction != null)
                    exception = ExecuteSafeGUIAction(
                        fallbackAction,
                        catchExceptions: false);
                if (exception != null)
                    throw exception;
            }
            return exception;
        }

        private static readonly Color _altGray = new Color(0.2f, 0.2f, 0.2f);
        private static float[] _heights = new float[5000];

        public static void ScrollView<T>(Rect rect, ref Vector2 scrollPosition, IEnumerable<T> elements, Func<T, float> heightLambda, Action<Rect, T> elementLambda, Func<T, IComparable> orderByLambda = null, bool drawBackground = true, bool showScrollbars = true, bool catchExceptions = false, bool drawMouseOverHighlights = true)
        {
            StashGUIState();
            Exception exception = null;
            try
            {
                if (drawBackground)
                {
                    Widgets.DrawMenuSection(rect);
                    rect = rect.ContractedBy(2);
                }
                Rect contentRect = new Rect(0, 0, showScrollbars ? rect.width - 23 : rect.width, 0);
                IEnumerable<T> elementsInt = orderByLambda == null ? elements : elements.OrderBy(orderByLambda);
                if (_heights.Length < elementsInt.Count())
                    _heights = new float[elementsInt.Count() * 2];
                float h;
                float w = showScrollbars ? rect.width - 16 : rect.width;
                int j = 0;
                int k = 0;
                bool inView = true;
                foreach (T element in elementsInt)
                {
                    h = heightLambda.Invoke(element);
                    _heights[j++] = h;
                    contentRect.height += Math.Max(h, 0f);
                }
                j = 0;
                Widgets.BeginScrollView(rect, ref scrollPosition, contentRect, showScrollbars: showScrollbars);
                Rect currentRect = new Rect(1, 0, w, 0);
                foreach (T element in elementsInt)
                {
                    if (_heights[j] <= 0.00f)
                    {
                        j++;
                        continue;
                    }
                    currentRect.height = _heights[j];
                    if (false
                        || scrollPosition.y - 50 > currentRect.yMax
                        || scrollPosition.y + 50 + rect.height < currentRect.yMin)
                        inView = false;
                    if (inView)
                    {
                        if (drawBackground && k % 2 == 0)
                            Widgets.DrawBoxSolid(currentRect, _altGray);
                        if (drawMouseOverHighlights)
                            Widgets.DrawHighlightIfMouseover(currentRect);
                        elementLambda.Invoke(currentRect, element);
                    }
                    currentRect.y += _heights[j];
                    k++;
                    j++;
                    inView = true;
                }
            }
            catch (Exception er)
            {
                Log.Error($"ROCKETMAN:UI error in ScrollView {er}");
                exception = er;
            }
            finally
            {
                RestoreGUIState();
                Widgets.EndScrollView();
            }
            if (exception != null && !catchExceptions)
                throw exception;
        }

        public static void GridView<T>(Rect rect, int columns, List<T> elements, Action<Rect, T> cellLambda, bool drawBackground = true, bool drawVerticalDivider = false)
        {
            ExecuteSafeGUIAction(() =>
            {
                if (drawBackground)
                {
                    Widgets.DrawMenuSection(rect);
                }
                rect = rect.ContractedBy(1);
                int rows = (int)Math.Ceiling((decimal)elements.Count / columns);
                float columnStep = rect.width / columns;
                float rowStep = rect.height / rows;
                Rect curRect = new Rect(0, 0, columnStep, rowStep);
                int k = 0;
                for (int i = 0; i < columns && k < elements.Count; i++)
                {
                    curRect.x = i * columnStep + rect.x;
                    for (int j = 0; j < rows && k < elements.Count; j++)
                    {
                        curRect.y = j * rowStep + rect.y;
                        cellLambda(curRect, elements[k++]);
                    }
                }
            });
        }

        private static Rect _r1 = new Rect(0f, 0f, 1f, 1f);
        private static readonly Color _hColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);

        public static void DrawTexture(Rect rect, Texture2D texture, Material material = null)
        {            
            if (material == null)
                GUI.DrawTexture(rect, texture);
            else if (Event.current.type == EventType.Repaint)
                Graphics.DrawTexture(rect, texture, _r1, 0, 0, 0, 0, new Color(GUI.color.r * 0.5f, GUI.color.g * 0.5f, GUI.color.b * 0.5f, GUI.color.a * 0.5f), material);
        }

        public static void DrawWeaponWithAttachments(Rect inRect, WeaponPlatformDef platform, IEnumerable<AttachmentLink> attachments, IEnumerable<WeaponPlatformDef.WeaponGraphicPart> parts = null, AttachmentLink highlight = null, Color? color = null, Material colorMat = null)
        {
            ExecuteSafeGUIAction(() =>
            {
                Color originalColor = GUI.color;
                Texture2D weaponTex = platform.UIWeaponTex;
                foreach (AttachmentLink link in attachments)
                {
                    if (link.HasOutline && (highlight == null || link.CompatibleWith(highlight)))
                        DrawTex(inRect, link, link.UIOutlineTex, colorMat);
                }
                if (highlight?.HasOutline ?? false)
                    DrawTex(inRect, highlight, highlight.UIOutlineTex, colorMat);
                if (parts != null)
                {
                    foreach (WeaponPlatformDef.WeaponGraphicPart part in parts)
                    {
                        if (part.HasOutline && (highlight == null || part.slotTags.All(s => !highlight.attachment.slotTags.Contains(s))))
                            GUI.DrawTexture(inRect, part.UIOutlineTex, ScaleMode.StretchToFill);
                    }
                }
                if (highlight != null)
                    GUI.color = _hColor;
                if (color != null && color.HasValue)
                    GUI.color = color.Value;
                DrawTexture(inRect, weaponTex, colorMat);
                if (parts != null)
                {
                    foreach (WeaponPlatformDef.WeaponGraphicPart part in parts)
                    {
                        if (part.HasPartMat && (highlight == null || part.slotTags.All(s => !highlight.attachment.slotTags.Contains(s))))
                            GUI.DrawTexture(inRect, part.UIPartTex, ScaleMode.StretchToFill);
                    }
                }
                foreach (AttachmentLink link in attachments)
                {
                    if (link.HasAttachmentMat && (highlight == null || link.CompatibleWith(highlight)))
                        DrawTex(inRect, link, link.UIAttachmentTex, colorMat);
                }
                GUI.color = originalColor;
                if (highlight?.HasAttachmentMat ?? false)
                    DrawTex(inRect, highlight, highlight.UIAttachmentTex, colorMat);
            });
            void DrawTex(Rect rect, AttachmentLink link, Texture2D texture, Material mat)
            {                
                if (link.HasDrawOffset)
                {
                    rect.x -= rect.width * link.drawOffset.x;
                    rect.y -= rect.height * link.drawOffset.y;
                }
                rect.xMin = rect.xMax - rect.width * link.drawScale.x;
                rect.yMin = rect.yMax - rect.height * link.drawScale.y;
                DrawTexture(rect, texture, mat);
            }
        }     

        public static void DrawWeaponWithAttachments(Rect inRect, WeaponPlatform weapon, AttachmentLink highlight = null, Color? color = null, Material colorMat = null)
        {
            DrawWeaponWithAttachments(inRect, weapon.Platform, weapon.CurLinks, weapon.VisibleDefaultParts, highlight, color, colorMat);
        }

        public static void DropDownMenu<T>(Func<T, string> labelLambda, Action<T> selectedLambda, T[] options)
        {
            DropDownMenu(labelLambda, selectedLambda, options.AsEnumerable());
        }

        public static void DropDownMenu<T>(Func<T, string> labelLambda, Action<T> selectedLambda, IEnumerable<T> options)
        {

            ExecuteSafeGUIAction(() =>
            {
                Text.Font = GameFont.Small;
                FloatMenuUtility.MakeMenu(options,
                    (option) =>
                    {
                        return labelLambda(option);
                    },
                    (option) =>
                    {
                        return () => selectedLambda(option);
                    }
                );
            });
        }

        public static void Row(Rect rect, List<Action<Rect>> contentLambdas, bool drawDivider = true, bool drawBackground = false)
        {
            ExecuteSafeGUIAction(() =>
            {
                if (drawBackground)
                {
                    Widgets.DrawMenuSection(rect);
                }
                float step = rect.width / contentLambdas.Count;
                Rect curRect = new Rect(rect.x - 5, rect.y, step - 10, rect.height);
                for (int i = 0; i < contentLambdas.Count; i++)
                {
                    Action<Rect> lambda = contentLambdas[i];
                    if (drawDivider && i + 1 < contentLambdas.Count)
                    {
                        Vector2 start = new Vector2(curRect.xMax + 5, curRect.yMin + 1);
                        Vector2 end = new Vector2(curRect.xMax + 5, curRect.yMax - 1);
                        Widgets.DrawLine(start, end, Color.white, 1);
                    }
                    ExecuteSafeGUIAction(() =>
                    {
                        lambda.Invoke(curRect);
                        curRect.x += step;
                    });
                }
            });
        }

        public static void CheckBoxLabeled(Rect rect, string label, ref bool checkOn, bool disabled = false, bool monotone = false, float iconWidth = 20, GameFont font = GameFont.Tiny, FontStyle fontStyle = FontStyle.Normal, bool placeCheckboxNearText = false, bool drawHighlightIfMouseover = true, Texture2D texChecked = null, Texture2D texUnchecked = null)
        {
            CheckBoxLabeled(rect, label, Color.white, ref checkOn, disabled, monotone, iconWidth, font, fontStyle, placeCheckboxNearText, drawHighlightIfMouseover, texChecked, texUnchecked);
        }

        public static void CheckBoxLabeled(Rect rect, string label, Color radioColor, ref bool checkOn, bool disabled = false, bool monotone = false, float iconWidth = 20, GameFont font = GameFont.Tiny, FontStyle fontStyle = FontStyle.Normal, bool placeCheckboxNearText = false, bool drawHighlightIfMouseover = true, Texture2D texChecked = null, Texture2D texUnchecked = null)
        {
            bool checkOnInt = checkOn;
            ExecuteSafeGUIAction(() =>
            {
                Text.Font = font;
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.CurFontStyle.fontStyle = fontStyle;
                if (placeCheckboxNearText)
                {
                    rect.width = Mathf.Min(rect.width, Text.CalcSize(label).x + 24f + 10f);
                }
                Widgets.Label(rect, label);
                if (!disabled && Widgets.ButtonInvisible(rect))
                {
                    checkOnInt = !checkOnInt;
                    if (checkOnInt)
                    {
                        SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
                    }
                    else
                    {
                        SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
                    }
                }
                Rect iconRect = new Rect(0f, 0f, iconWidth, iconWidth);
                iconRect.center = rect.RightPartPixels(iconWidth).center;
                Color color = GUI.color;
                if(radioColor != Color.white)
                {
                    GUI.color = radioColor;
                }
                else if (disabled || monotone)
                {
                    GUI.color = Widgets.InactiveColor;
                }               
                GUI.DrawTexture(image: (checkOnInt) ? ((texChecked != null) ? texChecked : Widgets.CheckboxOnTex) : ((texUnchecked != null) ? texUnchecked : Widgets.CheckboxOffTex), position: iconRect);                
                if (disabled || monotone)
                {
                    GUI.color = color;
                }
                if (drawHighlightIfMouseover)
                {
                    Widgets.DrawHighlightIfMouseover(rect);
                }
            });
            checkOn = checkOnInt;
        }

        public static void ColorBoxDescription(Rect rect, Color color, string description)
        {
            Rect textRect = new Rect(rect.x + 30, rect.y, rect.width - 30, rect.height);
            Rect boxRect = new Rect(0, 0, 10, 10);
            boxRect.center = new Vector2(rect.xMin + 15, rect.yMin + rect.height / 2);
            ExecuteSafeGUIAction(() =>
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Tiny;
                Text.CurFontStyle.fontStyle = FontStyle.Normal;
                Widgets.DrawBoxSolid(boxRect, color);
                Widgets.Label(textRect, description);
            });
        }
    }
}
