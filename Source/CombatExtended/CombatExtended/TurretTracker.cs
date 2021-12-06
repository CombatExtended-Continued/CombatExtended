using System;
using System.Collections.Generic;
using System.Text;
using Verse;
using RimWorld;
using Verse.AI;
using System.Diagnostics;
using UnityEngine;

namespace CombatExtended
{
    public class TurretTracker : MapComponent
    {
        private const int BUCKETCOUNT = 30;
        private const int SHADOW_DANGETICKS = 1800;
                   
        private CellIndices indices;        
        
        public HashSet<Building_Turret> Turrets = new HashSet<Building_Turret>();
        public HashSet<Building_CiwsGunCE> Ciwss = new HashSet<Building_CiwsGunCE>();        

        public TurretTracker(Map map) : base(map)
        {
            indices = map.cellIndices;                                                  
        }      

        public void Register(Building_Turret t)
        {            
            if (!Turrets.Contains(t))
            {                
                Turrets.Add(t);
                if(t is Building_CiwsGunCE ciws && !Ciwss.Contains(ciws))
                {
                    Ciwss.Add(ciws);
                }                
            }
        }        

        public void DeRegister(Building_Turret t)
        {
            if (Turrets.Contains(t))
            {
                Turrets.Remove(t);
                if (t is Building_CiwsGunCE ciws && Ciwss.Contains(ciws))
                {
                    Ciwss.Remove(ciws);
                }                
            }
        }      

        // Returns the closest turret to `position` on the which matches the criteria set in `validator`
        public Thing ClosestTurret(IntVec3 position, PathEndMode pathEndMode, TraverseParms parms, float maxDist,
            Predicate<Thing> validator = null)
        {
            return GenClosest.ClosestThingReachable(
                position, map, ThingRequest.ForUndefined(), pathEndMode,
                parms, maxDist, validator, Turrets);
        }                   
    }
}
