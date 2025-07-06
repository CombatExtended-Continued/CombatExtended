using System.Collections.Generic;
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
        public bool ShouldSetUpint;
        private CompFireModes compFireMode;

        public bool IsSetUpRn;

        private int starts = 5;

        #endregion

        #region Properties
        public bool ShouldSetUp
        {
            get
            {
                if (Controller.settings.AutoSetUp && compFireMode != null)
                {
                    Pawn pawn = ((Pawn_EquipmentTracker)this.ParentHolder).pawn;
                    return (((compFireMode.CurrentAimMode == Props.catDef.autosetMode) | (!Props.catDef.useAutoSetMode && compFireMode.CurrentAimMode != AimMode.Snapshot)) && !IsSetUpRn && !pawn.IsCarryingPawn());
                }
                return ShouldSetUpint && !IsSetUpRn;
            }
        }
        #endregion

        #region Methods

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compFireMode = this.parent.TryGetComp<CompFireModes>();
            SetUpInvert(this.parent);
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref IsSetUpRn, "isBipodSetUp");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                compFireMode = this.parent.TryGetComp<CompFireModes>();
                if (Controller.settings.BipodMechanics && compFireMode != null)
                {

                    if (IsSetUpRn)
                    {
                        SetupFromLoad();
                    }
                    else
                    {
                        SetUpInvert(this.parent);
                    }
                }
            }
            base.PostExposeData();
        }

        [Compatibility.Multiplayer.SyncMethod]
        public void DeployUpBipod() => ShouldSetUpint = true;

        [Compatibility.Multiplayer.SyncMethod]
        public void CloseBipod()
        {
            ShouldSetUpint = false;
            IsSetUpRn = false;
            SetUpInvert(parent);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!Controller.settings.BipodMechanics || this.ParentHolder is not Pawn_EquipmentTracker tracker)
            {
                yield break;
            }
            Pawn dad = tracker.pawn;
            if (!dad.Drafted)
            {
                yield break;
            }
            if (Controller.settings.AutoSetUp)
            {
                if (compFireMode == null)
                {
                    yield return new Command_Action
                    {
                        action = ShouldSetUpint ? CloseBipod : DeployUpBipod,
                        defaultLabel = ShouldSetUpint ? "CE_Close_Bipod".Translate() : "CE_Deploy_Bipod".Translate(),
                        icon = ContentFinder<Texture2D>.Get(ShouldSetUpint ? "UI/Buttons/closed_bipod" : "UI/Buttons/open_bipod")
                    };

                }
                else
                {
                    yield return new Command_Action
                    {
                        action = delegate { },
                        defaultLabel = IsSetUpRn ? "CE_Bipod_Set_Up".Translate() : "CE_Bipod_Not_Set_Up".Translate(),
                        icon = ContentFinder<Texture2D>.Get(IsSetUpRn ? "UI/Buttons/open_bipod" : "UI/Buttons/closed_bipod")
                    };
                }
            }
            else
            {
                yield return new Command_Action
                {
                    action = ShouldSetUpint ? CloseBipod : DeployUpBipod,
                    defaultLabel = ShouldSetUpint ? "CE_Close_Bipod".Translate() : "CE_Deploy_Bipod".Translate(),
                    icon = ContentFinder<Texture2D>.Get(ShouldSetUpint ? "UI/Buttons/closed_bipod" : "UI/Buttons/open_bipod")
                };
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            IsSetUpRn = false;
            ResetVerbProps(this.parent);
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

            CE_SoundDefOf.Interact_Bipod.PlayOneShot(new TargetInfo(source.PositionHeld, source.Map));
        }

        public void SetUpStart(Pawn pawn = null)
        {
            if (!(pawn?.Drafted ?? false))
            {
                return;
            }

            starts--;

            if (starts == 0)
            {
                if ((pawn.jobs?.curJob?.def?.HasModExtension<JobDefBipodCancelExtension>() ?? false) && !pawn.pather.MovingNow && ShouldSetUp)
                {
                    pawn.jobs.StopAll();
                    pawn.jobs.StartJob(new Job { def = CE_JobDefOf.JobDef_SetUpBipod, targetA = this.parent }, JobCondition.InterruptForced);
                }

                if (pawn.pather.MovingNow)
                {
                    SetUpInvert(this.parent);
                }
                starts = 5;
            }
        }

        public void SetupFromLoad()
        {
            VerbPropertiesCE changed = CopyVerbPropsFromThing(this.parent);
            changed.range += Props.additionalrange;
            changed.recoilAmount *= Props.recoilMulton * Props.recoilMultoff;
            changed.warmupTime *= Props.warmupMult * Props.warmupPenalty;
            AssignVerbProps(this.parent, changed);
        }
        #endregion
    }
}
