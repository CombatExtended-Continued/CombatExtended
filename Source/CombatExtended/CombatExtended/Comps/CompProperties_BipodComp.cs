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

		public float recoilMulton = 0.5f;

		public float recoilMultoff = 1f;

		public float warmupMult = 0.85f;

		public float warmupPenalty = 2f;

		public float swayMult = 1f;

		public float swayPenalty = 1f;

		public int ticksToSetUp = 60;

		public BipodCategoryDef catDef;



		public CompProperties_BipodComp()
		{
			this.compClass = typeof(BipodComp);
		}

		public CompProperties_BipodComp(Type compClass) : base(compClass)
		{
			this.compClass = compClass;
		}

	}
}
