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
    public class Dialog_SetValue : Window
    {
        private readonly Func<int, string> textGetter;
        private readonly Action<int> confirmAction;

        private int curValue;

        private const float BotAreaWidth = 60f;
        private const float BotAreaHeight = 30f;
        private new const float Margin = 10f;



        public Dialog_SetValue(Func<int, string> textGetter, Action<int> confirmAction, int value)
        {
            this.textGetter = textGetter;
            this.confirmAction = confirmAction;

            curValue = value;
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
            Text.Font = GameFont.Small;
            string text = textGetter(this.curValue);
            float height = Text.CalcHeight(text, inRect.width);

            Rect rect = new Rect(inRect.x, inRect.y, inRect.width, height);
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(rect, text);
            Text.Anchor = TextAnchor.UpperLeft;

            float btnSetterY = inRect.y + rect.height + Margin;


            Text.Anchor = TextAnchor.UpperCenter;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x + BotAreaWidth, btnSetterY, inRect.width - (BotAreaWidth) * 2, BotAreaHeight), curValue.ToString());
            Text.Font = GameFont.Small;

            if (Widgets.ButtonText(new Rect(inRect.x, btnSetterY, BotAreaWidth, BotAreaHeight), "-", true, true, true, null))
            {
                curValue--;
            }

            if (Widgets.ButtonText(new Rect(inRect.x + inRect.width - BotAreaWidth, btnSetterY, BotAreaWidth, BotAreaHeight), "+", true, true, true, null))
            {
                curValue++;
            }
            curValue = curValue >= 0 ? curValue : 0;

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;


            if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - BotAreaHeight, inRect.width, BotAreaHeight), "OK".Translate(), true, true, true, null))
            {
                Close(true);
                confirmAction(curValue);
            }
        }
    }
}
