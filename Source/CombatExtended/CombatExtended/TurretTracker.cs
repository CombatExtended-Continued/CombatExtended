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
        private const int SHADOW_DANGETICKS = 9000;
        
        private int index = 0;        
        private SightGrid grid;
        private CellIndices indices;
        private List<Building_TurretGunCE>[] buckets = new List<Building_TurretGunCE>[BUCKETCOUNT];
        
        public HashSet<Building_Turret> Turrets = new HashSet<Building_Turret>();
        public HashSet<Building_CiwsGunCE> Ciwss = new HashSet<Building_CiwsGunCE>();        

        public TurretTracker(Map map) : base(map)
        {
            indices = map.cellIndices;
            grid = new SightGrid(map, null);
            for (int i = 0; i < BUCKETCOUNT; i++)
                buckets[i] = new List<Building_TurretGunCE>(4);                                        
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();            
            if((GenTicks.TicksGame + 19) % 300 == 0)
            {                
                List<Building_TurretGunCE> bucket = buckets[index];
                for (int i = 0; i < bucket.Count; i++)
                {
                    Building_TurretGunCE turret = bucket[i];
                    if (turret.Active)
                        CastTurretShadow(turret);
                }
                index = (index + 1) % BUCKETCOUNT;
            }
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
                if(t is Building_TurretGunCE turretCE && !turretCE.IsMortar)
                {
                    buckets[turretCE.thingIDNumber % BUCKETCOUNT].Add(turretCE);
                }
            }
        }        

        public void Unregister(Building_Turret t)
        {
            if (Turrets.Contains(t))
            {
                Turrets.Remove(t);
                if (t is Building_CiwsGunCE ciws && Ciwss.Contains(ciws))
                {
                    Ciwss.Remove(ciws);
                }
                if (t is Building_TurretGunCE turretCE && !turretCE.IsMortar)
                {
                    for (int i = 0; i < 30; i++)
                        buckets[i].RemoveAll(t => t == turretCE);
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

        public bool GetVisibleToTurret(IntVec3 cell) => GetVisibleToTurret(indices.CellToIndex(cell));

        public bool GetVisibleToTurret(int index)
        {
            if (index >= 0 && index < indices.NumGridCells)
                return grid[index] - GenTicks.TicksGame > 0;
            return false;
        }

        private void CastTurretShadow(Building_TurretGunCE turret)
        {
            grid.Next();
            ShadowCastingUtility.CastVisibility(
                map,
                turret.Position,
                (cell) =>
                {
                    if (cell.InBounds(map))
                        grid[indices.CellToIndex(cell)] += SHADOW_DANGETICKS;
                },
                (int) Mathf.Min(turret.AttackVerb.EffectiveRange + 2.5f, 60)
            );            
        }        
    }
}
