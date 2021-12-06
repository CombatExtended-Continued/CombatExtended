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
        private HashSet<IntVec3> _drawnCells = new HashSet<IntVec3>();

        public class SightReader
        {            
            public SightGrid friendly;
            public SightGrid hostile;
            public SightGrid universal;
            public SightGrid turrets;

            private readonly Map map;
            private readonly CellIndices indices;
            private readonly SightTracker tacker;

            public SightTracker Tacker
            {
                get => tacker;
            }
            public Map Map
            {
                get => map;
            }

            public SightReader(SightTracker tracker)
            {
                this.tacker = tracker;
                this.map = tracker.map;
                this.indices = tracker.map.cellIndices;
            }

            public float GetEnemies(IntVec3 cell) => GetEnemies(indices.CellToIndex(cell));
            public float GetEnemies(int index)
            {                
                float value = 0f;
                if (hostile != null)
                    value += hostile[index];
                if (turrets != null)
                    value += turrets[index];
                if (universal != null)
                    value += universal[index];
                return value;
            }

            public float GetFriendlies(IntVec3 cell) => GetFriendlies(indices.CellToIndex(cell));
            public float GetFriendlies(int index) => friendly != null ? friendly[index] : 0f;            

            public float GetSightCoverRating(IntVec3 cell) => GetSightCoverRating(indices.CellToIndex(cell));
            public float GetSightCoverRating(int index)
            {
                float value = 0f;
                if (hostile != null)
                    value += hostile.GetCellSightCoverRating(index);
                if (turrets != null)
                    value += turrets.GetCellSightCoverRating(index);
                if (universal != null)
                    value += universal.GetCellSightCoverRating(index);
                return value;
            }

            public float GetVisibility(IntVec3 cell) => GetVisibility(indices.CellToIndex(cell));
            public float GetVisibility(int index)
            {
                float value = 0f;
                if (hostile != null)
                    value += hostile.GetVisibility(index);
                if (turrets != null)
                    value += turrets.GetVisibility(index);
                if (universal != null)
                    value += universal.GetVisibility(index);
                return value;
            }

            public Vector2 GetDirection(IntVec3 cell) => GetDirection(indices.CellToIndex(cell));
            public Vector2 GetDirection(int index)
            {
                Vector2 value = Vector2.zero;
                if (hostile != null)
                    value += hostile.GetDirectionAt(index);
                if (turrets != null)
                    value += turrets.GetDirectionAt(index);
                if (universal != null)
                    value += universal.GetDirectionAt(index);                
                return value;
            }

            public Vector2 GetFriendlyDirection(IntVec3 cell) => GetFriendlyDirection(indices.CellToIndex(cell));
            public Vector2 GetFriendlyDirection(int index) => friendly != null ? friendly.GetDirectionAt(index) : Vector2.zero;          
        }

        public readonly SightManager_Pawns friendly;
        public readonly SightManager_Pawns hostile;
        public readonly SightManager_Pawns universal;
        public readonly SightManager_Turrets turrets;       
        
        public SightTracker(Map map) : base(map)
        {
            friendly =
                new SightManager_Pawns(this, 20, 4);
            hostile =
                new SightManager_Pawns(this, 20, 4);
            universal =
                new SightManager_Pawns(this, 20, 10);
            turrets =
                new SightManager_Turrets(this, 20, 100);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            // --------------
            friendly.Tick();
            // --------------
            hostile.Tick();
            // --------------
            universal.Tick();
            // --------------
            turrets.Tick();
            //
            // debugging stuff.
            if (Controller.settings.DebugDrawLOSShadowGrid && GenTicks.TicksGame % 15 == 0)
            {
                _drawnCells.Clear();
                if (!Find.Selector.SelectedPawns.NullOrEmpty())
                {
                    foreach (Pawn pawn in Find.Selector.SelectedPawns)
                    {
                        pawn.GetSightReader(out SightReader reader);
                        IntVec3 center = pawn.Position;
                        if (center.InBounds(map))
                        {
                            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 64, true))
                            {
                                if (cell.InBounds(map) && !_drawnCells.Contains(cell))
                                {
                                    _drawnCells.Add(cell);
                                    var value = reader.GetEnemies(cell);
                                    if (value > 0)
                                        map.debugDrawer.FlashCell(cell, (float)reader.GetVisibility(cell) / 10f, $"{Math.Round(value, 3)} {value}", 15);
                                }
                            }
                        }
                    }
                }
                else
                {
                    IntVec3 center = UI.MouseMapPosition().ToIntVec3();
                    if (center.InBounds(map))
                    {
                        foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 64, true))
                        {
                            if (cell.InBounds(map) && !_drawnCells.Contains(cell))
                            {
                                _drawnCells.Add(cell);
                                var value = hostile.grid.GetVisibility(cell, out int enemies1) + friendly.grid.GetVisibility(cell, out int enemies2);
                                if (value > 0)
                                    map.debugDrawer.FlashCell(cell, (float)value / 10f, $"{Math.Round(value, 3)} {enemies1 + enemies2}", 15);
                            }
                        }
                    }
                }
                if(_drawnCells.Count > 0)
                    _drawnCells.Clear();
            }
        }

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();
            if (Controller.settings.DebugDrawLOSVectorGrid)
            {
                TurretTracker turretTracker = map.GetComponent<TurretTracker>();
                IntVec3 center = UI.MouseMapPosition().ToIntVec3();
                if (center.InBounds(map))
                {
                    foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 24, true))
                    {                        
                        float enemies = hostile.grid[cell] + friendly.grid[cell];
                        Vector3 dir = hostile.grid.GetDirectionAt(cell) + friendly.grid.GetDirectionAt(cell);
                        if (cell.InBounds(map) && enemies > 0)
                        {
                            Vector2 direction = dir.normalized * 0.5f;
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
                        }
                    }
                }
            }
        }
       
        public bool TryGetReader(Pawn pawn, out SightReader reader) => TryGetReader(pawn.Faction, out reader);
        public bool TryGetReader(Faction faction, out SightReader reader)
        {
            if (faction == null)
            {
                reader = null;
                return false;
            }
            if (faction.def == FactionDefOf.Mechanoid || faction.def == FactionDefOf.Insect)
            {
                reader = new SightReader(this);
                reader.hostile = friendly.grid;
                reader.friendly = universal.grid;
                reader.turrets = turrets.grid;
                reader.universal = hostile.grid;
            }
            reader = new SightReader(this);
            if (faction.HostileTo(map.ParentFaction))
            {                
                reader.hostile = friendly.grid;
                reader.friendly = hostile.grid;
                reader.turrets = turrets.grid;                
            }
            else
            {                
                reader.hostile = hostile.grid;
                reader.friendly = friendly.grid;
                reader.turrets = null;
            }
            reader.universal = universal.grid;
            return true;
        }

        public void Register(Building_TurretGunCE turret)
        {
            if(turret.Faction == map.ParentFaction)
            {
                turrets.DeRegister(turret);
                turrets.Register(turret);
            }
        }

        public void Register(Pawn pawn)
        {
            if (pawn.Faction == null)
                return;
            // make sure it's not already in.
            friendly.DeRegister(pawn);
            // make sure it's not already in.
            hostile.DeRegister(pawn);
            // make sure it's not already in.
            universal.DeRegister(pawn);

            if (pawn.Faction.def == FactionDefOf.Insect || pawn.Faction.def == FactionDefOf.Mechanoid)
            {
                universal.Register(pawn);
                return;
            }
            // now register the new pawn.
            if (pawn.Faction?.HostileTo(map.ParentFaction) ?? true)
                hostile.Register(pawn);
            else
                friendly.Register(pawn);            
        }

        public void DeRegister(Building_TurretGunCE turret) => turrets.DeRegister(turret);
        public void DeRegister(Pawn pawn)
        {
            // cleanup hostiltes incase pawn switched factions.
            hostile.DeRegister(pawn);
            // cleanup friendlies incase pawn switched factions.
            friendly.DeRegister(pawn);
            // cleanup universals incase everything else fails.
            universal.DeRegister(pawn);           
        }

        public override void MapRemoved()
        {
            base.MapRemoved();
            // TODO redo this
            hostile.Notify_MapRemoved();
            // TODO redo this
            friendly.Notify_MapRemoved();
            // TODO redo this
            universal.Notify_MapRemoved();
        }
    }
}

