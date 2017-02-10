using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    class JobDriver_HunkerDown : JobDriver
    {
        private Toil toilHunkerDown;
        private bool startedIncapacitated;
        private int ticksLeft;
        private int maxTicks;
        private bool willPee;
        IntVec3 coverPosition;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue(ref startedIncapacitated, "startedIncapacitated", false, false);
            Scribe_Values.LookValue(ref ticksLeft, "ticksLeft", 0, false);
            Scribe_Values.LookValue(ref maxTicks, "maxTicks", 0, false);
            Scribe_Values.LookValue(ref willPee, "willPee", false, false);
        }

        public override PawnPosture Posture
        {
            get
            {
                return CurToil != toilHunkerDown ? PawnPosture.Standing : PawnPosture.LayingAny;
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);

            //Define Toil

            maxTicks = Rand.Range(60, 600);
            ticksLeft = maxTicks;
            willPee = maxTicks > 500;

            toilHunkerDown = new Toil
            {
                initAction = delegate
                {
                    //int num = 0;
                    //IntVec3 intVec;
                    //while (true)
                    //{
                    //    intVec = pawn.Position + GenAdj.AdjacentCellsAndInside[Rand.Range(0, 9)];
                    //    num++;
                    //    if (num > 12)
                    //    {
                    //        intVec = pawn.Position;
                    //        break;
                    //    }
                    //    if (intVec.InBounds() && intVec.Standable())
                    //    {
                    //        break;
                    //    }
                    //}
                    //pawn.CurJob.targetB = intVec;
                    //pawn.Drawer.rotator.FaceCell(intVec);

                    toilHunkerDown.actor.pather.StopDead();
                },
                tickAction = delegate
                {
                    ticksLeft--;
                    if (willPee)
                    {
                        if (ticksLeft % maxTicks == maxTicks - 1)
                        {
                            //      MoteMaker.ThrowMetaIcon(pawn.Position, ThingDefOf.Mote_Heart);
                            //     FilthMaker.MakeFilth(pawn.CurJob.targetB.Cell, CE_ThingDefOf.FilthPee, pawn.LabelIndefinite(), 1);
                            FilthMaker.MakeFilth(pawn.Position, pawn.Map, CE_ThingDefOf.FilthPee, pawn.LabelIndefinite(), 1);
                        }
                    }
                    if (ticksLeft <= 0)
                    {
                        ReadyForNextToil();
                        if (willPee)
                            TaleRecorder.RecordTale(CE_TaleDefOf.WetHimself, pawn);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Never,
            };


            CompSuppressable comp = pawn.TryGetComp<CompSuppressable>();
            toilHunkerDown.FailOn(() => comp == null);
            if (comp != null)
            {
                float distToSuppressor = (pawn.Position - comp.suppressorLoc).LengthHorizontal;
                toilHunkerDown.FailOn(() => distToSuppressor < CompSuppressable.minSuppressionDist);
                toilHunkerDown.FailOn(() => !comp.isHunkering);
            }

            // bug can get the willPee if it's initialized, define the bool more accessable
            if (willPee)
                toilHunkerDown.WithEffect(EffecterDef.Named("Pee"), TargetIndex.A);

            // Start Toil

            yield return toilHunkerDown;
            if (JobGiver_RunForCover.GetCoverPositionFrom(pawn, comp.suppressorLoc, JobGiver_RunForCover.maxCoverDist * 1.5f, out coverPosition) && Rand.Value>0.5)
            {
                if (coverPosition != pawn.Position)
                {
                    Toil toil = new Toil();
                    toil.initAction = delegate
                    {
                        Pawn actor = toil.actor;
                        actor.Map.pawnDestinationManager.ReserveDestinationFor(pawn, coverPosition);
                        actor.pather.StartPath(coverPosition, PathEndMode.OnCell);
                    };
                    toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;

                    // Shame, shame, shame, shame!
                    if (willPee)
                        toil.WithEffect(EffecterDef.Named("Pee"), TargetIndex.A);

                    yield return toil;

                }
            }
            yield break;
        }


    }
}
