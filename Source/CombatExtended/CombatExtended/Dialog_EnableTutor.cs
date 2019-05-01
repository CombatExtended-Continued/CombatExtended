using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class Dialog_EnableTutor : Window
    {
        public override Vector2 InitialSize => new Vector2(500f, 300f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            string label = "CE_EnableTutorText".Translate();
            Widgets.Label(new Rect(0f, 0f, inRect.width, inRect.height), label);
            if (Widgets.ButtonText(new Rect(0f, inRect.height - 35f, inRect.width / 2f - 20f, 35f), "CE_EnableTutorEnable".Translate()))
            {
                Prefs.AdaptiveTrainingEnabled = true;
                Close();
            }
            if (Widgets.ButtonText(new Rect(inRect.width / 2f + 20f, inRect.height - 35f, inRect.width / 2f - 20f, 35f), "CE_EnableTutorDisable".Translate()))
            {
                Close();
            }
        }

        public override void Close(bool doCloseSound = true)
        {
            Controller.settings.ShowTutorialPopup = false;
            Controller.settings.Write();
            base.Close(doCloseSound);
        }
    }
}