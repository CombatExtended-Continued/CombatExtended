using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.CombatExtended.StatParts
{
	public class StatPart_Ammo : StatPart
	{
		public override void TransformValue(StatRequest req, ref float val)
		{
			float num;

			bool HasAmUser = req.Thing.TryGetComp<CompAmmoUser>() != null;

			if (HasAmUser)
			{
				bool HasAmmo = req.Thing.TryGetComp<CompAmmoUser>().CurrentAmmo != null;
				if (HasAmmo)
				{
					num = req.Thing.TryGetComp<CompAmmoUser>().CurrentAmmo.RecoilMult;
					val *= num;
					Log.Message(num.ToString());
				}
				
			}
		}

		public override string ExplanationPart(StatRequest req)
		{
			float f;

			bool HasAmUser = req.Thing.TryGetComp<CompAmmoUser>() != null;

			if (HasAmUser)
			{
				bool HasAmmo = req.Thing.TryGetComp<CompAmmoUser>().CurrentAmmo != null;
				if (HasAmmo)
				{
					f = req.Thing.TryGetComp<CompAmmoUser>().CurrentAmmo.RecoilMult;
					return "Current ammunition: " + f.ToString();
				}

					
			}
			return null;
		}

	}
}
