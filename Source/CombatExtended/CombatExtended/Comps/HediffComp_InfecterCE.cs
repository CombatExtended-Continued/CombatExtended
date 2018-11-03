using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class HediffComp_InfecterCE : HediffComp
    {
        private const float infectionInnerModifier = 3f;                            // Injuries to inner body parts will be this much more likely to get infected
        private static readonly IntRange InfectionDelayHours = new IntRange(6, 12); // Infections will appear somewhere within this timeframe

        private bool alreadyCausedInfection = false;
        private int ticksUntilInfect = -1;
        private float infectionModifier = 1;
        private int ticksTended = 0;

        public HediffCompProperties_InfecterCE Props { get { return (HediffCompProperties_InfecterCE)props; } }

        private void CheckMakeInfection()
        {
            ticksUntilInfect = -1;
            infectionModifier *= Pawn.health.immunity.DiseaseContractChanceFactor(HediffDefOf.WoundInfection, parent.Part); // Apply pawn immunity
            if (Pawn.def.race.Animal) infectionModifier *= 0.5f;

            // Adjust for difficulty
            if (Pawn.Faction == Faction.OfPlayer) infectionModifier *= Find.Storyteller.difficulty.playerPawnInfectionChanceFactor;

            // Find out how long the wound was untreated
            HediffComp_TendDuration compTended = parent.TryGetComp<HediffComp_TendDuration>();
            int ticksUntended = parent.ageTicks;
            if (compTended != null && compTended.IsTended)
            {
                ticksUntended -= ticksTended;

                infectionModifier /= Mathf.Pow(compTended.tendQuality + 0.75f, 2);  // Adjust infection chance based on tend quality
            }
            float infectChance = Props.infectionChancePerHourUntended * (ticksUntended / GenDate.TicksPerHour); // Calculate base chance from time untreated
            if (parent.Part.depth == BodyPartDepth.Inside) infectChance *= infectionInnerModifier;  // Increase chance of infection for inner organs
            if (Rand.Value < infectChance * infectionModifier)
            {
                alreadyCausedInfection = true;
                Pawn.health.AddHediff(HediffDefOf.WoundInfection, parent.Part);
            }
        }

        public override string CompDebugString()
        {
            if (this.alreadyCausedInfection) return "already caused infection";
            if (this.ticksUntilInfect <= 0) return "no infection will appear";
            return "infection may appear after: " + ticksUntilInfect + " ticks (infection chance factor: " + infectionModifier.ToString() + ")";
        }

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref alreadyCausedInfection, "alreadyCausedInfection", false);
            Scribe_Values.Look(ref ticksUntilInfect, "ticksUntilInfect", -1);
            Scribe_Values.Look(ref infectionModifier, "infectionModifier", 1);
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            // Determine time until infection check is made
            if (!alreadyCausedInfection 
                && !parent.Part.def.IsSolid(parent.Part, Pawn.health.hediffSet.hediffs) 
                && !Pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(parent.Part) 
                && !parent.IsPermanent())
            {
                ticksUntilInfect = InfectionDelayHours.RandomInRange * GenDate.TicksPerHour;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (parent.TryGetComp<HediffComp_TendDuration>().IsTended)
            {
                ticksTended++;
            }

            if (!alreadyCausedInfection && ticksUntilInfect > 0)
            {
                ticksUntilInfect--;
                if (ticksUntilInfect == 0) CheckMakeInfection();

            }
        }

        public override void CompTended(float quality, int batchPosition = 0)
        {
            if (Pawn.Spawned)
            {
                Room room = Pawn.GetRoom();
                infectionModifier *= room == null ? 1.5f : room.GetStat(RoomStatDefOf.InfectionChanceFactor);
            }
        }
    }
}
