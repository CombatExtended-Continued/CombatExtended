using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace CombatExtended.AI
{
	public abstract class SquadPathMover
	{
		public enum Events
		{
			go_Job,
			set_Up,
			squad_Changed,
			canceled_Job,
		}

		public enum JobTicks
		{
			moving
			ideal,
			finding_Path,
			new_job,
			free
		}

		public enum JobTactics
		{
			move,
			avoid,
			attack,
			hold_postion,
			hold_fire,
			findCover,
			free
		}

		private long TICK_COUNTER = 0;

		private Region Target;

		private readonly Map map;

		private readonly SquadBrain context;

		public SquadPathMover(Map map,SquadBrain context){
			this.map = map;
			this.context = context;
		}

		public virtual void TickAction(Events states)
		{
			switch (states)
			{
				case Events.go_Job:

					break;
				case Events.set_Up:

					break;
				case Events.canceled_Job:

					break;
				case Events.squad_Changed:

					break;
			}
		}



	}
}
