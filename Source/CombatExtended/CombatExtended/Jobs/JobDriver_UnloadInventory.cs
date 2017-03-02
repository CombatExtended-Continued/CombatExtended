using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Verse;
using Verse.AI;
using RimWorld;

namespace CombatExtended
{
	public class JobDriver_UnloadInventory : JobDriver
	{
		private const TargetIndex OtherPawnInd = TargetIndex.A;

		private const TargetIndex ItemToHaulInd = TargetIndex.B;

		private const TargetIndex StoreCellInd = TargetIndex.C;

		private const int UnloadDuration = 10;

		private Pawn OtherPawn
		{
			get
			{
				return (Pawn)CurJob.GetTarget(OtherPawnInd).Thing;
			}
		}
		
		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(OtherPawnInd);
			
			yield return Toils_Reserve.Reserve(OtherPawnInd);
			
			yield return Toils_Goto.GotoThing(OtherPawnInd, PathEndMode.Touch);
			
			yield return Toils_General.Wait(10);
			
			var dropOrStartCarrying = new Toil();
			
			dropOrStartCarrying.initAction = delegate
			{
				Pawn otherPawn = OtherPawn;
				if (!otherPawn.inventory.UnloadEverything || otherPawn.inventory.innerContainer.Count == 0)
				{
					EndJobWith(JobCondition.Succeeded);
				}
				else
				{
					int stackSize = -1;
					Thing thing = otherPawn.RandomNonLoadoutNonEquipment(out stackSize);
					switch (stackSize)
					{
						case -2:
							stackSize = thing.stackCount;
							break;
						case -1:
							otherPawn.inventory.UnloadEverything = false;
							EndJobWith(JobCondition.Succeeded);
							break;
					}
					IntVec3 c;
					if (!thing.def.EverStoreable || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) || !StoreUtility.TryFindStoreCellNearColonyDesperate(thing, pawn, out c))
					{
						otherPawn.inventory.innerContainer.TryDrop(thing, ThingPlaceMode.Near, out thing, null);
						EndJobWith(JobCondition.Succeeded);
					}
					else
					{
						otherPawn.inventory.innerContainer.TransferToContainer(thing, pawn.carryTracker.innerContainer, stackSize, out thing);
						CurJob.count = stackSize;
						CurJob.SetTarget(ItemToHaulInd, thing);
						CurJob.SetTarget(StoreCellInd, c);
					}
					thing.SetForbidden(false, false);
					if (otherPawn.inventory.innerContainer.Count == 0)
					{
						otherPawn.inventory.UnloadEverything = false;
					}
				}
			};
			
			yield return dropOrStartCarrying;
			
			yield return Toils_Reserve.Reserve(StoreCellInd);
			
			Toil carryToCell = Toils_Haul.CarryHauledThingToCell(StoreCellInd);
			
			yield return carryToCell;
			
			yield return Toils_Haul.PlaceHauledThingInCell(StoreCellInd, carryToCell, true);
		}
	}
}
