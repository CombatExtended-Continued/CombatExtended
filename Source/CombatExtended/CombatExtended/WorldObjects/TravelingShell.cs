﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CombatExtended.WorldObjects;
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
        private Texture2D expandingIcon;
        public override Texture2D ExpandingIcon
        {
            get
            {
                if (expandingIcon == null)
                {
                    var iconPath = (shellDef?.projectile as ProjectilePropertiesCE)?.shellingProps?.iconPath;
                    expandingIcon = iconPath.NullOrEmpty() ? base.ExpandingIcon : ContentFinder<Texture2D>.Get(iconPath, true);
                }
                return expandingIcon;
            }
        }
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
            Scribe_TargetInfo.Look(ref globalTarget, "globalTarget");
            Scribe_TargetInfo.Look(ref globalSource, "globalSource");
            CE_Scriber.Late(this, (id) =>
            {
                Scribe_References.Look(ref launcher, "launcher" + id);
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
            foreach (WorldObject worldObject in Find.World.worldObjects.ObjectsAt(tile))
            {
                if (TryShell(worldObject))
                {
                    break;
                }
            }
        }

        private bool TryShell(WorldObject worldObject)
        {
            bool shelled = false;
            if (worldObject is MapParent mapParent && mapParent.HasMap && Find.Maps.Contains(mapParent.Map))
            {
                shelled = true;
                Map map = mapParent.Map;
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
                LaunchProjectile(
                    sourceCell,
                    targetCell,
                    map: map,
                    shotSpeed: 55f);
            }
            WorldObjects.HostilityComp hostility = worldObject.GetComponent<WorldObjects.HostilityComp>();
            WorldObjects.HealthComp healthComp = worldObject.GetComponent<WorldObjects.HealthComp>();
            if (worldObject.Faction != Faction.OfPlayer && hostility != null && healthComp != null)
            {
                if (worldObject.Faction != null)
                {
                    hostility.TryHostilityResponse(Faction, new GlobalTargetInfo(StartTile));
                }
                if (!shelled)
                {
                    shelled = true;
                    healthComp.ApplyDamage(shellDef, Faction, globalSource);
                }
            }
            return shelled;
        }

        private void LaunchProjectile(IntVec3 sourceCell, LocalTargetInfo target, Map map, float shotSpeed = 20, float shotHeight = 200)
        {
            Vector3 source = new Vector3(sourceCell.x, shotHeight, sourceCell.z);
            Vector3 targetPos = target.Cell.ToVector3Shifted();

            ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(shellDef);
            ProjectilePropertiesCE pprops = projectile.def.projectile as ProjectilePropertiesCE;
            float shotRotation = pprops.TrajectoryWorker.ShotRotation(pprops, source, targetPos);
            float shotAngle = pprops.TrajectoryWorker.ShotAngle(pprops, source, targetPos, shotSpeed);

            projectile.canTargetSelf = false;
            projectile.Position = sourceCell;
            projectile.SpawnSetup(map, false);
            projectile.Launch(launcher, new Vector2(source.x, source.z), shotAngle, shotRotation, shotHeight, shotSpeed);
            //projectile.cameraShakingInit = Rand.Range(0f, 2f);
        }

        private IntVec3 FindRandomImpactCell(Map map) => ShellingUtility.FindRandomImpactCell(map, shellDef);
    }
}
