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
        //private static readonly IntRange InfectionDelayHours = new IntRange(6, 12); // Infections will appear somewhere within this timeframe

        private bool alreadyCausedInfection = false;
        private int ticksUntilInfect = -1;
        private float infectionChanceFactorFromTendRoom = 1;

        public HediffCompProperties_InfecterCE Props { get { return (HediffCompProperties_InfecterCE)props; } }

        private void CheckMakeInfection()
        {
            if (base.Pawn.health.immunity.DiseaseContractChanceFactor(HediffDefOf.WoundInfection, this.parent.Part) <= 0.001f)
            {
                this.ticksUntilInfect = -3;
                return;
            }
            float num = 1f;
            HediffComp_TendDuration hediffComp_TendDuration = this.parent.TryGetComp<HediffComp_TendDuration>();
            if (hediffComp_TendDuration != null && hediffComp_TendDuration.IsTended)
            {
                num *= this.infectionChanceFactorFromTendRoom;
                num *= InfectionChanceFactorFromTendQualityCurve.Evaluate(hediffComp_TendDuration.tendQuality);
            }
            num *= InfectionChanceFactorFromSeverityCurve.Evaluate(this.parent.Severity);
            if (base.Pawn.Faction == Faction.OfPlayer)
            {
                num *= Find.Storyteller.difficulty.playerPawnInfectionChanceFactor;
            }
            if (Rand.Value < num)
            {
                this.ticksUntilInfect = -4;
                base.Pawn.health.AddHediff(HediffDefOf.WoundInfection, this.parent.Part, null, null);
            }
            else
            {
                this.ticksUntilInfect = -3;
            }
        }

        /*
        private void CheckMakeInfection()
        {
            ticksUntilInfect = -1;
            infectionChanceFactorFromTendRoom *= Pawn.health.immunity.DiseaseContractChanceFactor(HediffDefOf.WoundInfection, parent.Part); // Apply pawn immunity
            if (Pawn.def.race.Animal) infectionChanceFactorFromTendRoom *= 0.5f;

            // Adjust for difficulty
            if (Pawn.Faction == Faction.OfPlayer) infectionChanceFactorFromTendRoom *= Find.Storyteller.difficulty.playerPawnInfectionChanceFactor;

            // Find out how long the wound was untreated
            HediffComp_TendDuration compTended = parent.TryGetComp<HediffComp_TendDuration>();
            int ticksUntended = parent.ageTicks;
            if (compTended != null && compTended.IsTended)
            {
                int ticksTended = Find.TickManager.TicksGame - compTended.tendTick;
                ticksUntended -= ticksTended;

                infectionChanceFactorFromTendRoom /= Mathf.Pow(compTended.tendQuality + 0.75f, 2);  // Adjust infection chance based on tend quality
            }
            float infectChance = Props.infectionChancePerHourUntended * (ticksUntended / GenDate.TicksPerHour); // Calculate base chance from time untreated
            if (parent.Part.depth == BodyPartDepth.Inside) infectChance *= infectionInnerModifier;  // Increase chance of infection for inner organs
            if (Rand.Value < infectChance * infectionChanceFactorFromTendRoom)
            {
                alreadyCausedInfection = true;
                Pawn.health.AddHediff(HediffDefOf.WoundInfection, parent.Part);
            }
        }*/


        //1:1 COPY
        private static readonly SimpleCurve InfectionChanceFactorFromTendQualityCurve = new SimpleCurve
        {
            {
                new CurvePoint(0f, 0.7f),
                true
            },
            {
                new CurvePoint(1f, 0.4f),
                true
            }
        };

        //1:1 COPY
        private static readonly SimpleCurve InfectionChanceFactorFromSeverityCurve = new SimpleCurve
        {
            {
                new CurvePoint(1f, 0.1f),
                true
            },
            {
                new CurvePoint(12f, 1f),
                true
            }
        };

        //1:1 COPY
        public override string CompDebugString()
        {
            if (this.alreadyCausedInfection) return "already caused infection"; //CUSTOM
            if (this.ticksUntilInfect > 0)
            {
                return string.Concat(new object[]
                {
                    "infection may appear in: ",
                    this.ticksUntilInfect,
                    " ticks\ninfectChnceFactorFromTendRoom: ",
                    this.infectionChanceFactorFromTendRoom.ToStringPercent()
                });
            }
            if (this.ticksUntilInfect == -4)
            {
                return "already created infection";
            }
            if (this.ticksUntilInfect == -3)
            {
                return "failed to make infection";
            }
            if (this.ticksUntilInfect == -2)
            {
                return "will not make infection";
            }
            if (this.ticksUntilInfect == -1)
            {
                return "uninitialized data!";
            }
            return "unexpected ticksUntilInfect = " + this.ticksUntilInfect;
        }

    //1:1 COPY
    public override void CompExposeData()
        {
            Scribe_Values.Look(ref alreadyCausedInfection, "alreadyCausedInfection", false); //CUSTOM
            Scribe_Values.Look<float>(ref this.infectionChanceFactorFromTendRoom, "infectionChanceFactor", 0f, false);
            Scribe_Values.Look<int>(ref this.ticksUntilInfect, "ticksUntilInfect", -2, false);
        }

        //1:1 COPY
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            if (this.parent.IsPermanent())
            {
                this.ticksUntilInfect = -2;
                return;
            }
            if (this.parent.Part.def.IsSolid(this.parent.Part, base.Pawn.health.hediffSet.hediffs))
            {
                this.ticksUntilInfect = -2;
                return;
            }
            if (base.Pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(this.parent.Part))
            {
                this.ticksUntilInfect = -2;
                return;
            }
            float chanceForInfection = this.Props.infectionChance;
            if (base.Pawn.RaceProps.Animal)
            {
                chanceForInfection *= 0.1f;
            }
            if (Rand.Value <= chanceForInfection)
            {
                this.ticksUntilInfect = HealthTuning.InfectionDelayRange.RandomInRange;
            }
            else
            {
                this.ticksUntilInfect = -2;
            }
        }

        //1:1 COPY
        public override void CompPostTick(ref float severityAdjustment)
        {
            if (this.ticksUntilInfect > 0)
            {
                this.ticksUntilInfect--;
                if (this.ticksUntilInfect == 0)
                {
                    this.CheckMakeInfection();
                }
            }
        }

        //1:1 COPY
        public override void CompTended(float quality, int batchPosition = 0)
        {
            if (base.Pawn.Spawned)
            {
                Room room = base.Pawn.GetRoom(RegionType.Set_Passable);
                if (room != null)
                {
                    this.infectionChanceFactorFromTendRoom = room.GetStat(RoomStatDefOf.InfectionChanceFactor);
                }
            }
        }
    }
}
