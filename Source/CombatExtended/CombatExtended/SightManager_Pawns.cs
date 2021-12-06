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
    public class SightManager_Pawns : SightTracker<Pawn>
    {
        public SightManager_Pawns(SightTracker tracker, int bucketCount = 20, int updateInterval = 10)
            : base(tracker, bucketCount, updateInterval)
        {
        }

        protected override int GetSightRange(IThingSightRecord record)
        {            
            Pawn pawn = record.thing;
            Verb verb = pawn.equipment?.PrimaryEq?.PrimaryVerb ?? null;
            if(verb == null || !verb.Available())
                verb = pawn.verbTracker?.AllVerbs.Where(v => v.Available()).MaxBy(v => v.IsMeleeAttack ? 0 : v.EffectiveRange) ?? null;
            if (verb == null)
                return -1;

            float range;
            if (pawn.RaceProps.Insect || pawn.RaceProps.IsMechanoid || pawn.RaceProps.Animal)
            {
                if (verb.IsMeleeAttack)
                    return 5;
                if ((range = verb.EffectiveRange) > 2.5f)
                    return (int)Mathf.Max(range * 0.75f, 5f);
                return -1;
            }
            if (verb.IsMeleeAttack)
            {
                SkillRecord melee = pawn.skills?.GetSkill(SkillDefOf.Melee) ?? null;
                if (melee != null)
                {
                    float skill = melee.Level;
                    return (int)Mathf.Clamp(skill, 4, 13);
                }
                return 5;
            }
            if ((range = verb.EffectiveRange * 0.75f) > 2.5f)
            {                
                SkillRecord shooting = pawn.skills?.GetSkill(SkillDefOf.Shooting) ?? null;
                float skill = 5;
                if (shooting != null)
                    skill = shooting.Level;
                if (map.IsNightTime())
                    skill = Mathf.Max(skill - (1 - pawn.GetStatValue(CE_StatDefOf.NightVisionEfficiency)) * 4, 0f);                    
                range = Mathf.Max(range * Mathf.Clamp(skill / 7.5f, 0.778f, 1.425f), 5);
                return  Mathf.CeilToInt(range);
            }
            return -1;
        }

        protected override bool Valid(Pawn pawn) => base.Valid(pawn) && !pawn.Dead;
        protected override bool Skip(IThingSightRecord record)
        {
            Pawn pawn = record.thing;            
            if (GenTicks.TicksGame - pawn.needs?.rest?.lastRestTick <= 30)
                return true;
            if (pawn.RaceProps.Humanlike && pawn.equipment?.equipment == null)
                return true;
            if (pawn.Downed)
                return true;
            return false;
        }
    }
}

