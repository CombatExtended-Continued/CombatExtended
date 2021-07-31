using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using CombatExtended.AI;

namespace CombatExtended
{
    public class CompSuppressable : ThingComp
    {
        #region Constants

        private const float minSuppressionDist = 5f;         // Minimum distance to be suppressed from, so melee won't be suppressed if it closes within this distance
        private const float maxSuppression = 1050f;          // Cap to prevent suppression from building indefinitely
        private const int TicksForDecayStart = 120;          // How long since last suppression before decay starts
        private const float SuppressionDecayRate = 5f;       // How much suppression decays per tick
        private const int TicksPerMote = 150;                // How many ticks between throwing a mote        

        private const int MinTicksUntilMentalBreak = 600;    // How long until pawn can have a mental break
        private const float ChanceBreakPerTick = 0.001f;     // How likely we are to break each tick above the threshold

        private const int HelpRequestCooldown = 1200;

        #endregion

        #region Fields

        // --------------- Location calculations ---------------

        /*
         * We track the initial location from which a pawn was suppressed and the total amount of suppression coming from that location separately.
         * That way if suppression stops coming from location A but keeps coming from location B the location will get updated without bouncing 
         * pawns or having to track fire coming from multiple locations
         */
        private int lastHelpRequestAt = -1;

        private IntVec3 suppressorLoc;
        private float locSuppressionAmount = 0f;

        private float currentSuppression = 0f;
        public bool isSuppressed = false;

        private int ticksUntilDecay = 0;
        private int ticksHunkered;

        private bool isCrouchWalking;

        #endregion

        #region Properties

        public CompProperties_Suppressable Props => (CompProperties_Suppressable)props;
        public IntVec3 SuppressorLoc => suppressorLoc;

        public float CurrentSuppression => currentSuppression;
        private float SuppressionThreshold
        {
            get
            {
                float threshold = 0f;
                Pawn pawn = parent as Pawn;
                if (pawn != null)
                {
                    //Get morale
                    float hardBreakThreshold = pawn.mindState?.mentalBreaker?.BreakThresholdMajor ?? 0;
                    float currentMood = pawn.needs?.mood?.CurLevel ?? 0.5f;
                    threshold = Mathf.Sqrt(Mathf.Max(0, currentMood - hardBreakThreshold)) * maxSuppression * 0.125f;
                }
                else
                {
                    Log.Error("CE tried to get suppression threshold of non-pawn");
                }
                return threshold;
            }
        }

        private CompInventory _compInventory = null;
        private CompInventory CompInventory
        {
            get
            {
                if (_compInventory == null) _compInventory = parent.TryGetComp<CompInventory>();
                return _compInventory;
            }
        }

        public bool IsHunkering
        {
            get
            {
                if (currentSuppression > (SuppressionThreshold * 10))
                {
                    if (isSuppressed)
                    {
                        return true;
                    }
                    // Removing suppression log
                    else
                    {
                        Log.Error("CE hunkering without suppression, this should never happen");
                    }
                }
                return false;
            }
        }
        public bool CanReactToSuppression
        {
            get
            {
                Pawn pawn = parent as Pawn;
                return !pawn.Position.InHorDistOf(SuppressorLoc, minSuppressionDist)
                    && !pawn.Downed
                    && !pawn.InMentalState;
            }
        }

        public bool IsCrouchWalking => CanReactToSuppression && isCrouchWalking;

        #endregion

