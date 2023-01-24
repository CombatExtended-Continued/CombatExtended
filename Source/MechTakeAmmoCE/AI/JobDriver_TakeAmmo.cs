using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

using CombatExtended;

namespace MTA
{
    public class JobDriver_TakeAmmo : JobDriver
    {
        private Thing Ammo
        {
            get
            {
                return this.job.GetTarget(TargetIndex.A).Thing;
            }
        }

        public int amount;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Ammo, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);
            CompAmmoUser ammoUser = pawn.equipment.Primary.GetComp<CompAmmoUser>();

            foreach (Thing thing in pawn.inventory.innerContainer)
            {
                if (!(thing.def is AmmoDef ammoDef))
                {
                    continue;
                }

                if (ammoDef == Ammo.def)
                {
                    continue;
                }

                yield return Toils_Ammo.Drop(thing.def, thing.stackCount);
            }
            if (job.count > 0)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
                yield return Toils_Haul.TakeToInventory(TargetIndex.A, job.count);
            }
            else if (job.count < 0)
            {
                yield return Toils_Ammo.Drop(ammoUser.SelectedAmmo, -job.count);
            }
            else
            {
                Log.Error("[MechTakeAmmoCE] Trying to start job that no need ammo");
            }

        }
    }
}
