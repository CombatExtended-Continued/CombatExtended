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
	public class bipodcomp : CompRangedGizmoGiver
	{
		public bool ShouldSetUpint = false;
		public bool ShouldSetUp
		{
			get
			{
				bool result = false;
				if (Controller.settings.AutoSetUp)
				{
					var varA = this.parent.TryGetComp<CompFireModes>();
					if (varA.CurrentAimMode != AimMode.Snapshot)
					{
						return true;
					}
				}
				else
				{
					return ShouldSetUpint;
				}

				return result;
			}
			set
			{

			}
		}

		public bool IsSetUpRn;

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

		public Verb_ShootWithBipod verbbipod
		{
			get
			{
				var result = (Verb_ShootWithBipod)this.parent.TryGetComp<CompEquippable>().PrimaryVerb;
				return result;
			}
		}
		public override void Notify_Unequipped(Pawn pawn)
		{

			IsSetUpRn = false;
		}
		public override void Notify_Equipped(Pawn pawn)
		{
			IsSetUpRn = false;
			verbbipod.WereChangesApplied1 = false;
			verbbipod.WereChangesApplied2 = false;
		}

		public CompProperties_BipodComp Props => (CompProperties_BipodComp)this.props;
		public void SetUpStart(Pawn pawn = null)
		{
			if (pawn != null)
			{
				pawn.jobs.StopAll();
				pawn.jobs.StartJob(new Job { def = BipodDefsOfs.JobDef_SetUpBipod, targetA = this.parent }, JobCondition.InterruptForced);
			}

		}
	}
}
