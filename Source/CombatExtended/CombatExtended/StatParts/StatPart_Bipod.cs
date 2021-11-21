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
	public class Bipod_Recoil_StatPart : StatPart
	{
		public override void TransformValue(StatRequest req, ref float val)
		{
			var varA = req.Thing.TryGetComp<bipodcomp>();
			if (varA != null)
			{
				if (varA.IsSetUpRn == true)
				{
					val *= varA.Props.recoilmulton;
				}
				else
				{
					val *= varA.Props.recoilmultoff;
				}
			}
		}

		public override string ExplanationPart(StatRequest req)
		{
			var varA = req.Thing.TryGetComp<bipodcomp>();
			if (varA != null)
			{
				if (varA.IsSetUpRn == true)
				{
					return "Bipod is set up - " + varA.Props.recoilmulton.ToString().Colorize(Color.blue);
				}
				else
				{
					return "Bipod is NOT set up - " + varA.Props.recoilmultoff.ToString().Colorize(Color.blue);
				}
			}
			else
			{
				return "";
			}
		}
	}
}
