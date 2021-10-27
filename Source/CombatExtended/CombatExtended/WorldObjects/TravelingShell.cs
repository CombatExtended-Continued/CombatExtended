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

        public override bool ExpandingIconFlipHorizontal
        {
            get => GenWorldUI.WorldToUIPosition(Start).x > GenWorldUI.WorldToUIPosition(End).x;
        }

        public override float ExpandingIconRotation
        {
            get
            {
                Vector2 start = GenWorldUI.WorldToUIPosition(Start);
                Vector2 end = GenWorldUI.WorldToUIPosition(End);
                float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * 57.29578f;
                if (angle > 180f)
                {
                    angle -= 180f;
                }
                return angle + 90f;
            }
        }                

        public TravelingShell()
        {                        
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref shellingInfo, "shellInfo");
            Scribe_Defs.Look(ref shellDef, "shellDef");            
        }       

        protected override void Arrived()
        {            
            int tile = Tile;
            MapParent mapParent = Find.World.worldObjects.MapParentAt(tile);
            if(mapParent == null)
            {
                return;
            }
            WorldObjects.ShellingComp shellingComp = mapParent.GetComponent<WorldObjects.ShellingComp>();
            if(shellingComp == null)
            {
                return;
            }            
            Faction faction = this.shellingInfo.Caster?.Faction ?? this.shellingInfo.Shooter?.Faction;
            if(faction != null) // check the projectile faction
            {
                shellingComp.Notify_ShelledBy(faction, shellingInfo.sourceTile);
            }                                            
            if (mapParent != null && mapParent.HasMap) // spawn the actual mortar shell
            {
                SpawnShell(mapParent.Map);
            }
            else // queue map damage
            {                               
                shellingComp.ApplyDamage(((ProjectilePropertiesCE)shellDef.projectile).shellingProps, faction, shellingInfo.sourceTile);
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
                map: map,                                                             
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
            return IntVec3.Invalid;
        }       

        private void Launch(IntVec3 sourceCell, LocalTargetInfo target, Map map, float shotSpeed = 20, float shotHeight = 100)
        {            
            IntVec3 targetCell = target.Cell;           
            Vector2 source = new Vector2(sourceCell.x, sourceCell.z);
            Vector2 destination = new Vector2(targetCell.x, targetCell.z);

            //Pawn shooter = shellingInfo.Shooter;
            //ProjectilePropertiesCE props = shellDef.projectile as ProjectilePropertiesCE;
            ////float distanceFactor = Mathf.Max(Find.WorldGrid.TraversalDistanceBetween(shellingInfo.sourceTile, shellingInfo.targetTile, true, maxDist:(int)(props.shellingProps.range * 1.2f)) / props.shellingProps.range, 0.50f);
            Vector2 shiftVec = new Vector2();
            //if (shooter != null)
            //{
            //    float shootingLevel = shooter.skills.GetSkill(SkillDefOf.Shooting).Level;
            //    shiftVec.x = distanceFactor * Rand.Range(-destination.x, map.Size.x - destination.x - 1) / shootingLevel;
            //    shiftVec.y = distanceFactor * Rand.Range(-destination.y, map.Size.z - destination.y - 1) / shootingLevel;
            //}
            //else
            {
                shiftVec.x = Rand.Range(-destination.x, map.Size.x - destination.x - 1) / 12f;
                shiftVec.y = Rand.Range(-destination.y, map.Size.z - destination.y - 1) / 12f;
            }
            destination += shiftVec;
            Vector2 w = (destination - source);

            ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(shellDef);
            ProjectilePropertiesCE pprops = projectile.def.projectile as ProjectilePropertiesCE;
            float shotRotation = (-90 + Mathf.Rad2Deg * Mathf.Atan2(w.y, w.x)) % 360;            
            float shotAngle = ProjectileCE.GetShotAngle(shotSpeed, (destination - source).magnitude, -shotHeight, false, pprops.Gravity);
            
            projectile.canTargetSelf = false;
            projectile.Position = sourceCell;
            projectile.SpawnSetup(map, false);
            projectile.Launch(null, source, shotAngle, shotRotation, shotHeight, shotSpeed);                    
        }       
    }
}
