using System;
using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended
{
	/// <summary>
	/// This class gets used when a Pawn returning from a Caravan is unloading it's own inventory.
	/// Class is mostly copied from Rimworld with adjustments to have it ask components of CombatExtended for what and how much to drop from the player's own invetory.
	/// </summary>
	public class JobDriver_UnloadYourInventory : JobDriver
	{
		private const TargetIndex ItemToHaulInd = TargetIndex.A;

		private const TargetIndex StoreCellInd = TargetIndex.B;

		private const int UnloadDuration = 10;
		
		private int amountToDrop;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref amountToDrop, "amountToDrop", -1, false);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        [DebuggerHidden]
		protected override IEnumerable<Toil> MakeNewToils()
		{
			yield return Toils_General.Wait(10);
			yield return new Toil
			{
				initAction = delegate
				{
					Thing dropThing;
					int dropCount;
					if (!this.pawn.inventory.UnloadEverything || !this.pawn.GetAnythingForDrop(out dropThing, out dropCount))
					{
						this.EndJobWith(JobCondition.Succeeded);
					}
					else
					{
						IntVec3 c;
						if (!StoreUtility.TryFindStoreCellNearColonyDesperate(dropThing, this.pawn, out c))
						{
							this.pawn.inventory.innerContainer.TryDrop(dropThing, this.pawn.Position, this.pawn.Map, ThingPlaceMode.Near, dropCount, out dropThing);
							this.EndJobWith(JobCondition.Succeeded);
						}
						else
						{
							pawn.CurJob.SetTarget(TargetIndex.A, dropThing);
							pawn.CurJob.SetTarget(TargetIndex.B, c);
							amountToDrop = dropCount;
						}
					}
				}
			};
			yield return Toils_Reserve.Reserve(TargetIndex.B, 1);
			yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.Touch);
			yield return new Toil
			{
				initAction = delegate
				{
					Thing thing = pawn.CurJob.GetTarget(TargetIndex.A).Thing;
					if (thing == null || !this.pawn.inventory.innerContainer.Contains(thing))
					{
						this.EndJobWith(JobCondition.Incompletable);
						return;
					}
					if (!this.pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) || !thing.def.EverStorable(true))
					{
						this.pawn.inventory.innerContainer.TryDrop(thing, this.pawn.Position, this.pawn.Map, ThingPlaceMode.Near, amountToDrop, out thing);
						this.EndJobWith(JobCondition.Succeeded);
					}
					else
					{
						this.pawn.inventory.innerContainer.TryTransferToContainer(thing, this.pawn.carryTracker.innerContainer, amountToDrop, out thing);
						pawn.CurJob.count = amountToDrop;
						pawn.CurJob.SetTarget(TargetIndex.A, thing);
					}
					thing.SetForbidden(false, false);
					
					if (!this.pawn.HasAnythingForDrop())
					{
						this.pawn.inventory.UnloadEverything = false;
					}
				}
			};
			Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
			yield return carryToCell;
			yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, carryToCell, true);
		}
	}
}
