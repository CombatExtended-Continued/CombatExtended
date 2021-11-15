using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class TravelingShell : TravelingThing
    {
        public GlobalTargetInfo globalTarget;
        public GlobalTargetInfo globalSource;
        public ThingDef equipmentDef;
        public ThingDef shellDef;
        public Thing launcher;

        public override string Label
        {
            get => shellDef.label;
        }

        public override float TilesPerTick
        {
            get => (shellDef.projectile as ProjectilePropertiesCE).shellingProps.tilesPerTick;
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
            Scribe_Defs.Look(ref equipmentDef, "equipmentDef");
            Scribe_Defs.Look(ref shellDef, "ShellDef");
            CE_Scriber.Late(this, (id) =>
            {
                Scribe_TargetInfo.Look(ref globalTarget, "globalTarget_" + id);
                Scribe_TargetInfo.Look(ref globalSource, "globalSource_" + id);                
                Scribe_References.Look(ref launcher, "launcher_" + id );
            });
        }        

        public override string GetDescription()
        {
            MapParent source = Find.WorldObjects.MapParentAt(globalSource.Tile);
            MapParent target = Find.WorldObjects.MapParentAt(globalTarget.Tile);
            StringBuilder builder = new StringBuilder();
            if (shellDef != null)
            {
                builder.Append(shellDef.description);
            }
            string message = "CE_TravelingShell_Description".Translate();
            string color1 = source?.Faction?.NameColored ?? "gray";
            string color2 = target?.Faction?.NameColored ?? "gray";
            builder.AppendFormat(message, $"<color={color1}>{source?.Label ?? $"{globalSource.Tile}"}</color>", source?.Faction?.Name ?? "Unknown", $"<color={color1}>{target?.Label ?? $"{globalTarget.Tile}"}</color>", target?.Faction?.Name ?? "Unknown");
            return builder.ToString();
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
            Faction faction = Faction;               
            if(faction != null && mapParent.Faction != null && mapParent.Faction != faction) // check the projectile faction
            {
                FactionRelation relation = faction.RelationWith(mapParent.Faction, true);
                if(relation == null)
                {
                    faction.TryMakeInitialRelationsWith(mapParent.Faction);
                    relation = faction.RelationWith(mapParent.Faction, true); 
                }
                faction.TryAffectGoodwillWith(mapParent.Faction, -75, true, true, HistoryEventDefOf.AttackedSettlement, globalTarget);
            }                                            
            if (mapParent != null && mapParent.HasMap) // spawn the actual mortar shell
            {
                SpawnShell(mapParent.Map);
            }
            else // queue map damage
            {                               
            }
        }       

        private void SpawnShell(Map map)
        {
            IntVec3 targetCell = globalTarget.Cell;
            if (!globalTarget.Cell.IsValid || !globalTarget.Cell.InBounds(map))
            {
                targetCell = FindRandomImpactCell(map);
            }            
            Vector3 direction = (Find.WorldGrid.GetTileCenter(globalSource.Tile) - Find.WorldGrid.GetTileCenter(globalTarget.Tile)).normalized;
            Vector3 mapSize = map.Size.ToVector3();
            mapSize.y = Mathf.Max(mapSize.x, mapSize.z);

            Ray ray = new Ray(targetCell.ToVector3(), -1 * direction);
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
            Vector2 w = (destination - source);

            ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(shellDef);
            ProjectilePropertiesCE pprops = projectile.def.projectile as ProjectilePropertiesCE;
            float shotRotation = (-90 + Mathf.Rad2Deg * Mathf.Atan2(w.y, w.x)) % 360;
            float shotAngle = ProjectileCE.GetShotAngle(shotSpeed, (destination - source).magnitude, -shotHeight, false, pprops.Gravity);

            projectile.canTargetSelf = false;
            projectile.Position = sourceCell;
            projectile.SpawnSetup(map, false);
            projectile.Launch(launcher, source, shotAngle, shotRotation, shotHeight, shotSpeed);
        }       
    }
}
