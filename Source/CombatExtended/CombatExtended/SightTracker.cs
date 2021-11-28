using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class SightTracker : MapComponent
    {
        private const int BUCKETCOUNT = 30;

        private struct PawnSightRecord
        {
            public Pawn pawn;            
            public bool friendly;
            public int index;

            public bool IsFriendly => friendly;
            public bool IsHostile => !friendly;
        }

        private int ticks = 0;
        private SightGrid gridFriendly;
        private SightGrid gridHostile;        

        private Stopwatch stopwatch = new Stopwatch();
        private int fIndex = 0;
        private List<Pawn>[] friendlies = new List<Pawn>[BUCKETCOUNT];
        private int hIndex = 0;
        private List<Pawn>[] hostiles = new List<Pawn>[BUCKETCOUNT];

        private Dictionary<Pawn, PawnSightRecord> pawnToInfo = new Dictionary<Pawn, PawnSightRecord>(100);

        public SightTracker(Map map) : base(map)
        {           
            gridFriendly = new SightGrid(map, null);
            gridHostile = new SightGrid(map, null);            
            for (int i = 0; i < BUCKETCOUNT; i++)
            {
                friendlies[i] = new List<Pawn>(5);
                hostiles[i] = new List<Pawn>(5);
            }
        }

        public override void MapComponentTick()
        {            
            base.MapComponentTick();
            
            if (ticks == 0 || (GenTicks.TicksGame + 13) % 30 == 0)
                UpdateGrid(gridHostile, friendlies, ref fIndex);            
            if (ticks == 0 || (GenTicks.TicksGame + 7) % 30 == 0)
                UpdateGrid(gridFriendly, hostiles, ref hIndex);

            ticks++;
            //if (DebugSettings.godMode && GenTicks.TicksGame % 15 == 0)
            //{
            //    TurretTracker turretTracker = map.GetComponent<TurretTracker>();
            //    IntVec3 center = UI.MouseMapPosition().ToIntVec3();
            //    if (center.InBounds(map))
            //    {                    
            //        foreach(IntVec3 cell in GenRadial.RadialCellsAround(center, 64, true))
            //        {
            //            if (gridHostile[cell] == 0)
            //                continue;
            //            if (cell.InBounds(map))                                                    
            //                map.debugDrawer.FlashCell(cell, (float)Mathf.Clamp(gridHostile[cell] / 10f, 0f, 0.95f), $"{gridHostile[cell]}", 15);                        
            //        }
            //    }
            //}
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
            if (pawn.Faction == null)
                return;
            if (pawnToInfo.ContainsKey(pawn))
                DeRegister(pawn);
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

        public void DeRegister(Pawn pawn)
        {
            if(!pawnToInfo.TryGetValue(pawn, out PawnSightRecord record))
                return;
            if (record.IsFriendly)            
                friendlies[record.index].Remove(pawn);
            else
                hostiles[record.index].Remove(pawn);
        }

        private void UpdateGrid(SightGrid grid, List<Pawn>[] pool, ref int index)
        {                        
            List<Pawn> pawns = pool[index];
            if (pawns.Count != 0)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (!pawn.Spawned || pawn.Downed || pawn.Suspended)
                        continue;                    
                    TryCastPawnSight(grid, pawn);                    
                }
            }
            index = (index + 1) % BUCKETCOUNT;
        }

        private void TryCastPawnSight(SightGrid grid, Pawn pawn)
        {            
            if (pawn.WorkTagIsDisabled(WorkTags.Violent) || pawn.equipment?.equipment == null)
                return;
            ThingWithComps weapon = pawn.equipment.Primary;
            if (weapon == null || !weapon.def.IsRangedWeapon)
                return;      
            float range = Mathf.Min(weapon.def.verbs?.Max(v => v.range) ?? -1, 62f) * 0.666f;
            if (range < 5.0f)
                return;            
            SkillRecord record = pawn.skills?.GetSkill(SkillDefOf.Shooting) ?? null;            
            if (record != null)
                range *= Mathf.Clamp(pawn.skills.GetSkill(SkillDefOf.Shooting).Level / 7.5f, 1.0f, 1.75f);
            grid.Next();
            ShadowCastingUtility.CastVisibility(
                map,
                pawn.Position,
                (cell) =>
                {                    
                    grid[cell] += 900;
                },
                Mathf.CeilToInt(range)
            );
        }
    }
}

