using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
    /* Regarding multi reservation...
     * As long as it's all the same job reserving the animal (sourceInd) it works out fine.
     * Need to check if the target still has the desired thing because if a pawn can take the entire stack then the thing object's container is changed rather than destroyed.
     * It's when a pawn takes a partial stack that new thing objects are created and the difference is deducted from the previously existing thing's stack count.
     */
    public class JobDriver_TakeFromOther : JobDriver
    {
        private TargetIndex thingInd = TargetIndex.A;
        private TargetIndex sourceInd = TargetIndex.B;
        private TargetIndex flagInd = TargetIndex.C;

        /// <summary>
        /// Property that converts TargetIndex.A into a Thing object.
        /// </summary>
        private Thing targetItem
        {
            get
            {
                return job.GetTarget(thingInd).Thing;
            }
        }
        /// <summary>
        /// Property that converts TargetIndex.B into a Thing object.
        /// </summary>
		private Pawn takePawn
        {
            get
            {
                return (Pawn)job.GetTarget(sourceInd).Thing;
            }
        }

        /// <summary>
        /// Property which is used to indicate that the job was created with the expectation that the Pawn doing the job is to equip the thing they are taking.
        /// </summary>
        /// <remarks>
        /// The test in this case looks to see if Target.C was given a Thing (will be the takePawn but any thing/pawn will do) vs being given null
        /// (in which case doesn't have a thing).  See the JobGiver_UpdateLoadout.cs file for how this is set as it's rather non-standard but we
        /// needed a way to store a bool value that could be saved into a Job.
        /// </remarks>
		private bool doEquip
        {
            get
            {
                return job.GetTarget(flagInd).HasThing;
            }
        }

        /// <summary>
        /// Generates the Job Report string displayed when clicking on a pawn working on this job.
        /// </summary>
        /// <returns>string of the generated report.</returns>
		public override string GetReport()
        {
            string text = CE_JobDefOf.TakeFromOther.reportString;
            text = text.Replace("FlagC", doEquip ? "CE_TakeFromOther_Equipping".Translate() : "CE_TakeFromOther_Taking".Translate());
            text = text.Replace("TargetA", targetItem.Label);
            text = text.Replace("TargetB", takePawn.LabelShort);
            return text;
        }

        /// <summary>
        /// A fail condition, if the takePawn is dead it no longer has a container.
        /// </summary>
        /// <returns>bool, true indicates the takePawn is dead.</returns>
        private bool DeadTakePawn()
        {
            return takePawn.Dead;
        }

        /// <summary>
        /// Walks the linked list from a Thing's holdingOwner.Owner back up to a Pawn, or null, and returns the result.
        /// </summary>
        /// <param name="container">IThingHolder type which can be a container of some sort or a pawn.</param>
        /// <returns>Pawn type which can be null if we didn't end up with a pawn at the top.</returns>
        private Pawn RootHolder(IThingHolder container)
        {
            IThingHolder holder = container;
            while (holder != null && (holder as Pawn) == null)
                holder = holder.ParentHolder;
            return holder as Pawn;
        }

        /// <summary>
        /// Handles the various yield returns that make up an iterated toil sequence.
        /// </summary>
        /// <returns>IEnumberable of Toil containing the sequence of actions the Pawn should take to fulfill the JobDriver's task.</returns>
		public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(sourceInd);
            this.FailOnDestroyedOrNull(thingInd);
            this.FailOn(DeadTakePawn);
            // We could set a slightly more sane value here which would prevent a hoard of pawns moving from pack animal to pack animal...
            // Also can enforce limits via JobGiver keeping track of how many things it's given away from each pawn, it's a small case though...
            yield return Toils_Reserve.Reserve(sourceInd, int.MaxValue, 0, null);
            yield return Toils_Goto.GotoThing(sourceInd, PathEndMode.Touch);
            yield return Toils_General.Wait(10);

            yield return new Toil
            {
                initAction = delegate
                {
                    // if the targetItem is no longer in the takePawn's inventory then another pawn already took it and we fail...
                    if (takePawn == RootHolder(targetItem.holdingOwner.Owner))
                    {
                        int amount = targetItem.stackCount < pawn.CurJob.count ? targetItem.stackCount : pawn.CurJob.count;
                        takePawn.inventory.innerContainer.TryTransferToContainer(targetItem, pawn.inventory.innerContainer, amount);
                        if (doEquip)
                        {
                            CompInventory compInventory = pawn.TryGetComp<CompInventory>();
                            if (compInventory != null)
                                compInventory.TrySwitchToWeapon((ThingWithComps)targetItem);
                        }
                    }
                    else
                    {
                        this.EndJobWith(JobCondition.Incompletable);
                    }
                }
            };
            yield return Toils_Reserve.Release(sourceInd);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
    }
}