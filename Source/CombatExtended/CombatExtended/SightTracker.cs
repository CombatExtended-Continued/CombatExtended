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
        public SightGridManager_Humanlikes friendlies;
        public SightGridManager_Humanlikes hostiles;
        public SightGridManager_UniversalEnemies mechsInsects;
        public SightGridUpdater_Turrets turrets;

        public SightGrid Friendly => friendlies.grid;
        public SightGrid Hostile => hostiles.grid;
        public SightGrid UniversalEnemies => mechsInsects.grid;
        
        public SightTracker(Map map) : base(map)
        {            
            friendlies =
                new SightGridManager_Humanlikes(this, 20, 4);
            hostiles =
                new SightGridManager_Humanlikes(this, 20, 4);
            mechsInsects =
                new SightGridManager_UniversalEnemies(this, 20, 10);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            // --------------
            friendlies.Tick();
            // --------------
            hostiles.Tick();
            // --------------
            mechsInsects.Tick();

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
                            var value = hostiles.grid.GetVisibility(cell, out int enemies);
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
                        if (cell.InBounds(map) && hostiles.grid[cell] >= 0)
                        {
                            Vector2 direction = hostiles.grid.GetDirectionAt(cell).normalized * 0.5f;

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
                            float value = hostiles.grid[cell];
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
            grid = !pawn.Faction.HostileTo(map.ParentFaction) ? friendlies.grid : hostiles.grid;
            return true;
        }

        public void Register(Pawn pawn)
        {
            if(pawn.RaceProps.Humanlike && pawn.RaceProps.intelligence == Intelligence.Humanlike)
            {
                // make sure it's not already in.
                friendlies.DeRegister(pawn);
                hostiles.DeRegister(pawn);
                // now register the new pawn.
                if (pawn.Faction?.HostileTo(map.ParentFaction) ?? true)
                    friendlies.Register(pawn);
                else
                    hostiles.Register(pawn);
            }
            else if(pawn.RaceProps.Insect || pawn.RaceProps.IsMechanoid)            
                mechsInsects.Register(pawn);            
        }        

        public void DeRegister(Pawn pawn)
        {
            // cleanup hostiltes incase pawn switched factions.
            hostiles.DeRegister(pawn);
            // cleanup friendlies incase pawn switched factions.
            friendlies.DeRegister(pawn);
            // cleanup universals incase everything else fails.
            mechsInsects.DeRegister(pawn);           
        }

        public override void MapRemoved()
        {
            base.MapRemoved();
            // TODO redo this
            hostiles.Notify_MapRemoved();
            // TODO redo this
            friendlies.Notify_MapRemoved();
            // TODO redo this
            mechsInsects.Notify_MapRemoved();
        }
    }
}

