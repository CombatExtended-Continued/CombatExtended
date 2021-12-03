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
            public int index;

            public Pawn pawn;
            public bool insect;
            public bool friendly;            

            public int lastUpdated = -1;
            public float lastRange = -1;
            
            public int extras;
            public int extrasRange;
            public IntVec3 extraPos;            

            public bool IsFriendly
            {
                get => friendly;
            }

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
                                map.debugDrawer.FlashCell(cell, (float)Mathf.Clamp(value, 0f, 0.95f), $"{Math.Round(value, 3)} {enemies}", 15);
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
                            Vector2 direction = gridHostile.GetDirectionAt(cell) / 62f;

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
                    index = pawn.thingIDNumber % BUCKETCOUNT
                };
                pawnToInfo[pawn] = record;
                insects[record.index].Add(pawn);
            }
            else if (pawn.Faction != null && pawn.RaceProps.Humanlike)
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
            if (!pawnToInfo.TryGetValue(pawn, out PawnSightRecord record))
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
                    if (TryCastPawnSight(grid, pawn))
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

            float range;
            range = Mathf.Min(weapon.def.verbs?.Max(v => v.range) ?? -1, 62f) * performanceRangeFactor;            
            range = (range + sightRecord.extrasRange) / (1 + sightRecord.extras);
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
            if (sightRecord.extras == 0)
            {
                PawnSightRecord best = null;
                int bestExtras = -1;
                foreach (Pawn p in pawn.Position.PawnsInRange(map, 4))
                {
                    if (p != pawn && pawnToInfo.TryGetValue(p, out PawnSightRecord s))
                    {
                        if (s.friendly == sightRecord.friendly
                            && s.insect == sightRecord.insect
                            && GenTicks.TicksGame - s.lastUpdated < SIGHTINTERVAL
                            && s.lastRange - range > -10
                            && bestExtras < s.extras)
                        {
                            best = s;
                            bestExtras = s.extras;
                        }
                    }
                }
                if (best != null)
                {
                    best.extrasRange += (int) range;
                    best.extraPos += GetShiftedPosition(pawn);
                    best.extras++;                    
                    return false;
                }
            }
            IntVec3 shiftedPos = GetShiftedPosition(pawn) + sightRecord.extraPos;
            shiftedPos = new IntVec3((int)((shiftedPos.x) / (sightRecord.extras + 1f)), 0, (int)((shiftedPos.z) / (sightRecord.extras + 1f)));            
            grid.Next(shiftedPos, range);
            grid.Set(shiftedPos, sightRecord.extras + 1, 1);
            ShadowCastingUtility.CastVisibility(
                map,
                shiftedPos,
                (cell, dist) =>
                {
                    grid.Set(cell, sightRecord.extras + 1, dist);
                },
                Mathf.CeilToInt(range)
            );            
            sightRecord.extras = 0;
            sightRecord.extrasRange = 0;
            sightRecord.extraPos = IntVec3.Zero;
            sightRecord.lastUpdated = GenTicks.TicksGame;
            sightRecord.lastRange = range;
            return true;
        }

        private bool TryCastInsect(Pawn insect)
        {
            PawnSightRecord sightRecord = pawnToInfo[insect];            
            float range = (10 + sightRecord.extrasRange) * performanceRangeFactor / (1 + sightRecord.extras);
            if (sightRecord.extras == 0)
            {
                PawnSightRecord best = null;
                int bestExtras = -1;
                foreach (Pawn p in insect.Position.PawnsInRange(map, 4))
                {
                    if (p != insect && pawnToInfo.TryGetValue(p, out PawnSightRecord s))
                    {
                        if (s.friendly == sightRecord.friendly
                            && s.insect
                            && GenTicks.TicksGame - s.lastUpdated < SIGHTINTERVAL
                            && bestExtras < s.extras)
                        {
                            best = s;
                            bestExtras = s.extras;
                        }
                    }
                }
                if (best != null)
                {
                    best.extras++;
                    return false;
                }
            }
            IntVec3 shiftedPos = insect.Position;
            shiftedPos = new IntVec3((int)((shiftedPos.x + sightRecord.extraPos.x) / (sightRecord.extras + 1f)), 0, (int)((shiftedPos.z + sightRecord.extraPos.z) / (sightRecord.extras + 1f)));            
            gridFriendly.Next(shiftedPos, range);
            gridFriendly.Set(shiftedPos, 1 + sightRecord.extras, 1);
            gridHostile.Next(shiftedPos, range);                        
            gridHostile.Set(shiftedPos, 1 + sightRecord.extras, 1);
            ShadowCastingUtility.CastVisibility(
                map,
                shiftedPos,
                (cell, dist) =>
                {
                    gridFriendly.Set(cell, 1 + sightRecord.extras, dist);
                    gridHostile.Set(cell, 1 + sightRecord.extras, dist);
                },
                Mathf.CeilToInt(range)
            );            
            sightRecord.extras = 0;
            sightRecord.lastUpdated = GenTicks.TicksGame;
            sightRecord.lastRange = range;
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

