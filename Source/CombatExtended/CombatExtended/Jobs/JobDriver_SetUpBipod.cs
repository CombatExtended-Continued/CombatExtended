using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CombatExtended;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace CombatExtended
{
	public class JobDriver_SetUpBipod : JobDriver
	{
		private ThingWithComps weapon
		{
			get
			{
				return base.TargetThingA as ThingWithComps;
			}
		}

		private const TargetIndex guntosetup = TargetIndex.A;
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return this.pawn.Reserve(this.job.GetTarget(guntosetup), this.job, 1, -1, null);
		}
		public BipodComp Bipod
		{
			get
			{
				return this.weapon.TryGetComp<BipodComp>();
			}
		}
		public override IEnumerable<Toil> MakeNewToils()
		{
			var Pawn = GetActor();

			int timeToSetUpTrue = (int)(Bipod.Props.ticksToSetUp / (Pawn.GetStatValue(StatDefOf.ShootingAccuracyPawn) * 0.5f));

			var cells = Pawn.CellsAdjacent8WayAndInside();

			bool hasCover = false;

			foreach (IntVec3 vec in cells)
			{
				if (!hasCover)
				{
					hasCover = vec.GetThingList(Pawn.Map).Any(x => x.def.fillPercent >= 0.2f && x.def.fillPercent < 1f);

					if (hasCover)
					{
						Log.Message(vec.GetThingList(Pawn.Map).Find(x => x.def.fillPercent >= 0.2f && x.def.fillPercent < 1f).Label);
					}
				}
			}

			yield return Toils_General.Wait(timeToSetUpTrue).WithProgressBarToilDelay(TargetIndex.A);
			yield return Toils_General.Do(delegate { Bipod.SetUpEnd(weapon); });


		}
	}
}
