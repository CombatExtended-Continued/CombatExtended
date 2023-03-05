using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
    public class Dialog_SetMagCount : Window
    {
        private readonly CompMechAmmo mechAmmo;

        private const float BotAreaWidth = 30f;
        private const float BotAreaHeight = 30f;
        private new const float Margin = 3f;


        public Dialog_SetMagCount(CompMechAmmo mechAmmo)
        {
            if (mechAmmo == null)
            {
                Log.Error("null CompMechAmmo for Dialog_SetMagCount");
                return;
            }
            this.mechAmmo = mechAmmo;
        }

        public override void PreOpen()
        {
            Vector2 initialSize = this.InitialSize;
            initialSize.y = (mechAmmo.AmmoUser.Props.ammoSet.ammoTypes.Count + 4) * (BotAreaHeight + Margin);
            this.windowRect = new Rect(((float)UI.screenWidth - initialSize.x) / 2f, ((float)UI.screenHeight - initialSize.y) / 2f, initialSize.x, initialSize.y);
            this.windowRect = this.windowRect.Rounded();
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(300f, 130f);
            }
        }
        public override void DoWindowContents(Rect inRect)
        {
            float curY = 0;
            string Maglabel = "MTA_MagazinePrefix".Translate(mechAmmo.AmmoUser.Props.magazineSize);
            DrawLabel(inRect, ref curY, Maglabel);
            foreach (var ammoType in mechAmmo.AmmoUser.Props.ammoSet.ammoTypes)
            {
                int value = 0;
                mechAmmo.Loadouts.TryGetValue(ammoType.ammo, out value);
                string label = ammoType.ammo.ammoClass.labelShort != null ? ammoType.ammo.ammoClass.labelShort : ammoType.ammo.ammoClass.label;
                DrawThingRow(inRect, ref curY, ref value, ammoType.ammo, label);
                mechAmmo.Loadouts.SetOrAdd(ammoType.ammo, value);
            }
            curY += Margin;

            if (Widgets.ButtonText(new Rect(inRect.x, curY, inRect.width, BotAreaHeight), "OK".Translate(), true, true, true, null))
            {
                mechAmmo.TakeAmmoNow();
                Close(true);
            }
        }

        public void DrawThingRow(Rect rect, ref float curY, ref int count, Def defForIcon, string label)
        {
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.DefIcon(new Rect(rect.x, curY, BotAreaWidth, BotAreaHeight), defForIcon);
            Widgets.Label(new Rect(rect.x + BotAreaWidth + Margin, curY + BotAreaHeight / 4, rect.width - BotAreaWidth * 4, BotAreaHeight), label);
            if (Widgets.ButtonText(new Rect(rect.x + rect.width - BotAreaWidth * 4, curY, BotAreaWidth, BotAreaHeight), "-", true, true, true, null))
            {
                count--;
            }
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(rect.x + rect.width - BotAreaWidth * 3, curY + BotAreaHeight / 4, BotAreaWidth * 2, BotAreaHeight), count.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonText(new Rect(rect.x + rect.width - BotAreaWidth, curY, BotAreaWidth, BotAreaHeight), "+", true, true, true, null))
            {
                count++;
            }

            count = count < 0 ? 0 : count;

            curY += BotAreaHeight + Margin;
        }

        public void DrawLabel(Rect rect, ref float curY, string label)
        {
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(rect.x + BotAreaWidth, curY, rect.width - BotAreaWidth * 2, BotAreaHeight), label.ToString());
            curY += BotAreaHeight + Margin;
        }
    }
}
