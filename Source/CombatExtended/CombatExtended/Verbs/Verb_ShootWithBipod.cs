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
    public class Verb_ShootWithBipod : Verb_ShootCE
    {

		public bool WereChangesApplied1 = false;

		public bool WereChangesApplied2 = false;

		public VerbPropertiesCE verpbrops
		{
			get
			{
				return (VerbPropertiesCE)this.EquipmentSource.def.Verbs.Find(tt22 => tt22.verbClass == typeof(Verb_ShootWithBipod));
			}
		}

		public bipodcomp bipodcomp
		{
			get
			{
				return this.EquipmentSource.TryGetComp<bipodcomp>();
			}
		}

		public override void VerbTickCE()
		{
			if (Controller.settings.bipodMechanics)
			{
				if (CasterPawn.Drafted)
				{
					if (!(CasterPawn.ParentHolder is Map))
					{
						return;
					}
					if (CasterPawn != null)
					{
						if (!CasterPawn.pather.Moving && EquipmentSource.TryGetComp<bipodcomp>().IsSetUpRn)
						{
							if (!WereChangesApplied1)
							{
								VerbPropertiesCE VerbPropsClone = (VerbPropertiesCE)this.verbProps.MemberwiseClone();
								VerbPropsClone.warmupTime = verpbrops.warmupTime * bipodcomp.Props.warmupmult;
								VerbPropsClone.range += EquipmentSource.TryGetComp<bipodcomp>().Props.additionalrange;
								this.verbProps = VerbPropsClone;
								WereChangesApplied1 = true;
							}


						}
						if (!CasterPawn.pather.Moving && !EquipmentSource.TryGetComp<bipodcomp>().IsSetUpRn)
						{
							if (!WereChangesApplied2)
							{
								VerbPropertiesCE VerbPropsClone = (VerbPropertiesCE)this.verbProps.MemberwiseClone();
								VerbPropsClone.warmupTime = verpbrops.warmupTime * bipodcomp.Props.warmuppenalty;
								VerbPropsClone.range = verpbrops.range;
								this.verbProps = VerbPropsClone;
								WereChangesApplied2 = true;
							}

						}
						if (!CasterPawn.pather.Moving && EquipmentSource.TryGetComp<bipodcomp>().ShouldSetUp && CasterPawn.CurJob.def != BipodDefsOfs.JobDef_SetUpBipod && !bipodcomp.IsSetUpRn)
						{
							EquipmentSource.TryGetComp<bipodcomp>().SetUpStart(CasterPawn);
						}
						if (CasterPawn.pather.Moving)
						{
							WereChangesApplied1 = false;
							WereChangesApplied2 = false;
							EquipmentSource.TryGetComp<bipodcomp>().IsSetUpRn = false;
							this.verbProps = EquipmentSource.def.Verbs.Find(tt33 => tt33.verbClass == typeof(Verb_ShootWithBipod)).MemberwiseClone();
						}
					}
				}
			}
			



			base.VerbTickCE();
		}
	}
}
