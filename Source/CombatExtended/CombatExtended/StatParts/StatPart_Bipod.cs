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
			var varA = req.Thing.TryGetComp<BipodComp>();
			if (Controller.settings.BipodMechanics)
			{
				if (varA != null)
				{
					if (varA.IsSetUpRn == true)
					{
						val *= varA.Props.recoilMulton;
					}
					else
					{
						val *= varA.Props.recoilMultoff;
					}
				}
			}
			
		}

		public override string ExplanationPart(StatRequest req)
		{
			var varA = req.Thing.TryGetComp<BipodComp>();
			if (varA != null)
			{
				if (varA.IsSetUpRn == true)
				{
					return "Bipod IS set up -" + " x".Colorize(Color.green) + varA.Props.recoilMulton.ToString().Colorize(Color.green);
				}
				else
				{
					return "Bipod is NOT set up -" + " x".Colorize(Color.red) + varA.Props.recoilMultoff.ToString().Colorize(Color.red);
				}
			}
			else
			{
				return "";
			}
		}
	}


	public class Bipod_Sway_StatPart : StatPart
	{
		public override void TransformValue(StatRequest req, ref float val)
		{
			var varA = req.Thing.TryGetComp<BipodComp>();
			if (Controller.settings.BipodMechanics)
			{
				if (varA != null)
				{
					if (varA.IsSetUpRn == true)
					{
						val *= varA.Props.swayMult;
					}
					else
					{
						val *= varA.Props.warmupPenalty;
					}
				}
			}

		}

		public override string ExplanationPart(StatRequest req)
		{
			var varA = req.Thing.TryGetComp<BipodComp>();
			if (varA != null)
			{
				if (varA.IsSetUpRn == true)
				{
					return "Bipod IS set up -" + " x".Colorize(Color.green) + varA.Props.swayMult.ToString().Colorize(Color.green);
				}
				else
				{
					return "Bipod is NOT set up - " + "x".Colorize(Color.red) + varA.Props.swayPenalty.ToString().Colorize(Color.red);
				}
			}
			else
			{
				return "";
			}
		}
	}
}
