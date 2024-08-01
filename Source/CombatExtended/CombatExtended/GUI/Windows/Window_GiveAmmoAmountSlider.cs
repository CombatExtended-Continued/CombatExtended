using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;

namespace CombatExtended
{
    public class Window_GiveAmmoAmountSlider : Window
    {
        public float ammoToGiveAmount = 1;

        public CompAmmoGiver sourceComp;

        public Thing sourceAmmo;

        public Pawn selPawn;

        public Pawn dad;

        public bool finalized = false;

        public int maxAmmoCount;

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(350f, 125f);
            }
        }
        public override void DoWindowContents(Rect inRect)
        {
            Widgets.HorizontalSlider(inRect.TopHalf().BottomHalf(), ref ammoToGiveAmount, new FloatRange(0, maxAmmoCount), "CE_AmmoAmount".Translate() + " " + ammoToGiveAmount.ToString(), 1);

            if (Widgets.ButtonText(inRect.BottomHalf().LeftHalf(), "Cancel".Translate()))
            {
                this.Close();
            }

            if (Widgets.ButtonText(inRect.BottomHalf().RightHalf(), "OK".Translate()))
            {
                finalized = true;
                this.Close();
            }
        }

        public override void Close(bool doCloseSound = true)
        {
            if (finalized)
            {
                sourceComp.GiveAmmo(selPawn, sourceAmmo, (int)this.ammoToGiveAmount);
            }

            base.Close(doCloseSound);
        }
    }
}
