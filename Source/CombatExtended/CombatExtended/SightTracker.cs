using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CombatExtended.Utilities;
using Mono.Security.Cryptography;
using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class SightTracker : MapComponent
    {
        private const int BUCKETCOUNT = 15;
        private const int BUCKETINTERVAL = 20;       
        private const float SIGHTINTERVAL = BUCKETCOUNT * BUCKETINTERVAL;

        private class PawnSightRecord
        {
            public Pawn pawn;
            public bool insect;
            public bool friendly;
            public float lastRange = -1;
            public int lastUpdated = -1;            
            public int index;

            public bool IsFriendly => friendly;
            public bool IsHostile => !friendly;
        }
        
        private int updateNum = 0;        
        private SightGrid gridFriendly;
        private SightGrid gridHostile;

        private float performanceRangeFactor = 0.5f;
        private int pawnsCastedNum;        

        private Stopwatch stopwatch = new Stopwatch();
        private int fIndex = 0;
        private List<Pawn>[] friendlies = new List<Pawn>[BUCKETCOUNT];
        private int hIndex = 0;
        private List<Pawn>[] hostiles = new List<Pawn>[BUCKETCOUNT];
        private int iIndex = 0;
        private List<Pawn>[] insects = new List<Pawn>[BUCKETCOUNT];

        private Dictionary<Pawn, PawnSightRecord> pawnToInfo = new Dictionary<Pawn, PawnSightRecord>(100);

        public SightGrid Friendly
        {
            get => gridFriendly;
        }
        public SightGrid Hostile
        {
            get => gridHostile;
        }
    
        public SightTracker(Map map) : base(map)
        {           
            gridFriendly = new SightGrid(map, null, SIGHTINTERVAL);
            gridHostile = new SightGrid(map, null, SIGHTINTERVAL);            
            for (int i = 0; i < BUCKETCOUNT; i++)
            {
                friendlies[i] = new List<Pawn>(5);
                hostiles[i] = new List<Pawn>(5);
                insects[i] = new List<Pawn>(5);
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            
            int ticks = GenTicks.TicksGame;
            if ((ticks + 5) % BUCKETINTERVAL == 0)
            {                    
                UpdatePawns(gridHostile, friendlies, ref fIndex);
                updateNum++;
            }
            if ((ticks + 10) % BUCKETINTERVAL == 0)
            {
                UpdatePawns(gridFriendly, hostiles, ref hIndex);
                updateNum++;
            }
            if ((ticks + 15) % BUCKETINTERVAL == 0)
            {
                UpdateInsects();
                updateNum++;
            }
            if (updateNum % 3 == 0)
            {
                performanceRangeFactor = 0.5f - Mathf.Min(pawnsCastedNum, 100) / 100f * 0.35f;                    
                pawnsCastedNum = updateNum = 0;                    
            }
            
            if (Controller.settings.DebugDrawLOSShadowGrid && GenTicks.TicksGame % 15 == 0)
            {
                TurretTracker turretTracker = map.GetComponent<TurretTracker>();
                IntVec3 center = UI.MouseMapPosition().ToIntVec3();
                if (center.InBounds(map))
                {
                    foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 64, true))
                    {
                        //if (cell.InBounds(map) && turretTracker.GetVisibleToTurret(cell))
                        //    map.debugDrawer.FlashCell(cell, 0.5f, $"{1f}", 15);
                        if (gridHostile[cell] >= 0)
                            map.debugDrawer.FlashCell(cell, (float)Mathf.Clamp(gridHostile[cell] / 10f, 0f, 0.95f), $"{gridHostile[cell]}", 15);
                    }
                }
            }
        }

        public bool TryGetGrid(Pawn pawn, out SightGrid grid)
        {
            grid = null;
            if (pawn.Faction == null)                            
                return false;            
            grid = !pawn.Faction.HostileTo(map.ParentFaction) ? gridFriendly : gridHostile;
            return true;
        }

        public void Register(Pawn pawn)
        {
            if (pawnToInfo.ContainsKey(pawn))
                DeRegister(pawn);
            if (pawn.RaceProps.Insect)
            {
                PawnSightRecord record = new PawnSightRecord()
                {
                    pawn = pawn,
                    insect = true,
                    index = pawn.thingIDNumber % BUCKETCOUNT
                };
                pawnToInfo[pawn] = record;
                insects[record.index].Add(pawn);
            }            
            else if(pawn.Faction != null && pawn.RaceProps.Humanlike)
            {                
                PawnSightRecord record = new PawnSightRecord()
                {
                    pawn = pawn,
                    friendly = !pawn.Faction.HostileTo(map.ParentFaction),
                    index = pawn.thingIDNumber % BUCKETCOUNT
                };
                pawnToInfo[pawn] = record;
                if (record.IsFriendly)
                    friendlies[record.index].Add(pawn);
                else
                    hostiles[record.index].Add(pawn);
            }
        }

        public void DeRegister(Pawn pawn)
        {
            if(!pawnToInfo.TryGetValue(pawn, out PawnSightRecord record))
                return;
            if (record.insect)
                insects[record.index].Remove(pawn);
            else
            {
                if (record.IsFriendly)
                    friendlies[record.index].Remove(pawn);
                else
                    hostiles[record.index].Remove(pawn);
            }
            pawnToInfo.Remove(pawn);
        }        

        private void UpdatePawns(SightGrid grid, List<Pawn>[] pool, ref int index)
        {                        
            List<Pawn> pawns = pool[index];
            if (pawns.Count != 0)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];                    
                    if (!pawn.Spawned || pawn.Downed || pawn.Suspended) 
                        continue;                    
                    if(TryCastPawnSight(grid, pawn))
                        pawnsCastedNum++; 
                }
            }
            index = (index + 1) % BUCKETCOUNT;
        }

        private void UpdateInsects()
        {
            List<Pawn> pawns = insects[iIndex];
            if (pawns.Count != 0)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (!pawn.Spawned || pawn.Downed || pawn.Suspended)
                        continue;
                    if (TryCastInsect(pawn))
                        pawnsCastedNum++;
                }
            }
            iIndex = (iIndex + 1) % BUCKETCOUNT;
        }       

        private bool TryCastPawnSight(SightGrid grid, Pawn pawn)
        {
            PawnSightRecord sightRecord = pawnToInfo[pawn];
            if (GenTicks.TicksGame - sightRecord.lastUpdated < SIGHTINTERVAL)
                return false;

            if (pawn.WorkTagIsDisabled(WorkTags.Violent) || pawn.equipment?.equipment == null || GenTicks.TicksGame - pawn.needs?.rest?.lastRestTick <= 30)            
                return false;
            
            ThingWithComps weapon = pawn.equipment.Primary;
            if (weapon == null || !weapon.def.IsRangedWeapon)            
                return false;
            
            float range = Mathf.Min(weapon.def.verbs?.Max(v => v.range) ?? -1, 62f) * performanceRangeFactor;
            if (range < 3.0f)            
                return false;
            
            SkillRecord record = pawn.skills?.GetSkill(SkillDefOf.Shooting) ?? null;
            if (record != null)            
                range *= Mathf.Clamp(pawn.skills.GetSkill(SkillDefOf.Shooting).Level / 7.5f, 1.0f, 1.5f);

            float t = grid[pawn.Position];
            if (true
                && (t > 3)
                && (pawn.Position.PawnsInRange(map, 7)?.Any(p => p != pawn
                                && pawnToInfo.TryGetValue(p, out PawnSightRecord s)                                
                                && sightRecord.friendly == s.friendly
                                && s.insect == false
                                && GenTicks.TicksGame - s.lastUpdated < SIGHTINTERVAL
                                && s.lastRange - range > -10) ?? false))
                return false;

            grid.Next();
            ShadowCastingUtility.CastVisibility(
                map,
                pawn.Position,
                (cell) =>
                {                    
                    grid[cell] += SIGHTINTERVAL;
                },
                Mathf.CeilToInt(range)
            );            
            sightRecord.lastUpdated = GenTicks.TicksGame;
            sightRecord.lastRange = range;
            return true;
        }

        private bool TryCastInsect(Pawn insect)
        {
            float range = 10 * performanceRangeFactor;
            if (true
                && (gridFriendly[insect.Position] > 2 || gridHostile[insect.Position] > 2)
                && (insect.Position.PawnsInRange(map, range)?.Any(p => p != insect
                        && pawnToInfo.TryGetValue(p, out PawnSightRecord s)
                        && s.insect                        
                        && GenTicks.TicksGame - s.lastUpdated < SIGHTINTERVAL) ?? false))
                return false;
            gridFriendly.Next();
            gridHostile.Next();
            ShadowCastingUtility.CastVisibility(
                map,
                insect.Position,
                (cell) =>
                {
                    gridFriendly[cell] += SIGHTINTERVAL;
                    gridHostile[cell] += SIGHTINTERVAL;
                },
                Mathf.CeilToInt(range)
            );
            PawnSightRecord sightRecord = pawnToInfo[insect];
            sightRecord.lastUpdated = GenTicks.TicksGame;
            sightRecord.lastRange = range;
            return true;
        }
    }
}

