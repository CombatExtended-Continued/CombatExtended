using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CombatExtended.Utilities;
using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class SightGridManager_UniversalEnemies : SightGridManager<Pawn>
    {
        public SightGridManager_UniversalEnemies(SightTracker tracker, int bucketCount = 20, int updateInterval = 10)
            : base(tracker, bucketCount, updateInterval)
        {
        }

        protected override bool Valid(Pawn pawn)
        {
            return base.Valid(pawn) && !pawn.Dead;
        }

        protected override bool CanGroup(IThingSightRecord first, IThingSightRecord second)
        {
            return (first.thing.RaceProps.IsMechanoid && first.thing.RaceProps.IsMechanoid) || (first.thing.RaceProps.Insect && first.thing.RaceProps.Insect);
        }

        protected override int GetSightRange(IThingSightRecord record)
        {
            Pawn pawn = record.thing;
            if (pawn.RaceProps.Insect)            
                return (int) Mathf.Clamp(record.thing.CurrentEffectiveVerb?.EffectiveRange ?? -1, 5f, 15f);

            ThingWithComps weapon = pawn.equipment.Primary;
            if (weapon == null || !weapon.def.IsRangedWeapon)
                return 10;

            float range;
            range = Mathf.Min(weapon.def.verbs?.Max(v => v.range) ?? -1, 62f) * 0.5f;
            range = (range + record.carryRange) / (1 + record.carry);
            if (range < MinRange)
                return -1;
            
            float skill = 10;
            if (map.IsNightTime())
                skill = Mathf.Max(skill - (1 - pawn.GetStatValue(CE_StatDefOf.NightVisionEfficiency)) * 4, 0f);                 
            return Mathf.CeilToInt(range * Mathf.Clamp(skill / 7.5f, 0.85f, 1.75f));
        }

        protected override bool Skip(IThingSightRecord record)
        {            
            if (GenTicks.TicksGame - record.thing.needs?.rest?.lastRestTick <= 30)
                return true;
            return false;
        }

        protected override IEnumerable<Pawn> ThingsInRange(IntVec3 position, float range) => position.PawnsInRange(map, range);
    }
}

