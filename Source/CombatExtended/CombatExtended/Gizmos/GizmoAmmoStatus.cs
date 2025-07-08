using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended;
[StaticConstructorOnStartup]
public class GizmoAmmoStatus : Command
{

    public CompAmmoUser compAmmo;
    public string prefix = "";
    private static readonly new Texture2D BGTex = ContentFinder<Texture2D>.Get("UI/Widgets/DesButBG", true);

    public override float GetWidth(float maxWidth)
    {
        return 120;
    }

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        Rect backgroundRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), Height);

        Rect inRect = backgroundRect.ContractedBy(6);
        GUI.DrawTexture(backgroundRect, BGTex);

        Text.Font = GameFont.Tiny;
        Rect textRect = inRect.TopHalf();
        Widgets.Label(textRect, prefix + (compAmmo.CurrentAmmo == null ? compAmmo.parent.def.LabelCap : compAmmo.CurrentAmmo.ammoClass.LabelCap));

        if (compAmmo.HasMagazine)
        {
            Rect barRect = inRect.BottomHalf();
            Widgets.FillableBar(barRect, (float)compAmmo.CurMagCount / compAmmo.MagSize);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, compAmmo.CurMagCount + " / " + compAmmo.MagSize);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        return new GizmoResult(GizmoState.Clear);
    }
}
