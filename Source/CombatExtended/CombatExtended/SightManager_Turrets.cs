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
    public class SightManager_Turrets : SightTracker<Building_TurretGunCE>
    {
        public SightManager_Turrets(SightTracker tracker, int bucketCount = 30, int updateInterval = 200)
            : base(tracker, bucketCount, updateInterval)
        {            
        }             

        protected override int GetSightRange(IThingSightRecord record)
        {
            float range = record.thing.CurrentEffectiveVerb?.EffectiveRange ?? -1;
            if (range == -1)
                return -1;

            if (record.thing.IsMannable)
            {
                Pawn pawn = record.thing.mannableComp.ManningPawn;
                if(pawn != null)
                {
                    SkillRecord shooting = pawn.skills?.GetSkill(SkillDefOf.Shooting) ?? null;
                    float skill = 5;
                    if (shooting != null)
                        skill = shooting.Level;
                    if (map.IsNightTime())
                        skill = Mathf.Max(skill - (1 - pawn.GetStatValue(CE_StatDefOf.NightVisionEfficiency)) * 4, 0f);
                    range = Mathf.Max(range * Mathf.Clamp(skill / 7.5f, 0.778f, 1.425f), 5);                    
                }                
            }
            return Mathf.CeilToInt(range);            
        }

        protected override bool Skip(IThingSightRecord record)
        {
            return !record.thing.Active || (record.thing.IsMannable && !(record.thing.MannableComp?.MannedNow ?? false));
        }
    }
}

