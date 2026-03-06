using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended;
public class JobDriver_HunkerDown : JobDriver
{
    private const int GetUpCheckInterval = 60;

    private void SetPosture()
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

        int changePostureDelay = 10 + Mathf.RoundToInt(60f / Mathf.Max(0.6f, pawn.GetStatValue(StatDefOf.MoveSpeed) - 1.25f));
        Toil changePostureToil = Toils_General.Wait(changePostureDelay);
        changePostureToil.AddFinishAction(SetPosture);

        //Hunkering toil
        Toil toilNothing = new Toil();
        toilNothing.defaultCompleteMode = ToilCompleteMode.Delay;
        toilNothing.defaultDuration = GetUpCheckInterval;

        // Start Toils
        yield return changePostureToil;
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
