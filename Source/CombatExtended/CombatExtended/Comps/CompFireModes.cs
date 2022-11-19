﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class CompFireModes : CompRangedGizmoGiver
    {

        #region Fields

        private Verb verbInt = null;

        private List<FireMode> availableFireModes = new List<FireMode>(Enum.GetNames(typeof(FireMode)).Length);
        private List<AimMode> availableAimModes = new List<AimMode>(Enum.GetNames(typeof(AimMode)).Length) { AimMode.AimedShot };
        private FireMode currentFireModeInt;
        private AimMode currentAimModeInt;
	private bool newComp = true;
        public TargettingMode targetMode = TargettingMode.torso;

        #endregion

        #region Properties

        public CompProperties_FireModes Props
        {
            get
            {
                return (CompProperties_FireModes)props;
            }
        }

        public List<AimMode> AvailableAimModes
        {
            get
            {
                return availableAimModes;
            }
        }

        public List<FireMode> AvailableFireModes
        {
            get
            {
                return availableFireModes;
            }
        }

        // Fire mode variables
        private Verb Verb
        {
            get
            {
                if (verbInt == null)
                {
                    CompEquippable compEquippable = parent.TryGetComp<CompEquippable>();
                    if (compEquippable != null)
                    {
                        verbInt = compEquippable.PrimaryVerb;
                    }
                    else
                    {
                        Log.ErrorOnce(parent.LabelCap + " has CompFireModes but no CompEquippable", 50020);
                    }
                }                
                return verbInt;
            }
        }
        public Thing Caster
        {
            get
            {
                return Verb.caster;
            }
        }
        public Pawn CasterPawn
        {
            get
            {
                return Caster as Pawn;
            }
        }

        private bool IsTurretMannable = false;

        public float HandLing
        {
            get
            {
                if (Caster is Pawn)
                {
                    return CasterPawn.GetStatValue(StatDefOf.ShootingAccuracyPawn);
                }
                IsTurretMannable = (Caster.TryGetComp<CompMannable>() != null);
                return 0f;
            }
        }
        public FireMode CurrentFireMode
        {
            get
            {
                return currentFireModeInt;
            }
            set
            {
                currentFireModeInt = value;
            }
        }
        public AimMode CurrentAimMode
        {
            get
            {
                return currentAimModeInt;
            }
            set
            {
                currentAimModeInt = value;
            }
        }

        #endregion

        #region Methods

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            LongEventHandler.ExecuteWhenFinished(InitAvailableFireModes);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentFireModeInt, "currentFireMode", FireMode.AutoFire);
            Scribe_Values.Look(ref currentAimModeInt, "currentAimMode", AimMode.AimedShot);
            Scribe_Values.Look(ref targetMode, "currentTargettingMode", TargettingMode.torso);
	    Scribe_Values.Look(ref newComp, "newComp", false);
        }

        public void InitAvailableFireModes()
        {
            availableFireModes.Clear();
            // Calculate available fire modes
            if (parent.GetStatValue(CE_StatDefOf.BurstShotCount) > 1 || Props.noSingleShot)
            {
                availableFireModes.Add(FireMode.AutoFire);
            }
            if (Props.aimedBurstShotCount > 1)
            {
                if (Props.aimedBurstShotCount >= Verb.verbProps.burstShotCount)
                {
                    Log.Warning(parent.LabelCap + " burst fire shot count is same or higher than auto fire");
                }
                else
                {
                    availableFireModes.Add(FireMode.BurstFire);
                }
            }
            if (!Props.noSingleShot)
            {
                availableFireModes.Add(FireMode.SingleFire);
            }
            if (!Props.noSnapshot)
            {
                availableAimModes.Add(AimMode.Snapshot);
                availableAimModes.Add(AimMode.SuppressFire);
            }

            // Sanity check in case def changed
            if (newComp || !availableFireModes.Contains(currentFireModeInt) || !availableAimModes.Contains(currentAimModeInt))
            {
		newComp = false;
                ResetModes();
            }
        }

        /// <summary>
        /// Cycles through all available fire modes in order
        /// </summary>
        [Compatibility.Multiplayer.SyncMethod]
        public void ToggleFireMode()
        {
            int currentFireModeNum = availableFireModes.IndexOf(currentFireModeInt);
            currentFireModeNum = (currentFireModeNum + 1) % availableFireModes.Count;
            currentFireModeInt = availableFireModes.ElementAt(currentFireModeNum);
            if (availableFireModes.Count > 1) PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_FireModes, KnowledgeAmount.Total);
        }

        [Compatibility.Multiplayer.SyncMethod]
        public void ToggleAimMode()
        {
            int currentAimModeNum = availableAimModes.IndexOf(currentAimModeInt);
            currentAimModeNum = (currentAimModeNum + 1) % availableAimModes.Count;
            currentAimModeInt = availableAimModes.ElementAt(currentAimModeNum);
            if (availableAimModes.Count > 1) PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_AimModes, KnowledgeAmount.Total);
        }

        [Compatibility.Multiplayer.SyncMethod]
        public void ChangeTargetMode()
        {
            switch (targetMode)
            {
                case TargettingMode.torso:
                    targetMode = TargettingMode.head;
                    break;
                case TargettingMode.head:
                    targetMode = TargettingMode.legs;
                    break;
                case TargettingMode.legs:
                    targetMode = TargettingMode.automatic;
                    break;
                case TargettingMode.automatic:
                    targetMode = TargettingMode.torso;
                    break;
            }
        }

        /// <summary>
        /// Resets the selected fire mode to the first one available (e.g. when the gun is dropped)
        /// </summary>
        public void ResetModes()
        {
	    //Required since availableFireModes.Capacity is set but its contents aren't so ElementAt(0) causes errors in some instances
            if (availableFireModes.Count > 0)
                currentFireModeInt = availableFireModes.ElementAt(0);

            currentAimModeInt = Props.aiAimMode;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (CasterPawn?.Faction == Faction.OfPlayer)
            {
                foreach (Command com in GenerateGizmos())
                {
                    yield return com;
                }
            }
        }

        public Texture2D TrueIcon
        {
            get
            {
                string mode_name = "";

                switch (targetMode)
                {
                    case TargettingMode.torso:
                        mode_name = "center";
                        break;
                    case TargettingMode.legs:
                        mode_name = "legs";
                        break;
                    case TargettingMode.head:
                        mode_name = "head";
                        break;
                    case TargettingMode.automatic:
                        mode_name = "auto";
                        break;
                }

                return ContentFinder<Texture2D>.Get("UI/Buttons/Targetting/" + mode_name);
            }
        }

        public IEnumerable<Command> GenerateGizmos()
        {
            Command_Action toggleFireModeGizmo = new Command_Action
            {
                action = ToggleFireMode,
                defaultLabel = ("CE_" + currentFireModeInt.ToString() + "Label").Translate(),
                defaultDesc = "CE_ToggleFireModeDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get(("UI/Buttons/" + currentFireModeInt.ToString()), true),
                tutorTag = availableFireModes.Count > 1 ? "CE_FireModeToggle" : null
            };
            if (availableFireModes.Count > 1)
            {
                // Teach about fire modes
                toggleFireModeGizmo.tutorTag = "CE_FireModeToggle";
                LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_FireModes, parent, OpportunityType.GoodToKnow);
            }
            yield return toggleFireModeGizmo;

            Command_Action toggleAimModeGizmo = new Command_Action
            {
                action = ToggleAimMode,
                defaultLabel = ("CE_" + currentAimModeInt.ToString() + "Label").Translate(),
                defaultDesc = "CE_ToggleAimModeDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get(("UI/Buttons/" + currentAimModeInt.ToString()), true),
            };
            if (availableAimModes.Count > 1)
            {
                // Teach about aim modes
                toggleAimModeGizmo.tutorTag = "CE_AimModeToggle";
                LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_AimModes, parent, OpportunityType.GoodToKnow);
            }
            yield return toggleAimModeGizmo;


            if (CurrentAimMode != AimMode.SuppressFire)
            {
                if ( (HandLing > 2.45f) | IsTurretMannable)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Targeted area: " + targetMode,
                        defaultDesc = "",
                        icon = TrueIcon,
                        action = ChangeTargetMode
                    };
                }
            } 
        }

        /*
        public override string GetDescriptionPart()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (availableFireModes.Count > 0)
            {
                stringBuilder.AppendLine("CE_FireModes".Translate() + ": ");
                foreach (FireMode fireMode in availableFireModes)
                {
                    stringBuilder.AppendLine("   -" + ("CE_" + fireMode.ToString() + "Label").Translate());
                }
                if (Props.aimedBurstShotCount > 0 && availableFireModes.Contains(FireMode.BurstFire))
                {
                    stringBuilder.AppendLine("CE_AimedBurstCount".Translate() + ": " + GenText.ToStringByStyle(Props.aimedBurstShotCount, ToStringStyle.Integer));
                }
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }
        */

        #endregion
    }
}
