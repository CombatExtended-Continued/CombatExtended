using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class JobDriver_EquipFromInventory : JobDriver
    {
        public ThingWithComps Weapon
        {
            get
            {
                return (ThingWithComps)job.targetA.Thing;
            }
        }

        public CompInventory CompInventory
        {
            get
            {
                return pawn.TryGetComp<CompInventory>();
            }
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            yield return Toils_General.Wait(5).FailOn((toil) =>
            {
                return !toil.actor.inventory?.Contains(Weapon) ?? true;
            });
            yield return Toils_General.Do(() =>
            {
                CompInventory.TrySwitchToWeapon(Weapon, stopJob: false);

                if (pawn.equipment.Contains(Weapon))
                    this.EndJobWith(JobCondition.Succeeded);
                else
                    this.EndJobWith(JobCondition.Incompletable);
            });
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
    }
}
