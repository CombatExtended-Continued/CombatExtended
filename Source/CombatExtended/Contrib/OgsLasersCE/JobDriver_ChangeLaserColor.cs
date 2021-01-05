using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace OgsLasers
{
    public class JobDriver_ChangeLaserColor : JobDriver
    {
        private Thing Gun => job.GetTarget(TargetIndex.A).Thing;

        private IBeamColorThing BeamColorThing => job.GetTarget(TargetIndex.A).Thing as IBeamColorThing;

        private Thing Prism => job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (BeamColorThing == null) return false;

            return pawn.Reserve(Gun, job, 1, -1, null, errorOnFailed) && pawn.Reserve(Prism, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
            Toil repair = Toils_General.Wait(75, TargetIndex.None);
            repair.FailOnDespawnedOrNull(TargetIndex.A);
            repair.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            repair.WithEffect(Gun.def.repairEffect, TargetIndex.A);
            repair.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            yield return repair;
            yield return new Toil
            {
                initAction = delegate ()
                {

                    BeamColorThing.BeamColor = pawn.CurJob.maxNumMeleeAttacks;

                    var targetInfo = new TargetInfo(Gun.Position, Map, false);
                    Effecter effecter = EffecterDefOf.Deflect_Metal.Spawn();
                    effecter.Trigger(targetInfo, targetInfo);
                    effecter.Cleanup();
                    this.Prism.Destroy(DestroyMode.Vanish);
                }
            };
            yield break;
        }
    }
}
