using RimWorld;
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
        public CompProperties_FireModes Props
        {
            get
            {
                return (CompProperties_FireModes)props;
            }
        }

        // Fire mode variables
        private Verb verbInt = null;
        private Verb verb
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
        public Thing caster
        {
            get
            {
                return verb.caster;
            }
        }
        public Pawn casterPawn
        {
            get
            {
                return caster as Pawn;
            }
        }
        private List<FireMode> availableFireModes = new List<FireMode>(Enum.GetNames(typeof(FireMode)).Length);
        private List<AimMode> availableAimModes = new List<AimMode>(Enum.GetNames(typeof(AimMode)).Length) { AimMode.Snapshot, AimMode.AimedShot };

        private FireMode currentFireModeInt;
        public FireMode currentFireMode
        {
            get
            {
                return currentFireModeInt;
            }
        }
        private AimMode currentAimModeInt;
        public AimMode currentAimMode
        {
            get
            {
                return currentAimModeInt;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            LongEventHandler.ExecuteWhenFinished(InitAvailableFireModes);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentFireModeInt, "currentFireMode", FireMode.AutoFire);
            Scribe_Values.Look(ref currentAimModeInt, "currentAimMode", AimMode.Snapshot);
        }

        private void InitAvailableFireModes()
        {
            // Calculate available fire modes
            if (verb.verbProps.burstShotCount > 1 || Props.noSingleShot)
            {
                availableFireModes.Add(FireMode.AutoFire);
            }
            if (Props.aimedBurstShotCount > 1)
            {
                if (Props.aimedBurstShotCount >= verb.verbProps.burstShotCount)
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
            if (Props.noSnapshot) availableAimModes.Remove(AimMode.Snapshot);

            // Sanity check in case def changed
            if (!availableFireModes.Contains(currentFireModeInt) || !availableAimModes.Contains(currentAimMode))
            {
                ResetModes();
            }
        }

        /// <summary>
        /// Cycles through all available fire modes in order
        /// </summary>
        public void ToggleFireMode()
        {
            int currentFireModeNum = availableFireModes.IndexOf(currentFireModeInt);
            currentFireModeNum = (currentFireModeNum + 1) % availableFireModes.Count;
            currentFireModeInt = availableFireModes.ElementAt(currentFireModeNum);
            if (availableFireModes.Count > 1) PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_FireModes, KnowledgeAmount.Total);
        }

        public void ToggleAimMode()
        {
            int currentAimModeNum = availableAimModes.IndexOf(currentAimModeInt);
            currentAimModeNum = (currentAimModeNum + 1) % availableAimModes.Count;
            currentAimModeInt = availableAimModes.ElementAt(currentAimModeNum);
            if (availableAimModes.Count > 1) PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_AimModes, KnowledgeAmount.Total);
        }

        /// <summary>
        /// Resets the selected fire mode to the first one available (e.g. when the gun is dropped)
        /// </summary>
        public void ResetModes()
        {
            currentFireModeInt = availableFireModes.ElementAt(0);
            currentAimModeInt = availableAimModes.ElementAt(0);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (casterPawn != null && casterPawn.Faction.Equals(Faction.OfPlayer))
            {
                foreach(Command com in GenerateGizmos())
                {
                    yield return com;
                }
            }
        }

        public IEnumerable<Command> GenerateGizmos()
        {
            Command_Action toggleFireModeGizmo = new Command_Action
            {
                action = ToggleFireMode,
                defaultLabel = ("CE_" + currentFireMode.ToString() + "Label").Translate(),
                defaultDesc = "CE_ToggleFireModeDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get(("UI/Buttons/" + currentFireMode.ToString()), true),
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
                defaultLabel = ("CE_" + currentAimMode.ToString() + "Label").Translate(),
                defaultDesc = "CE_ToggleAimModeDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get(("UI/Buttons/" + currentAimMode.ToString()), true),
            };
            if (availableAimModes.Count > 1)
            {
                // Teach about aim modes
                toggleAimModeGizmo.tutorTag = "CE_AimModeToggle";
                LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_AimModes, parent, OpportunityType.GoodToKnow);
            }
            yield return toggleAimModeGizmo;
        }

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
	}
}
