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
			
			var pawnSkill = Pawn.GetStatValue(StatDefOf.ShootingAccuracyPawn) * 0.5f;
			
			if(pawnSkill == 0)
				pawnSkill = 0.25f;

			int timeToSetUpTrue = Mathf.Clamp((int)(Bipod.Props.ticksToSetUp / ( pawnSkill )), 1, Bipod.Props.ticksToSetUp * 3);

			var cells = Pawn.CellsAdjacent8WayAndInside();

			//Commented out as it wasn't used before, but also contains a fix for it at line 57 in case it is accepted. Log.Message was 
			/*
			bool hasCover = false;

			foreach (IntVec3 vec in cells)
			{
				if (!hasCover)
				{
					hasCover = vec.GetThingList(Pawn.Map).Any(x => x.def.fillPercent >= 0.2f && x.def.fillPercent < 1f);

					if (hasCover)
					{
						timeToSetUpTrue = Mathf.Max(timeToSetUpTrue - 60, 1);
						Log.Message(vec.GetThingList(Pawn.Map).Find(x => x.def.fillPercent >= 0.2f && x.def.fillPercent < 1f).Label);
					}
				}
			}*/

			yield return Toils_General.Wait(timeToSetUpTrue).WithProgressBarToilDelay(TargetIndex.A);
			yield return Toils_General.Do(delegate { Bipod.SetUpEnd(weapon); });


		}
	}
}
