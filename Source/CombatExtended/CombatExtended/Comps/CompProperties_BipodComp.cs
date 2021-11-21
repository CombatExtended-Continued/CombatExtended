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
	public class CompProperties_BipodComp : CompProperties
	{


		public int additionalrange = 12;

		public float recoilmulton = 0.5f;

		public float recoilmultoff = 1f;

		public float warmupmult = 0.85f;

		public float warmuppenalty = 2f;

		public int TicksToSetUp = 60;



		public CompProperties_BipodComp()
		{
			this.compClass = typeof(bipodcomp);
		}

		public CompProperties_BipodComp(Type compClass) : base(compClass)
		{
			this.compClass = compClass;
		}

	}
}
