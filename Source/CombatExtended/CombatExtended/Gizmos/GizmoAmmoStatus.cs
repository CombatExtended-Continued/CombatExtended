using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended;
[StaticConstructorOnStartup]
public class GizmoAmmoStatus : Gizmo_Slider
{

    public CompAmmoUser compAmmo;
    public string prefix = "";

    public override float Width => 120;

    public override float Target
    {
        get => (float)compAmmo.TryReloadOn / compAmmo.MagSize;
        set => compAmmo.TryReloadOn = Mathf.FloorToInt(value * compAmmo.MagSize);
    }

    public override bool IsDraggable => compAmmo.MagSize > 1;

    public override float ValuePercent => (float)compAmmo.CurMagCount / compAmmo.MagSize;

    public override string Title => prefix + (compAmmo.CurrentAmmo == null ? compAmmo.parent.def.LabelCap : compAmmo.CurrentAmmo.ammoClass.LabelCap);

    private static bool draggingBar = false;
    public override bool DraggingBar
    {
        get => draggingBar;
        set => draggingBar = value;
    }

    public override string GetTooltip()
    {
        return "CE_ReloadAmmoTooltip".Translate();
    }

    public override string BarLabel => compAmmo.CurMagCount + " / " + compAmmo.MagSize;
    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        if (IsDraggable)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), 75f);
            if (Mouse.IsOver(rect))
            {
                TooltipHandler.TipRegion(rect, new Func<string>(this.AutoReloadTip), 42827123);
            }
        }
        return base.GizmoOnGUI(topLeft, maxWidth, parms);
    }
    public override void DrawHeader(Rect rect, ref bool mouseOverElement)
    {
        
        Text.Font = GameFont.Tiny;
        base.DrawHeader(rect, ref mouseOverElement);
        Text.Font = GameFont.Small;
    }

    private string AutoReloadTip()
    {
        return "CE_TryReloadAmmoTooltip".Translate(compAmmo.TryReloadOn);
    }
}
