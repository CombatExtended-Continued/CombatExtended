using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace CombatExtended
{
    public class JobDriver_GiveAmmo : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return GetActor().CanReserveAndReach(TargetA, PathEndMode.ClosestTouch, Danger.Deadly);
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_General.Do(
                delegate
                {
                    var targetPawn = (Pawn)TargetA.Thing;

                    var newThing = ThingMaker.MakeThing(TargetB.Thing.def);

                    newThing.HitPoints = TargetB.Thing.HitPoints;

                    newThing.stackCount = TargetB.Thing.stackCount;

                    TargetB.Thing.Destroy();

                    targetPawn.inventory.TryAddItemNotForSale(newThing);
                });
        }
    }
}
