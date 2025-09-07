using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    class JobDriver_HunkerDown : JobDriver
    {
        private const int GetUpCheckInterval = 60;

        public override void SetInitialPosture()
        {
            pawn.jobs.posture = PawnPosture.LayingOnGroundNormal;
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);

            //Define Toil
            Toil toilWait = new Toil();
            toilWait.initAction = () =>
            {
                toilWait.actor.pather.StopDead();
            };

            Toil toilNothing = new Toil();
            //toilNothing.initAction = () => {};
            toilNothing.defaultCompleteMode = ToilCompleteMode.Delay;
            toilNothing.defaultDuration = GetUpCheckInterval;

            // Start Toil
            yield return toilWait;
            yield return toilNothing;
            yield return Toils_Jump.JumpIf(toilNothing, () =>
            {
                CompSuppressable comp = pawn.TryGetComp<CompSuppressable>();
                if (comp == null)
                {
                    return false;
                }
                if (!comp.CanReactToSuppression)
                {
                    return false;
                }
                return comp.IsHunkering;
            });
        }
    }
}
