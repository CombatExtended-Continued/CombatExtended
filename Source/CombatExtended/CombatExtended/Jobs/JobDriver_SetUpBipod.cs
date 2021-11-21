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
		public bipodcomp Bipod
		{
			get
			{
				return this.weapon.TryGetComp<bipodcomp>();
			}
		}
		public override IEnumerable<Toil> MakeNewToils()
		{
			yield return Toils_General.Wait(Bipod.Props.TicksToSetUp);
			yield return Toils_General.Do(delegate { Bipod.IsSetUpRn = true; });


		}
	}
}
