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
        private const float InfectionInnerModifier = 3f;                            // Injuries to inner body parts will be this much more likely to get infected
        private const float TreatmentQualityExponential = 3f;                       // Treatment factor = (TendQuality + base) ^ exponential
        private const float TreatmentQualityBase = 0.75f;
        private const float DamageThreshold = 10f;                                  // Wounds below this severity will decrease chance of infection, above increase
        private static readonly IntRange InfectionDelayHours = new IntRange(6, 12); // Infections will appear somewhere within this timeframe

        private bool _alreadyCausedInfection = false;
        private int _ticksUntilInfect = -1;
        private float _infectionModifier = 1;
        private int _ticksTended = 0;
        private bool _tendedOutside;

        public HediffCompProperties_InfecterCE Props => (HediffCompProperties_InfecterCE)props;
        private bool IsInternal => parent.Part.depth == BodyPartDepth.Inside;

        private void CheckMakeInfection()
        {
            _ticksUntilInfect = -1;
            _infectionModifier *= Pawn.health.immunity.DiseaseContractChanceFactor(HediffDefOf.WoundInfection, parent.Part); // Apply pawn immunity
            if (Pawn.def.race.Animal) _infectionModifier *= 0.5f;

            // Adjust for difficulty
            if (Pawn.Faction == Faction.OfPlayer) _infectionModifier *= Find.Storyteller.difficulty.playerPawnInfectionChanceFactor;

            // Find out how long the wound was untreated
            var compTended = parent.TryGetComp<HediffComp_TendDuration>();
            var ticksUntended = parent.ageTicks;
            if (compTended != null && compTended.IsTended)
            {
                ticksUntended -= _ticksTended;
                _infectionModifier /= Mathf.Pow(compTended.tendQuality + TreatmentQualityBase, TreatmentQualityExponential);  // Adjust infection chance based on tend quality
            }
            var infectChance = Props.infectionChancePerHourUntended * ((float)ticksUntended / GenDate.TicksPerHour); // Calculate base chance from time untreated

            if (IsInternal) infectChance *= InfectionInnerModifier;  // Increase chance of infection for inner organs

            infectChance *= parent.Severity / DamageThreshold;

            // Infection check
            if (Rand.Value < infectChance * _infectionModifier)
            {
                _alreadyCausedInfection = true;
                Pawn.health.AddHediff(HediffDefOf.WoundInfection, parent.Part);
            }
        }

        public override string CompDebugString()
        {
            if (this._alreadyCausedInfection) return "already caused infection";
            if (this._ticksUntilInfect <= 0) return "no infection will appear";
            return "infection may appear after: " + _ticksUntilInfect + " ticks (infection chance factor: " + _infectionModifier.ToString() + ")";
        }

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref _alreadyCausedInfection, "alreadyCausedInfection", false);
            Scribe_Values.Look(ref _ticksUntilInfect, "ticksUntilInfect", -1);
            Scribe_Values.Look(ref _infectionModifier, "infectionModifier", 1);
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            // Determine time until infection check is made
            if (!_alreadyCausedInfection 
                && !parent.Part.def.IsSolid(parent.Part, Pawn.health.hediffSet.hediffs) 
                && !Pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(parent.Part) 
                && !parent.IsPermanent())
            {
                _ticksUntilInfect = InfectionDelayHours.RandomInRange * GenDate.TicksPerHour;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (!(_tendedOutside && IsInternal) && parent.TryGetComp<HediffComp_TendDuration>().IsTended)
            {
                _ticksTended++;
            }

            if (!_alreadyCausedInfection && _ticksUntilInfect > 0)
            {
                _ticksUntilInfect--;
                if (_ticksUntilInfect == 0) CheckMakeInfection();
            }
        }

        public override void CompTended_NewTemp(float quality, float maxQuality, int batchPosition = 0)
        {
            if (Pawn.Spawned)
            {
                var room = Pawn.GetRoom();
                _tendedOutside = room == null;
                _infectionModifier *= room == null ? RoomStatDefOf.InfectionChanceFactor.roomlessScore : RoomStatDefOf.InfectionChanceFactor.Worker.GetScore(room);
            }
        }
    }
}
