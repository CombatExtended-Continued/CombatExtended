using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class CompSuppressable : ThingComp
    {
        #region Variables

        public CompProperties_Suppressable Props
        {
            get
            {
                return (CompProperties_Suppressable)props;
            }
        }

        // --------------- Global constants ---------------

        private const float minSuppressionDist = 5f;        //Minimum distance to be suppressed from, so melee won't be suppressed if it closes within this distance
        private const float maxSuppression = 100f;          //Cap to prevent suppression from building indefinitely
        private const float suppressionDecayRate = 7.5f;    //How much suppression decays per second
        private int ticksPerMote = 150;               //How many ticks between throwing a mote

        // --------------- Location calculations ---------------

        /*
         * We track the initial location from which a pawn was suppressed and the total amount of suppression coming from that location separately.
         * That way if suppression stops coming from location A but keeps coming from location B the location will get updated without bouncing 
         * pawns or having to track fire coming from multiple locations
         */
        private IntVec3 suppressorLocInt;
        public IntVec3 suppressorLoc
        {
            get
            {
                return suppressorLocInt;
            }
        }
        private float locSuppressionAmount = 0f;

        // --------------- Suppression calculations ---------------
        private float currentSuppressionInt = 0f;
        public float currentSuppression
        {
            get
            {
                return currentSuppressionInt;
            }
        }
        public float parentArmor
        {
            get
            {
                float armorValue = 0f;
                Pawn pawn = parent as Pawn;
                if (pawn != null)
                {
                    //Get most protective piece of armor
                    if (pawn.apparel.WornApparel != null && pawn.apparel.WornApparel.Count > 0)
                    {
                        List<Apparel> wornApparel = new List<Apparel>(pawn.apparel.WornApparel);
                        foreach (Apparel apparel in wornApparel)
                        {
                            float apparelArmor = apparel.GetStatValue(StatDefOf.ArmorRating_Sharp, true);
                            if (apparelArmor > armorValue)
                            {
                                armorValue = apparelArmor;
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("Tried to get parent armor of non-pawn");
                }
                return armorValue;
            }
        }
        private float suppressionThreshold
        {
            get
            {
                float threshold = 0f;
                Pawn pawn = parent as Pawn;
                if (pawn != null)
                {
                    //Get morale
                    float hardBreakThreshold = pawn.GetStatValue(StatDefOf.MentalBreakThreshold) + 0.15f;
                    float currentMood = pawn.needs != null && pawn.needs.mood != null ? pawn.needs.mood.CurLevel : 0.5f;
                    threshold = Mathf.Max(0, currentMood - hardBreakThreshold);
                }
                else
                {
                    Log.Error("Tried to get suppression threshold of non-pawn");
                }
                return threshold * maxSuppression * 0.5f;
            }
        }

        public bool isSuppressed = false;
        public bool isHunkering
        {
            get
            {
                if (currentSuppressionInt > suppressionThreshold * 2)
                {
                    if (isSuppressed)
                    {
                        return true;
                    }
                    // Removing suppression log
                    else
                    {
                        Log.Warning("Hunkering without suppression, this should never happen");
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
                return !pawn.Position.InHorDistOf(suppressorLoc, minSuppressionDist)
                    && !pawn.Downed
                    && !pawn.InMentalState;
            }
        }

        #endregion

        #region Methods
        // --------------- Public functions ---------------
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.LookValue<float>(ref currentSuppressionInt, "currentSuppression", 0f);
            Scribe_Values.LookValue<IntVec3>(ref suppressorLocInt, "suppressorLoc");
            Scribe_Values.LookValue<float>(ref locSuppressionAmount, "locSuppression", 0f);
        }

        public void AddSuppression(float amount, IntVec3 origin)
        {
            Pawn pawn = parent as Pawn;
            if (pawn == null)
            {
                Log.Error("Trying to suppress non-pawn " + parent.ToString() + ", this should never happen");
                return;
            }

            // No suppression on berserking or fleeing pawns
            if (pawn.MentalStateDef != null && (pawn.MentalState.def == MentalStateDefOf.Berserk || pawn.MentalState.def == MentalStateDefOf.PanicFlee))
            {
                currentSuppressionInt = 0f;
                isSuppressed = false;
                return;
            }

            // Add suppression to global suppression counter
            currentSuppressionInt += amount;
            if (currentSuppressionInt > maxSuppression)
            {
                currentSuppressionInt = maxSuppression;
            }

            // Add suppression to current suppressor location if appropriate
            if (suppressorLocInt == origin)
            {
                locSuppressionAmount += amount;
            }
            else if (locSuppressionAmount < suppressionThreshold)
            {
                suppressorLocInt = origin;
                locSuppressionAmount = currentSuppressionInt;
            }

            // Assign suppressed status and interrupt activity if necessary
            if (!isSuppressed && currentSuppressionInt > suppressionThreshold)
            {
                isSuppressed = true;
                if (pawn.CurJob != null && (pawn.CurJob.def != CE_JobDefOf.HunkerDown || pawn.CurJob.def != CE_JobDefOf.RunForCover))
                {
                    pawn.jobs.StopAll();
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            //Apply decay once per second
            if (parent.IsHashIntervalTick(60))
            {
                //Decay global suppression
                if (currentSuppressionInt > suppressionDecayRate)
                {
                    currentSuppressionInt -= suppressionDecayRate;

                    //Check if pawn is still suppressed
                    if (isSuppressed && currentSuppressionInt <= suppressionThreshold)
                    {
                        isSuppressed = false;
                    }
                }
                else if (currentSuppressionInt > 0)
                {
                    currentSuppressionInt = 0;
                    isSuppressed = false;
                }

                //Decay location suppression
                if (locSuppressionAmount > suppressionDecayRate)
                {
                    locSuppressionAmount -= suppressionDecayRate;
                }
                else if (locSuppressionAmount > 0)
                {
                    locSuppressionAmount = 0;
                }
            }

            //Throw mote at set interval
            if (Gen.IsHashIntervalTick(this.parent, ticksPerMote) && CanReactToSuppression)
            {
                if (isHunkering)
                {
                    MoteMaker.ThrowMetaIcon(parent.Position, parent.Map, CE_ThingDefOf.Mote_HunkerIcon);
                }
	            else if (this.isSuppressed)
                {
                    //MoteMaker.ThrowText(this.parent.Position.ToVector3Shifted(), parent.Map, "CE_SuppressedMote".Translate());
                    MoteMaker.ThrowMetaIcon(parent.Position, parent.Map, CE_ThingDefOf.Mote_SuppressIcon);
                }
			}

            /*if (Gen.IsHashIntervalTick(parent, ticksPerMote + Rand.Range(30, 300))
                && parent.def.race.Humanlike && !robotBodyList.Contains(parent.def.race.body.defName))
            {
                if (isHunkering || isSuppressed)
                {
                    AGAIN: string rndswearsuppressed = RulePackDef.Named("SuppressedMote").Rules.RandomElement().Generate();

                    if (rndswearsuppressed == "[suppressed]" || rndswearsuppressed == "" || rndswearsuppressed == " ")
                    {
                        goto AGAIN;
                    }
                    MoteMaker.ThrowText(this.parent.Position.ToVector3Shifted(), Find.VisibleMap, rndswearsuppressed);
                }
                //standard    MoteMaker.ThrowText(parent.Position.ToVector3Shifted(), "CE_SuppressedMote".Translate());
            }*/
        }
        #endregion
    }
}