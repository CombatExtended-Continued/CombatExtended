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

namespace CombatExtended.Compatibility
{
	[StaticConstructorOnStartup]
	public class Bipod_Injector
	{
		static Bipod_Injector()
		{
			Add_and_change_all();
		}

		public static void Add_and_change_all()
		{
			foreach (BipodCategoryDef bipod_def in DefDatabase<BipodCategoryDef>.AllDefs)
			{
				List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(k => (k.weaponTags?.Any(O => O == bipod_def.bipod_id) ?? false) && (!k.Verbs?.Any(P => P.verbClass == typeof(CompProperties_BipodComp)) ?? false));
				foreach (ThingDef def in defs)
				{
					Log.Message("adding bipod (" + bipod_def.label + ") to: " + def.defName.Colorize(Color.cyan));
					if (def.Verbs?.Any(PP => PP.verbClass == typeof(Verb_ShootCE)) ?? false)
					{
						var dar = def.Verbs.Find(PP => PP.verbClass == typeof(Verb_ShootCE)).MemberwiseClone();

						if (dar != null)
						{
							dar.verbClass = typeof(Verb_ShootWithBipod);
							def.Verbs.Clear();
							def.comps.Add(new CompProperties_BipodComp { swaymult = bipod_def.swaymult, swaypenalty = bipod_def.swaypenalty, additionalrange = bipod_def.ad_Range, recoilmulton = bipod_def.recoil_mult_setup, recoilmultoff = bipod_def.recoil_mult_NOT_setup, TicksToSetUp = bipod_def.setuptime, warmupmult = bipod_def.warmup_mult_setup, warmuppenalty = bipod_def.warmup_mult_NOT_setup });
							def.Verbs.Add(dar);
							Log.Message("sucessfully added bipod (" + bipod_def.label + ") to: " + def.label.Colorize(bipod_def.log_color));
						}
						else
						{
							Log.Message("adding bipod failed in " + def.label.Colorize(Color.red) + ". It appears to have no VerbShootCE in verbs");
						}
					}
					else
					{
						Log.Message("adding bipod failed in " + def.label.Colorize(Color.red) + ". It appears to have no VerbShootCE in verbs. It's verbs are following:");
						foreach (VerbProperties verbp in def.Verbs)
						{
							Log.Message(verbp.verbClass.Name.Colorize(Color.magenta));
						}

					}
				}
			}
		}

	}
}
