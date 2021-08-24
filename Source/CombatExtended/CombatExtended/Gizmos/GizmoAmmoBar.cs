using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using CombatExtended.RocketGUI;
using GUIUtility = CombatExtended.RocketGUI.GUIUtility;
using Verse.Sound;

namespace CombatExtended
{
    public class GizmoAmmoBar : Gizmo
    {                
        private static readonly Color backgroundColor = new Color(0.08f, 0.09f, 0.11f);
        private static readonly Color elementBackgroundColor = new Color(0.19f, 0.22f, 0.25f, 1f);
        private static readonly Color fillColor = new Color(0.01f, 0.3f, 0.63f, 1f);

        private class AmmoBarElement
        {
            public Rect rect;
            public IReloadable reloadable;
            public int sig = -1;            
        }               

        public Thing weapon;
        public CompAmmoUser overrideUser = null;
        
        private int sigCounter = 0;
        private readonly List<AmmoBarElement> bars = new List<AmmoBarElement>();                                

        public override float GetWidth(float maxWidth)
        {
            return Mathf.Min(120, maxWidth);
        }

        private Event evt;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            evt = Event.current;
            Rect inRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), Height);            
            // main background            
            GUI.DrawTexture(inRect, CE_GizmoTex.BGTex);
            // update reloadables
            if (overrideUser == null)
            {
                sigCounter += 1;
                foreach (IReloadable reloadable in weapon.GetReloadables())
                {
                    AmmoBarElement bar = bars.FirstOrDefault(b => b.reloadable == reloadable);
                    // didn't found a matching ammo bar so we create one
                    if (bar == null) bars.Add(bar = new AmmoBarElement() { reloadable = reloadable, rect = inRect });
                    bar.sig = sigCounter;
                }
                bars.RemoveAll(b => b.sig != sigCounter);
            }
            // for turrets and other things that doesn't support multiple verbs
            else if(bars.Count == 0)
            {                
                // didn't found a matching ammo bar so we create one
                bars.Add(new AmmoBarElement() { reloadable = overrideUser, rect = inRect });                
            }
            // start drawing
            // draw background
            Widgets.DrawBoxSolid(inRect.ContractedBy(1), backgroundColor);
            // draw bars
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                Rect rect = inRect.ContractedBy(3, 2);                
                int count = Mathf.Max(bars.Count, 2);
                rect.height = rect.height / count;       
                bool drawText = bars.Count <= 2;
                for (int i = 0; i < bars.Count; i++)
                {                                                                    
                    DrawBar(rect.ContractedBy(0, 1), bars[i], drawText);                    
                    rect.y += rect.height;
                }
            });           
            return Mouse.IsOver(inRect) ? new GizmoResult(GizmoState.Mouseover) : new GizmoResult(GizmoState.Clear);
        }

        private void DrawBar(Rect inRect, AmmoBarElement element, bool drawText = true)
        {                                   
            AmmoDef ammoDef = element.reloadable.CurrentAmmo ?? element.reloadable.SelectedAmmo ?? element.reloadable.AmmoSet.ammoTypes[0].ammo;
            Widgets.DrawBoxSolid(inRect, elementBackgroundColor);
            Widgets.DrawHighlightIfMouseover(inRect);
            if (Mouse.IsOver(inRect))
            {                
                if(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                    GUI.DrawTexture(inRect, Widgets.ButtonBGAtlasClick);

                bool clicked = false;
                // process event
                if (Input.GetMouseButtonUp(0))
                {
                    clicked = true;
                    if (element.reloadable.HasAmmo && !element.reloadable.MagazineFull)
                    {
                        element.reloadable.TryStartReload();
                        clicked = false;
                    }
                }
                if (clicked || Input.GetKeyUp(KeyCode.Mouse1)) 
                    DoFloatMenuOption(element, ammoDef);

                // do dropdown menu                       
                if (!(Find.WindowStack.FloatMenu?.IsOpen ?? false))
                {
                    GUIUtility.ExecuteSafeGUIAction(() =>
                    {
                        Text.Font = GameFont.Small;
                        GUI.color = Color.white;
                        StringBuilder builder = new StringBuilder();
                        builder.Append($"{ammoDef.label.CapitalizeFirst()}\n<color=orange>{ammoDef.ammoClass.LabelCap}</color>");
                        builder.Append("\n\n<color=gray>");
                        builder.Append("CE_AmmoBar_Reload".Translate());
                        builder.Append("</color>");
                        builder.Append("\n<color=gray>");
                        builder.Append("CE_AmmoBar_Ammotype".Translate());
                        builder.Append("</color>");
                        TooltipHandler.TipRegion(inRect, builder.ToString());
                    });
                }
            }
            // prepare the bar
            Rect iconRect, barRect;            
            element.rect = inRect;                        
            GUI.color = Color.white;
            // draw the icon
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                iconRect = inRect;                
                iconRect.width = iconRect.height;
                iconRect = iconRect.CenteredOnXIn(inRect.LeftPartPixels(29));
                iconRect.x += 3;
                Widgets.DefIcon(iconRect, ammoDef, null, 0.9f);
            });
            // draw the ammo bar
            inRect.xMin += 29 + 4;
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                barRect = inRect;
                if (drawText) barRect = barRect.ContractedBy(2);
                GUI.color = fillColor;                
                Widgets.FillableBar(barRect.ContractedBy(1), (float)element.reloadable.CurMagCount / element.reloadable.MagSize);
                GUI.color = Color.white;
                if (drawText)
                {
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(barRect, $"{element.reloadable.CurMagCount}/{element.reloadable.MagSize}");
                }
            });
        }

        private static readonly List<int> _dumy = new List<int>() { 0};

        private void DoFloatMenuOption(AmmoBarElement bar, AmmoDef curretDef)
        {                        
            IEnumerable<AmmoDef> avialable = bar.reloadable.AvailableAmmoDefs?.Where(a => a != bar.reloadable.CurrentAmmo) ?? null;
            if (!avialable.EnumerableNullOrEmpty())
            {
                GUIUtility.DropDownMenu((ammo) =>
                {
                    return ammo.ammoClass.label;
                },
                (ammo) =>
                {
                    if (bar.reloadable.AvailableAmmoDefs.Contains(ammo))
                    {
                        bar.reloadable.SelectedAmmo = ammo;
                        if (Controller.settings.AutoReloadOnChangeAmmo)
                            bar.reloadable.TryStartReload();
                    }
                }, avialable);
            }
            else
            {
                GUIUtility.DropDownMenu((ammo) =>
                {
                    if(!bar.reloadable.HasAmmo)
                        return "CE_AmmoBar_Out".Translate($"{curretDef.label} - {curretDef.ammoClass.LabelCap}");
                    return "CE_AmmoBar_Selected".Translate(curretDef.ammoClass.LabelCap);
                },
                (_) =>
                {                   
                }, _dumy);
            }
        }
    }
}
