using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class GizmoAmmoStatus : Command
    {
        private static bool initialized;

        public CompAmmoUser compAmmo;
        public string prefix = "";

        private static Texture2D FullTex;
        private static Texture2D EmptyTex;
        private static new Texture2D BGTex;

        public override float GetWidth(float maxWidth)
        {
            return 120;
        }

        public GizmoAmmoStatus()
        {
            if (!initialized)
            {
                InitializeTextures();
                initialized = true;
            }
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect backgroundRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), Height);

            Rect inRect = backgroundRect.ContractedBy(6);
            GUI.DrawTexture(backgroundRect, BGTex);

            Text.Font = GameFont.Tiny;
            Rect textRect = inRect.BottomHalf();
            Widgets.Label(textRect, prefix + (compAmmo.CurrentAmmo == null ? compAmmo.parent.def.LabelCap : compAmmo.CurrentAmmo.ammoClass.LabelCap));

            if (compAmmo.HasMagazine)
            {
                Rect barRect = inRect.TopHalf();
                Widgets.FillableBar(barRect, compAmmo.CurMagCount / compAmmo.Props.magazineSize);

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(barRect, compAmmo.CurMagCount + " / " + compAmmo.Props.magazineSize);
                Text.Anchor = TextAnchor.UpperLeft;
            }

            return new GizmoResult(GizmoState.Clear);
        }

        private void InitializeTextures()
        {
            if (FullTex == null)
                FullTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));
            if (EmptyTex == null)
                EmptyTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);
            if (BGTex == null)
                BGTex = ContentFinder<Texture2D>.Get("UI/Widgets/DesButBG", true);
        }
    }
}
