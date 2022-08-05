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
        public int ammoToGiveAmount = 1;

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
            Widgets.Label(inRect.TopHalf().TopHalf(), "CE_AmmoAmount".Translate() + " " + ammoToGiveAmount.ToString());
            ammoToGiveAmount = (int)Widgets.HorizontalSlider(inRect.TopHalf().BottomHalf(), ammoToGiveAmount, 0, maxAmmoCount);

            if(Widgets.ButtonText(inRect.BottomHalf().LeftHalf(), "Cancel".Translate()))
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
                sourceComp.ammoAmountToGive = this.ammoToGiveAmount;

                var jobdef = CE_JobDefOf.GiveAmmo;

                var job = new Job { def = jobdef, targetA = dad, targetB = sourceAmmo };

                selPawn.jobs.StartJob(job, JobCondition.InterruptForced);
            }

            base.Close(doCloseSound);
        }
    }
}
