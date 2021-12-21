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
	public class BipodComp : CompRangedGizmoGiver
	{
        #region Fields
        public bool ShouldSetUpint = false;

		public bool IsSetUpRn;

		private int starts = 5;

        #endregion

        #region Properties
        public bool ShouldSetUp
		{
			get
			{
				bool result = false;
				if (Controller.settings.AutoSetUp)
				{
					var varA = this.parent.TryGetComp<CompFireModes>();
					result = ((varA.CurrentAimMode == Props.catDef.autosetMode) | !Props.catDef.useAutoSetMode) && !IsSetUpRn;
				}
				else
				{
					result = ShouldSetUpint && !IsSetUpRn;
				}

				return result;
			}
			set
			{

			}
		}
        #endregion

        #region Methods
        public override void PostExposeData()
		{
			Scribe_Values.Look<bool>(ref IsSetUpRn, "isBipodSetUp");
			base.PostExposeData();
		}
		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (Controller.settings.BipodMechanics)
			{
				if (Controller.settings.AutoSetUp)
				{
					if (this.ParentHolder is Pawn_EquipmentTracker)
					{
						Pawn dad = ((Pawn_EquipmentTracker)this.ParentHolder).pawn;
						if (dad.Drafted)
						{
							if (IsSetUpRn)
							{
								yield return new Command_Action
								{
									action = delegate {},
									defaultLabel = "Bipod IS set up",
									icon = ContentFinder<Texture2D>.Get("UI/Buttons/open_bipod")
								};
							}
							else
							{
								yield return new Command_Action
								{
									action = delegate { },
									defaultLabel = "Bipod IS NOT set up",
									icon = ContentFinder<Texture2D>.Get("UI/Buttons/closed_bipod")
								};
							}
						}
					}
				}
				else
				{
					if (this.ParentHolder is Pawn_EquipmentTracker)
					{
						Pawn dad = ((Pawn_EquipmentTracker)this.ParentHolder).pawn;
						if (dad.Drafted)
						{
							if (!ShouldSetUpint)
							{
								yield return new Command_Action
								{
									action = delegate { ShouldSetUpint = true; },
									defaultLabel = "Set up bipod",
									icon = ContentFinder<Texture2D>.Get("UI/Buttons/open_bipod")
								};
							}
							else
							{
								yield return new Command_Action
								{
									action = delegate { ShouldSetUpint = false; },
									defaultLabel = "Close bipod",
									icon = ContentFinder<Texture2D>.Get("UI/Buttons/closed_bipod")
								};

							}
						}

					}
				}
				
			}
			


		}
		public override void Notify_Unequipped(Pawn pawn)
		{
			IsSetUpRn = false;
		}

		public CompProperties_BipodComp Props => (CompProperties_BipodComp)this.props;

		public VerbPropertiesCE CopyVerbPropsFromThing(Thing source)
		{
			return (VerbPropertiesCE)source.TryGetComp<CompEquippable>().PrimaryVerb.verbProps.MemberwiseClone();
		}

		public void AssignVerbProps(Thing target, VerbPropertiesCE props)
		{
			target.TryGetComp<CompEquippable>().PrimaryVerb.verbProps = props;
		}

		public void ResetVerbProps(Thing source)
		{
			var VerbPropsCloneDef = (VerbPropertiesCE)source.def.verbs.Find(x => x is VerbPropertiesCE).MemberwiseClone();

			AssignVerbProps(target: source, props: VerbPropsCloneDef);
		}

		public void SetUpInvert(Thing source)
		{
			ResetVerbProps(source: source);

			IsSetUpRn = false;

			var changed = CopyVerbPropsFromThing(source);

			changed.recoilAmount *= Props.recoilMultoff;

			changed.warmupTime *= Props.warmupPenalty;

			AssignVerbProps(source, changed);
		}

		public void SetUpEnd(Thing source)
		{
			var changed = CopyVerbPropsFromThing(source);

			changed.range += Props.additionalrange;

			changed.recoilAmount *= Props.recoilMulton;

			changed.warmupTime *= Props.warmupMult;

			IsSetUpRn = true;

			AssignVerbProps(source, changed);
		}

		public void SetUpStart(Pawn pawn = null)
		{
			starts--;

			if (starts == 0)
			{
				if (pawn != null && (pawn.jobs?.curJob?.def?.HasModExtension<JobDefBipodCancelExtension>() ?? false) && !pawn.pather.Moving && ShouldSetUp)
				{
					pawn.jobs.StopAll();
					pawn.jobs.StartJob(new Job { def = BipodDefsOfs.JobDef_SetUpBipod, targetA = this.parent }, JobCondition.InterruptForced);
				}

				if (pawn.pather.Moving)
				{
					SetUpInvert(this.parent);
	
				}
				starts = 5;
			}


		}
        #endregion
    }
}