        #region Methods

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentSuppression, "currentSuppression", 0f);
            Scribe_Values.Look(ref suppressorLoc, "suppressorLoc");
            Scribe_Values.Look(ref locSuppressionAmount, "locSuppression", 0f);
            Scribe_Values.Look(ref isSuppressed, "isSuppressed", false);
            Scribe_Values.Look(ref ticksUntilDecay, "ticksUntilDecay", 0);
            Scribe_Values.Look(ref lastHelpRequestAt, "lastHelpRequestAt", -1);
        }

        public void AddSuppression(float amount, IntVec3 origin)
        {
            Pawn pawn = parent as Pawn;
            if (pawn == null)
            {
                Log.Error("CE trying to suppress non-pawn " + parent.ToString() + ", this should never happen");
                return;
            }

            // No suppression on berserking or fleeing pawns
            if (!CanReactToSuppression)
            {
                currentSuppression = 0f;
                isSuppressed = false;
                return;
            }

            // Add suppression to global suppression counter
            var suppressAmount = amount * pawn.GetStatValue(CE_StatDefOf.Suppressability);
            currentSuppression += suppressAmount;
            if (Controller.settings.DebugShowSuppressionBuildup)
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, suppressAmount.ToString());
            }
            ticksUntilDecay = TicksForDecayStart;
            if (currentSuppression > maxSuppression)
            {
                currentSuppression = maxSuppression;
            }

            // Add suppression to current suppressor location if appropriate
            if (suppressorLoc == origin)
            {
                locSuppressionAmount += amount;
            }
            else if (locSuppressionAmount < SuppressionThreshold)
            {
                suppressorLoc = origin;
                locSuppressionAmount = currentSuppression;
            }

            // Assign suppressed status and assign reaction job
            if (currentSuppression > SuppressionThreshold)
            {
                isSuppressed = true;
                Job reactJob = SuppressionUtility.GetRunForCoverJob(pawn);
                if (reactJob == null && IsHunkering)
                {
                    reactJob = JobMaker.MakeJob(CE_JobDefOf.HunkerDown, pawn);
                    LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_Hunkering, pawn, OpportunityType.Critical);
                }
                if (reactJob != null && reactJob.def != pawn.CurJob?.def)
                {
                    // Only reserve destination when we know for certain the pawn isn't already running for cover
                    pawn.Map.pawnDestinationReservationManager.Reserve(pawn, reactJob, reactJob.GetTarget(TargetIndex.A).Cell);
                    pawn.jobs.StartJob(reactJob, JobCondition.InterruptForced, null, pawn.jobs.curJob?.def == JobDefOf.ManTurret);
                    LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_SuppressionReaction, pawn, OpportunityType.Critical);
                }
                else
                {
                    // Crouch-walk
                    isCrouchWalking = true;
                }
                // Throw taunt
                if (Rand.Chance(0.01f))
                {
                    var tauntThrower = (TauntThrower)(pawn.Map.GetComponent(typeof(TauntThrower)));
                    tauntThrower?.TryThrowTaunt(CE_RulePackDefOf.SuppressedMote, pawn);
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            // Update suppressed tick counter and check for mental breaks
            if (!isSuppressed)
                ticksHunkered = 0;
            else if (IsHunkering)
                ticksHunkered++;

            if (ticksHunkered > MinTicksUntilMentalBreak && Rand.Chance(ChanceBreakPerTick))
            {
                var pawn = (Pawn)parent;
                if (pawn.mindState != null && !pawn.mindState.mentalStateHandler.InMentalState)
                {
                    var possibleBreaks = SuppressionUtility.GetPossibleBreaks(pawn);
                    if (possibleBreaks.Any())
                    {
                        pawn.mindState.mentalStateHandler.TryStartMentalState(possibleBreaks.RandomElement());
                    }
                }
            }

            //Apply decay once per second
            if (ticksUntilDecay > 0)
            {
                ticksUntilDecay--;
            }
            else if (currentSuppression > 0)
            {
                //Decay global suppression
                if (Controller.settings.DebugShowSuppressionBuildup && Gen.IsHashIntervalTick(parent, 30))
                {
                    MoteMaker.ThrowText(parent.DrawPos, parent.Map, "-" + (SuppressionDecayRate * 30), Color.red);
                }
                currentSuppression -= Mathf.Min(SuppressionDecayRate, currentSuppression);
                isSuppressed = currentSuppression > 0;

                // Clear crouch-walking
                if (!isSuppressed) isCrouchWalking = false;

                //Decay location suppression
                locSuppressionAmount -= Mathf.Min(SuppressionDecayRate, locSuppressionAmount);
            }

            // Throw mote at set interval
            if (parent.IsHashIntervalTick(TicksPerMote) && CanReactToSuppression)
            {
                if (this.IsHunkering)
                {
                    CE_Utility.MakeIconOverlay((Pawn)parent, CE_ThingDefOf.Mote_HunkerIcon);
                }
                else if (this.isSuppressed)
                {
                    CE_Utility.MakeIconOverlay((Pawn)parent, CE_ThingDefOf.Mote_SuppressIcon);
                }
            }
            if (!parent.Faction.IsPlayerSafe()
                && parent.IsHashIntervalTick(120)
                && isSuppressed
                && GenTicks.TicksGame - lastHelpRequestAt > HelpRequestCooldown)
            {
                lastHelpRequestAt = GenTicks.TicksGame;

                SuppressionUtility.TryRequestHelp(parent as Pawn);
            }
        }

        #endregion
    }
}