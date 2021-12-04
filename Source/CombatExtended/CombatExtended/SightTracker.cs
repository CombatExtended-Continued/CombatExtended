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
    public class SightTracker : MapComponent
    {
        private const int BUCKETCOUNT = 20;
        private const int BUCKETINTERVAL = 15;
        private const float SIGHTINTERVAL = BUCKETCOUNT * BUCKETINTERVAL;

        private static readonly Vector2 DEBUGDOTOFFSET = new Vector2(-1, -1);
        private static readonly Vector2 DEBUGDOTSIZE = new Vector2(3, 3);

        private class PawnSightRecord
        {
            /// <summary>
            /// The bucket index of the owner pawn.
            /// </summary>
            public int bucketIndex;
            /// <summary>
            /// Owner pawn.
            /// </summary>
            public Pawn pawn;
            /// <summary>
            /// Wether the parent pawn is an insect.
            /// </summary>
            public bool insect;
            /// <summary>
            /// Wether the parent pawn's faction is friendly to the parent map.
            /// </summary>
            public bool friendly;            
            /// <summary>
            /// The tick at which this pawn was updated.
            /// </summary>
            public int lastCycle = -1;
            /// <summary>
            /// Last range of value of the owner pawn.
            /// </summary>
            public float lastRange = -1;
            /// <summary>
            /// Number of other pawns skipping casting by using this pawn.
            /// </summary>
            public int carry;
            /// <summary>
            /// The sum of ranges of the carried pawns.
            /// </summary>
            public int carryRange;
            /// <summary>
            /// The sum of positions for the carried pawns.
            /// </summary>
            public IntVec3 carryPosition;            
            /// <summary>
            /// Returns wether this pawn is friendly to the parent map.
            /// </summary>
            public bool IsFriendly
            {
                get => friendly;
            }
            /// <summary>
            /// Returns wether this pawn is friendly to the parent map.
            /// </summary>
            public bool IsHostile
            { 
                get => !friendly;
            }
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
            gridFriendly = new SightGrid(map);
            gridHostile = new SightGrid(map);
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
                performanceRangeFactor = 0.5f - Mathf.Min(pawnsCastedNum, 75f) / 75f * 0.35f;
                pawnsCastedNum = updateNum = 0;                
            }

            if (Controller.settings.DebugDrawLOSShadowGrid && GenTicks.TicksGame % 15 == 0)
            {
                TurretTracker turretTracker = map.GetComponent<TurretTracker>();
                IntVec3 center = UI.MouseMapPosition().ToIntVec3();
                if (center.InBounds(map))
                {
                    foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 32, true))
                    {
                        if (cell.InBounds(map))
                        {
                            var value = gridHostile.GetVisibility(cell, out int enemies);
                            if (value > 0)
                                map.debugDrawer.FlashCell(cell, (float)value/ 10f, $"{Math.Round(value, 3)} {enemies}", 15);
                        }
                    }
                }
            }
        }

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();
            if (Controller.settings.DebugDrawLOSShadowGrid)
            {
                TurretTracker turretTracker = map.GetComponent<TurretTracker>();
                IntVec3 center = UI.MouseMapPosition().ToIntVec3();
                if (center.InBounds(map))
                {
                    foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 32, true))
                    {
                        //if (cell.InBounds(map) && turretTracker.GetVisibleToTurret(cell))
                        //    map.debugDrawer.FlashCell(cell, 0.5f, $"{1f}", 15);                        
                        if (cell.InBounds(map) && gridHostile[cell] >= 0)
                        {
                            Vector2 direction = gridHostile.GetDirectionAt(cell).normalized * 0.5f;

                            Vector2 start = UI.MapToUIPosition(cell.ToVector3Shifted());
                            Vector2 end = UI.MapToUIPosition(cell.ToVector3Shifted() + new Vector3(direction.x, 0, direction.y));
                            if (Vector2.Distance(start, end) > 1f
                                && start.x > 0
                                && start.y > 0
                                && end.x > 0
                                && end.y > 0
                                && start.x < UI.screenWidth
                                && start.y < UI.screenHeight
                                && end.x < UI.screenWidth
                                && end.y < UI.screenHeight)
                                Widgets.DrawLine(start, end, Color.white, 1);
                            float value = gridHostile[cell];
                        }
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
                    bucketIndex = pawn.thingIDNumber % BUCKETCOUNT
                };
                pawnToInfo[pawn] = record;
                insects[record.bucketIndex].Add(pawn);
            }
            else if (pawn.Faction != null && pawn.RaceProps.Humanlike)
            {
                PawnSightRecord record = new PawnSightRecord()
                {
                    pawn = pawn,
                    friendly = !pawn.Faction.HostileTo(map.ParentFaction),
                    bucketIndex = pawn.thingIDNumber % BUCKETCOUNT
                };
                pawnToInfo[pawn] = record;
                if (record.IsFriendly)
                    friendlies[record.bucketIndex].Add(pawn);
                else
                    hostiles[record.bucketIndex].Add(pawn);
            }
        }

        public void DeRegister(Pawn pawn)
        {
            if (!pawnToInfo.TryGetValue(pawn, out PawnSightRecord record))
                return;
            if (record.insect)
                insects[record.bucketIndex].Remove(pawn);
            else
            {
                if (record.IsFriendly)
                    friendlies[record.bucketIndex].Remove(pawn);
                else
                    hostiles[record.bucketIndex].Remove(pawn);
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
                    if (TryCastPawnSight(grid, pawn))
                        pawnsCastedNum++;
                }
            }
            index++;
            if(index >= pool.Length)
            {
                index = 0;
                grid.NextCycle();
            }
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
            if (grid.CycleNum == sightRecord.lastCycle)
                return false;

            if (pawn.WorkTagIsDisabled(WorkTags.Violent) || pawn.equipment?.equipment == null || GenTicks.TicksGame - pawn.needs?.rest?.lastRestTick <= 30)
                return false;

            ThingWithComps weapon = pawn.equipment.Primary;
            if (weapon == null || !weapon.def.IsRangedWeapon)
                return false;

            float range;
            range = Mathf.Min(weapon.def.verbs?.Max(v => v.range) ?? -1, 62f) * performanceRangeFactor;            
            range = (range + sightRecord.carryRange) / (1 + sightRecord.carry);
            if (range < 3.0f)
                return false;
                
            SkillRecord record = pawn.skills?.GetSkill(SkillDefOf.Shooting) ?? null;
            if (record != null)
            {
                float skill = pawn.skills.GetSkill(SkillDefOf.Shooting).Level;
                if (map.IsNightTime())
                    skill = Mathf.Max(skill - (1 - pawn.GetStatValue(CE_StatDefOf.NightVisionEfficiency)) * 4, 0f);
                
                range *= Mathf.Clamp(skill / 7.5f, 0.85f, 1.75f);
            }                                   
            if (sightRecord.carry == 0)
            {
                PawnSightRecord best = null;
                int bestExtras = -1;
                foreach (Pawn p in pawn.Position.PawnsInRange(map, 8 * (1 - performanceRangeFactor)))
                {
                    if (p != pawn && pawnToInfo.TryGetValue(p, out PawnSightRecord s))
                    {
                        if (s.friendly == sightRecord.friendly
                            && s.insect == sightRecord.insect
                            && grid.CycleNum - s.lastCycle == 1
                            && s.lastRange - range > -15
                            && bestExtras < s.carry)
                        {
                            best = s;
                            bestExtras = s.carry;
                        }
                    }
                }
                if (best != null)
                {
                    best.carryRange += (int) range;
                    best.carryPosition += GetShiftedPosition(pawn);
                    best.carry++;
                    sightRecord.lastCycle = grid.CycleNum;
                    return false;
                }
            }
            IntVec3 shiftedPos = GetShiftedPosition(pawn) + sightRecord.carryPosition;
            shiftedPos = new IntVec3((int)((shiftedPos.x) / (sightRecord.carry + 1f)), 0, (int)((shiftedPos.z) / (sightRecord.carry + 1f)));            
            grid.Next(shiftedPos, range);
            grid.Set(shiftedPos, sightRecord.carry + 1, 1);
            ShadowCastingUtility.CastVisibility(
                map,
                shiftedPos,
                (cell, dist) =>
                {
                    grid.Set(cell, sightRecord.carry + 1, dist);
                },
                Mathf.CeilToInt(range)
            );            
            sightRecord.carry = 0;
            sightRecord.carryRange = 0;
            sightRecord.carryPosition = IntVec3.Zero;
            sightRecord.lastCycle = grid.CycleNum;
            sightRecord.lastRange = range;
            return true;
        }

        private bool TryCastInsect(Pawn insect)
        {
            //PawnSightRecord sightRecord = pawnToInfo[insect];
            //if (grid.Cycle == sightRecord.lastCycle)
            //    return false;
            //float range = (10 + sightRecord.carryRange) * performanceRangeFactor / (1 + sightRecord.carry);
            //if (sightRecord.carry == 0)
            //{
            //    PawnSightRecord best = null;
            //    int bestExtras = -1;
            //    foreach (Pawn p in insect.Position.PawnsInRange(map, 8 * (1 - performanceRangeFactor)))
            //    {
            //        if (p != insect && pawnToInfo.TryGetValue(p, out PawnSightRecord s))
            //        {
            //            if (s.friendly == sightRecord.friendly
            //                && s.insect
            //                && GenTicks.TicksGame - s.lastCycle < SIGHTINTERVAL
            //                && bestExtras < s.carry)
            //            {
            //                best = s;
            //                bestExtras = s.carry;
            //            }
            //        }
            //    }
            //    if (best != null)
            //    {
            //        best.carry++;
            //        return false;
            //    }
            //}
            //IntVec3 shiftedPos = insect.Position;
            //shiftedPos = new IntVec3((int)((shiftedPos.x + sightRecord.carryPosition.x) / (sightRecord.carry + 1f)), 0, (int)((shiftedPos.z + sightRecord.carryPosition.z) / (sightRecord.carry + 1f)));            
            //gridFriendly.Next(shiftedPos, range);
            //gridFriendly.Set(shiftedPos, 1 + sightRecord.carry, 1);
            //gridHostile.Next(shiftedPos, range);                        
            //gridHostile.Set(shiftedPos, 1 + sightRecord.carry, 1);
            //ShadowCastingUtility.CastVisibility(
            //    map,
            //    shiftedPos,
            //    (cell, dist) =>
            //    {
            //        gridFriendly.Set(cell, 1 + sightRecord.carry, dist);
            //        gridHostile.Set(cell, 1 + sightRecord.carry, dist);
            //    },
            //    Mathf.CeilToInt(range)
            //);
            //sightRecord.carry = 0;
            //sightRecord.carryRange = 0;
            //sightRecord.carryPosition = IntVec3.Zero;
            //sightRecord.lastCycle = GenTicks.TicksGame;
            //sightRecord.lastRange = range;
            return true;
        }

        private static IntVec3 GetShiftedPosition(Pawn pawn)
        {
            PawnPath path;

            if (!(pawn.pather?.moving ?? false) || (path = pawn.pather.curPath) == null || path.NodesLeftCount <= 1)
                return pawn.Position;

            float distanceTraveled = Mathf.Min(pawn.GetStatValue(StatDefOf.MoveSpeed) * SIGHTINTERVAL / 60f, path.NodesLeftCount - 1);
            //
            // IntVec3 position = path.Peek(Mathf.FloorToInt(distanceTraveled));
            // pawn.Map.debugDrawer.FlashCell(pawn.Position, 0.1f, "original");
            // pawn.Map.debugDrawer.FlashCell(position, 0.9f, "shifted");
            return path.Peek(Mathf.FloorToInt(distanceTraveled));
        }
    }
}

