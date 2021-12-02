using System;
using System.Collections.Generic;
using System.Linq;
using CombatExtended.Utilities;
using Mono.Security.X509.Extensions;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public class CompUrgentDangerResponse : ICompTactics
    {
        private const int HEADSCANSTART = 4;
        private const int HEADSCANEND = 6;
        private const int NODECHECKS = 6;
        private const int NODESTEP = 5;
        private const int NODESTEPMIN = 3;
        private const float FOCUSWEIGHT = 0.4f;
        private const float VISIONWEIGHT = 0.4f;
        private const float HEARINGWEIGHT = 0.2f;
        private const float AWARENESSMIN = 0.5f;

        private bool _disabled = false;
        
        private int _cooldownTick = -1;        
        private IntVec3 _lastCell;        

        public override int Priority => 1200;
        private float Vision
        {
            get
            {
                if (SelPawn.health?.capacities == null)
                {
                    return 0f;
                }
                if (!SelPawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight))
                {
                    return 0f;
                }
                return SelPawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight);
            }
        }
        private float Focus
        {
            get
            {
                if (SelPawn.health?.capacities == null)
                {
                    return 0f;
                }
                if (!SelPawn.health.capacities.CapableOf(PawnCapacityDefOf.Consciousness))
                {
                    return 0f;
                }
                return SelPawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
            }
        }
        private float Hearing
        {
            get
            {
                if (SelPawn.health?.capacities == null)
                {
                    return 0f;
                }
                if (!SelPawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing))
                {
                    return 0f;
                }
                return SelPawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing);
            }
        }

        public CompUrgentDangerResponse()
        {
        }

        public override void Initialize(Pawn pawn)
        {
            base.Initialize(pawn);
            _disabled = !pawn.RaceProps.Humanlike || pawn.RaceProps.intelligence != Intelligence.Humanlike || pawn.Faction == null;                
        }

        public override void TickShort()
        {
            base.TickShort();
            if (_disabled || _cooldownTick > GenTicks.TicksGame)
            {
                return;
            }
            Pawn pawn = SelPawn;            
            if(_lastCell == pawn.Position)
            {
                _lastCell = IntVec3.Invalid;
                _cooldownTick = GenTicks.TicksGame + 30;                
                return;
            }
            PawnPath path = pawn.pather?.curPath ?? null;
            if(path == null || !pawn.pather.moving)
            {
                _lastCell = pawn.Position;
                _cooldownTick = GenTicks.TicksGame + 60;                
                return;
            }            
            if (pawn.Faction == null || pawn.Drafted)
            {
                _cooldownTick = GenTicks.TicksGame + GenTicks.TickRareInterval;
                return;
            }            
            if (path.NodesLeftCount <= NODESTEP + 1 || path.NodesLeftCount <= HEADSCANEND)
            {
                return;
            }            
            float vision = Vision, hearing = Hearing, focus = Focus;
            float awarness = vision * VISIONWEIGHT + focus * FOCUSWEIGHT + hearing * HEARINGWEIGHT;
            if (awarness < AWARENESSMIN)
            {
                _cooldownTick = GenTicks.TicksGame + GenTicks.TickRareInterval;
                return;
            }
            //Pawn other;
            //Map map = pawn.Map;            
            //Faction faction = pawn.Faction;            
            
            //for (int i = HEADSCANSTART; i < HEADSCANEND; i++)
            //{
            //    IntVec3 cell = path.nodes[path.curNodeIndex - i];                               
            //    if((other = cell.GetFirstPawn(map)) != null && !other.Downed && pawn.HostileTo(other))
            //    {
            //        TryHostilityResponse(other);
            //        break;
            //    }                
            //}            
        }

        //private void TryHostilityResponse(Pawn enemy)
        //{
        //    Map.debugDrawer.FlashCell(SelPawn.Position, 1.0f, duration: 100);


        //    if (SelPawn.WorkTagIsDisabled(WorkTags.Violent))
        //    {
        //        return;
        //    }                                          
        //    ThingWithComps weapon = CurrentWeapon;
        //    SightTracker tracker = Map.GetComponent<SightTracker>();
        //    if (weapon?.def.IsMeleeWeapon ?? false)
        //    {
        //        return;
        //    }
        //    tracker.TryGetGrid(enemy, out SightGrid gridSelf);
        //    tracker.TryGetGrid(SelPawn,  out SightGrid gridEnemy);
        //    if(gridEnemy == null)
        //    {                
        //        return;
        //    }
        //    float friendlies = gridSelf[enemy.Position];
        //    float enemies =   gridEnemy[enemy.Position];
        //    if (friendlies + 2 > enemies && friendlies != 0)
        //    {
        //        return;
        //    }
        //    if (weapon.def.weaponTags != null && weapon.def.weaponTags.Any(s => s == "CE_AI_BROOM"))
        //    {
        //        OrderAttackJob();
        //    }            
        //}
       
        //private void OrderAttackJob()
        //{
        //    Job job = JobMaker.MakeJob(JobDefOf.Wait_Combat);
        //    if (job != null)
        //    {
        //        SelPawn.jobs.StartJob(job, JobCondition.InterruptForced);
        //    }
        //}      
    }
}