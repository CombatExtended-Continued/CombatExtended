using System;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public class CompUrgentDangerResponse : ICompTactics
    {
        private const int COOLDOWN_DANGER_JOB = 1200;

        private int danger = 0;        

        public override int Priority => 1200;

        public CompUrgentDangerResponse()
        {            
        }

        public override Job TryGiveTacticalJob()
        {           
            float reactUrgency = 0;
            SightGrid sightGrid = MapSightGrid;
            if (sightGrid != null)
            {
                reactUrgency += sightGrid[SelPawn.Position] * 2f;
            }
            TurretTracker tracker = MapTurretTracker;
            if (tracker != null && tracker.GetVisibleToTurret(SelPawn.Position))
            {
                reactUrgency += 5f;
            }
            if(tracker == null && sightGrid == null)
            {
                return null;
            }                     
            return null;            
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref danger, "danger");            
        }      
    }
}

