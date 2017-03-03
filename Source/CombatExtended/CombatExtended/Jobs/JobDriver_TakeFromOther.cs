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
	public class JobDriver_TakeFromOther : JobDriver
	{
		private TargetIndex thingInd = TargetIndex.A;
		private TargetIndex sourceInd = TargetIndex.B;
		private TargetIndex flagInd = TargetIndex.C;
		
		private Thing targetItem
		{
			get {
				return CurJob.GetTarget(thingInd).Thing;
			}
		}
		private Pawn takePawn
		{
			get {
				return (Pawn)CurJob.GetTarget(sourceInd).Thing;
			}
		}
		private bool doEquip
		{
			get
			{
				return CurJob.GetTarget(flagInd).HasThing;
			}
		}

		public override string GetReport()
		{
			string text = CE_JobDefOf.TakeFromOther.reportString;
			text = text.Replace("FlagC", doEquip ? "CE_TakeFromOther_Equipping".Translate() : "CE_TakeFromOther_Taking".Translate());
			text = text.Replace("TargetA", targetItem.Label);
			text = text.Replace("TargetB", takePawn.LabelShort);
			return text;
		}

		
		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(sourceInd);
			yield return Toils_Reserve.Reserve(sourceInd);
			yield return Toils_Goto.GotoThing(sourceInd, PathEndMode.Touch);
			yield return Toils_General.Wait(10);
			yield return new Toil {
				initAction = delegate
				{
					takePawn.inventory.GetInnerContainer().TransferToContainer(targetItem, this.pawn.inventory.GetInnerContainer(), base.CurJob.count);
					if (doEquip)
					{
						CompInventory compInventory = this.pawn.TryGetComp<CompInventory>();
						if (compInventory != null)
							compInventory.TrySwitchToWeapon((ThingWithComps)targetItem);
					}
				}
			};
		}
	}
}