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
	[StaticConstructorOnStartup]
	public class Bipod_Injector
	{
		static Bipod_Injector()
		{
			Add_and_change_all();
			AddJobModExts();
		}

		public static void Add_and_change_all()
		{
			foreach (BipodCategoryDef bipod_def in DefDatabase<BipodCategoryDef>.AllDefs)
			{
				List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(k => (k.weaponTags?.Any(O => O == bipod_def.bipod_id) ?? false) && (!k.Verbs?.Any(P => P.verbClass == typeof(CompProperties_BipodComp)) ?? false));
				foreach (ThingDef def in defs)
				{
					if (def.Verbs?.Any(PP => PP.verbClass == typeof(Verb_ShootCE)) ?? false)
					{
						var dar = def.Verbs.Find(PP => PP.verbClass == typeof(Verb_ShootCE)).MemberwiseClone();

						if (dar != null)
						{
							dar.verbClass = typeof(Verb_ShootCE);
							def.Verbs.Clear();
							def.comps.Add(new CompProperties_BipodComp { catDef = bipod_def, swayMult = bipod_def.swayMult, swayPenalty = bipod_def.swayPenalty, additionalrange = bipod_def.ad_Range, recoilMulton = bipod_def.recoil_mult_setup, recoilMultoff = bipod_def.recoil_mult_NOT_setup, ticksToSetUp = bipod_def.setuptime, warmupMult = bipod_def.warmup_mult_setup, warmupPenalty = bipod_def.warmup_mult_NOT_setup });
							def.Verbs.Add(dar);
							def.statBases.Add(new StatModifier { value = 0f, stat = BipodDefsOfs.BipodStats});
						}
					}
					else
					{
						Log.Message("adding bipod failed in " + def.defName.Colorize(Color.red) + ". It appears to have no VerbShootCE in verbs. It's verbs are following:");
						foreach (VerbProperties verbp in def.Verbs)
						{
							Log.Message(verbp.verbClass.Name.Colorize(Color.magenta));
						}

					}
				}
			}
		}

		public static void AddJobModExts()
		{
			List<JobDef> defs = DefDatabase<JobDef>.AllDefsListForReading.FindAll(x => x.defName.Contains("Wait") | x.defName.Contains("wait"));

			foreach (JobDef def in defs)
			{
				if (def.modExtensions == null)
				{
					def.modExtensions = new List<DefModExtension>();
				}

				def.modExtensions.Add(new JobDefBipodCancelExtension());
			}
		}

	}
}
