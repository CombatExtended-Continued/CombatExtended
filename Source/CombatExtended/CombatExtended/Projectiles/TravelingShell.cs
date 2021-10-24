using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class TravelingShell : TravelingThing
    {
        public ThingDef shellDef;        
        public GlobalShellingInfo shellingInfo;

        public override float TilesPerTick
        {
            get => shellingInfo.tilesPerTick;
        }

        public TravelingShell()
        {                        
        }

        public override void ExposeData()
        {
            Scribe_Deep.Look(ref shellingInfo, "shellInfo");
            Scribe_Defs.Look(ref shellDef, "shellDef");
            base.ExposeData();
        }

        protected override void Arrived()
        {
            int tile = Tile;
            Settlement settlement = Find.World.worldObjects.SettlementAt(tile);
            if(settlement != null && settlement.Faction != null && settlement.Faction.def != FactionDefOf.Mechanoid && !settlement.Faction.IsPlayerSafe())
            {
                var relation = settlement.Faction.RelationWith(Faction);
                var change = relation.baseGoodwill;                
                if(relation.baseGoodwill > 0)
                {
                    change = Mathf.Clamp(relation.baseGoodwill * relation.baseGoodwill + 25, -100, 100);
                }
                settlement.Faction.TryAffectGoodwillWith(Faction, (int) (-1f * change), canSendMessage: true, canSendHostilityLetter: true, HistoryEventDefOf.AttackedSettlement);
                relation.baseGoodwill -= 20;
            }
            Site site = Find.World.worldObjects.SiteAt(tile);
            if(site != null)
            {
                // damage site
            }
            MapParent mapParent = Find.World.worldObjects.MapParentAt(tile);
            if(mapParent != null)
            {                
                if (Find.Maps.Any(m => m.uniqueID == mapParent.Map.uniqueID)) // shell the map now
                {
                    SpawnShell(mapParent.Map);
                }
                else // queue map damage
                {

                }
            }            
        }       

        private void SpawnShell(Map map)
        {
            IntVec3 targetCell = shellingInfo.targetCell;
            if (!shellingInfo.targetCell.IsValid || !shellingInfo.targetCell.InBounds(map))
            {
                targetCell = FindRandomImpactCell(map);
            }
            if (!targetCell.IsValid)
            {
                Log.Warning("CE: Failed to find a proper position to impact an artillery shell");
                return;
            }
            // create a simple ray to check where we need to spawn the projectile
            Ray ray = new Ray(targetCell.ToVector3(), shellingInfo.InboundVec);
            // prepare the map size for the bound
            Vector3 mapSize = map.Size.ToVector3();
            mapSize.y = Mathf.Max(mapSize.x, mapSize.z);
            // use map bounds to find where the shell should enter from
            Bounds mapBounds = new Bounds((mapSize / 2f).Yto0(), mapSize);
            mapBounds.IntersectRay(ray, out float distanceToEdge);
            IntVec3 sourceCell = ray.GetPoint(distanceToEdge * 0.75f).ToIntVec3();                        
            Launch(
                sourceCell,
                targetCell,
                map,                                                             
                shotSpeed: 120f);
        }        

        private IntVec3 FindRandomImpactCell(Map map)
        {            
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.ElapsedTicks / Stopwatch.Frequency < 0.01f)
            {
                IntVec3 cell = new IntVec3(Rand.Range(5, map.cellIndices.mapSizeX - 5), 0, Rand.Range(5, map.cellIndices.mapSizeZ - 5));
                RoofDef roof = map.roofGrid.RoofAt(cell);
                if (roof == null || roof == RoofDefOf.RoofConstructed)
                {
                    return cell;
                }
            }
            stopwatch.Stop();
            Log.Message($"{stopwatch.ElapsedMilliseconds} MS");
            return IntVec3.Invalid;
        }       

        private void Launch(IntVec3 sourceCell, LocalTargetInfo target, Map map, float shotSpeed = 20)
        {
            float shotHeight = 100;
            IntVec3 targetCell = target.Cell;           
            Vector2 source = new Vector2(sourceCell.x, sourceCell.z);
            Vector2 destination = new Vector2(targetCell.x, targetCell.z);
            Vector2 w = (destination - source);            
            ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(shellDef);
            float shotRotation = (-90 + Mathf.Rad2Deg * Mathf.Atan2(w.y, w.x)) % 360;
            ProjectilePropertiesCE pprops = projectile.def.projectile as ProjectilePropertiesCE;
            float shotAngle = ProjectileCE.GetShotAngle(shotSpeed, (destination - source).magnitude, -shotHeight, false, pprops.Gravity);                        
            projectile.canTargetSelf = false;
            projectile.Position = sourceCell;
            projectile.SpawnSetup(map, false);            
            projectile.Launch(null, source, shotAngle, shotRotation, shotHeight, shotSpeed);                    
        }
    }
}
