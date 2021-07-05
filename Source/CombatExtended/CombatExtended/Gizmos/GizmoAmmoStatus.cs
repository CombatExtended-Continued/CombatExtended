using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class GizmoAmmoStatus : Command
    {
        private static bool initialized;
        //Link
        public CompAmmoUser compAmmo;
        public string prefix = "";

        private static Texture2D FullTex;
        private static Texture2D EmptyTex;
        private static new Texture2D BGTex;

        public override float GetWidth(float maxWidth)
        {
            return 120;
        }


        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            if (!initialized)
                InitializeTextures();

            Rect overRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), Height);
            Widgets.DrawBox(overRect);
            GUI.DrawTexture(overRect, BGTex);

            Rect inRect = overRect.ContractedBy(6);

            // Ammo type
            Rect textRect = inRect;
            textRect.height = overRect.height / 2;
            Text.Font = GameFont.Tiny;
            Widgets.Label(textRect, prefix + (compAmmo.CurrentAmmo == null ? compAmmo.parent.def.LabelCap : compAmmo.CurrentAmmo.ammoClass.LabelCap));

            // Bar
            if (compAmmo.HasMagazine)
            {
                Rect barRect = inRect;
                barRect.yMin = overRect.y + overRect.height / 2f;
                float ePct = (float)compAmmo.CurMagCount / compAmmo.Props.magazineSize;
                Widgets.FillableBar(barRect, ePct);
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
            initialized = true;
        }
    }
}
