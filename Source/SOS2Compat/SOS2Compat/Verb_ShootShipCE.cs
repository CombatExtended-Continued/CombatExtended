using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using CombatExtended.Utilities;
using Verse.AI;
using UnityEngine;
using SaveOurShip2;
using Vehicles;
using SaveOurShip2.Vehicles;

namespace CombatExtended.Compatibility
{
    public class Verb_ShootShipCE : Verb_ShootCE
    {
        // This class combines functionality from SaveOurShip2.Verb_LaunchProjectileShip with Verb_ShootCE essentially making a hybrid verb that chooses its logic based on
        // if the turret that is shooting it is on the ground or in space. Also tweaks the logic from Verb_ShootCE so that shots are fired from up above the roof height as Ship Turrets
        // are on top of the hull
        public override float ShotHeight => 2.5f; // Set the height to be above roofs since ship guns are always mounted on top of roofs. Needed so we can shoot over the top of walls/roofs at targets
        public bool GroundDefenseMode
        {
            get
            {
                Building_ShipTurretCE turret = this.caster as Building_ShipTurretCE;
                if (turret != null)
                {
                    return turret.GroundDefenseMode;
                }
                return false;
            }
        }
        public VerbPropertiesShipWeaponCE VerbPropsShip => verbProps as VerbPropertiesShipWeaponCE;

        #region CE Functionality
        private Vector3 ShotSource
        {
            get
            {
                var casterPos = caster.DrawPos;
                return new Vector3(casterPos.x, ShotHeight, casterPos.z);
            }
        }

        // Copied from Verb_LaunchProjectileCE so we can call a modified version of CanHitFromCellIgnoringRange that checks if roofs are in the way of Line of Sight
        public override bool TryFindCEShootLineFromTo(IntVec3 root, LocalTargetInfo targ, out ShootLine resultingLine)
        {
            if (targ.HasThing && targ.Thing.Map != caster.Map)
            {
                resultingLine = default(ShootLine);
                return false;
            }
            if (EffectiveRange <= ShootTuning.MeleeRange) // If this verb has a MAX range up to melee range (NOT a MIN RANGE!)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                return ReachabilityImmediate.CanReachImmediate(root, targ, caster.Map, PathEndMode.Touch, null);
            }
            CellRect cellRect = (!targ.HasThing) ? CellRect.SingleCell(targ.Cell) : targ.Thing.OccupiedRect();
            float num = cellRect.ClosestDistSquaredTo(root);
            if (num > EffectiveRange * EffectiveRange || num < verbProps.minRange * verbProps.minRange)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                return false;
            }
            //if (!this.verbProps.NeedsLineOfSight) This method doesn't consider the currently loaded projectile
            if (Projectile.projectile.flyOverhead)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                return true;
            }

            // First check current cell for early opt-out
            IntVec3 dest;
            var shotSource = root.ToVector3Shifted();
            shotSource.y = ShotHeight;

            // Adjust for multi-tile turrets
            if (caster.def.building?.IsTurret ?? false)
            {
                shotSource = ShotSource;
            }

            if (CanHitFromCellIgnoringRange(shotSource, targ, out dest))
            {
                resultingLine = new ShootLine(root, dest);
                return true;
            }

            // For pawns, calculate possible lean locations
            if (CasterIsPawn)
            {
                ShootLeanUtility.LeanShootingSourcesFromTo(root, cellRect.ClosestCellTo(root), caster.Map, tempLeanShootSources);
                foreach (var leanLoc in tempLeanShootSources.OrderBy(c => c.DistanceTo(targ.Cell)))
                {
                    var leanOffset = 0.5f - 0.001f; // -0.001f ensures rounding works as intended regardless of whether leanOffset is positive or negative
                    var leanPosOffset = (leanLoc - root).ToVector3() * leanOffset;
                    if (CanHitFromCellIgnoringRange(shotSource + leanPosOffset, targ, out dest))
                    {
                        resultingLine = new ShootLine(leanLoc, dest);
                        return true;
                    }
                }
            }

            resultingLine = new ShootLine(root, targ.Cell);
            return false;
        }

        private bool CanHitFromCellIgnoringRange(Vector3 shotSource, LocalTargetInfo targ, out IntVec3 goodDest)
        {
            if (targ.Thing != null && targ.Thing.Map != caster.Map)
            {
                goodDest = IntVec3.Invalid;
                return false;
            }
            if (CanHitCellFromCellIgnoringRange(shotSource, targ.Cell, targ.Thing))
            {
                goodDest = targ.Cell;
                return true;
            }
            goodDest = IntVec3.Invalid;
            return false;
        }

        private bool CanHitCellFromCellIgnoringRange(Vector3 shotSource, IntVec3 targetLoc, Thing targetThing = null)
        {
            // Vanilla checks
            if (verbProps.mustCastOnOpenGround && (!targetLoc.Standable(caster.Map) || caster.Map.thingGrid.CellContains(targetLoc, ThingCategory.Pawn)))
            {
                return false;
            }
            if (verbProps.requireLineOfSight)
            {
                // Calculate shot vector
                Vector3 targetPos;
                if (targetThing != null)
                {
                    float shotHeight = shotSource.y;
                    AdjustShotHeight(caster, targetThing, ref shotHeight);
                    shotSource.y = shotHeight;
                    Vector3 targDrawPos = targetThing.DrawPos;
                    targetPos = new Vector3(targDrawPos.x, new CollisionVertical(targetThing).Max, targDrawPos.z);
                    var targPawn = targetThing as Pawn;
                    if (targPawn != null)
                    {
                        targetPos += targPawn.Drawer.leaner.LeanOffset * 0.6f;
                    }
                }
                else
                {
                    targetPos = targetLoc.ToVector3Shifted();
                }
                Ray shotLine = new Ray(shotSource, (targetPos - shotSource));

                // Create validator to check for intersection with partial cover
                var aimMode = CompFireModes?.CurrentAimMode;
                var lastHeight = shotSource.y;
                Predicate<IntVec3> CanShootThroughCell = (IntVec3 cell) =>
                {
                    Thing cover = cell.GetFirstPawn(caster.Map) ?? cell.GetCover(caster.Map);

                    // Calculate the height of the shot at this cell
                    float t = (cell.ToVector3Shifted() - shotSource).magnitude / (targetPos - shotSource).magnitude;
                    float shotHeightAtCell = Mathf.Lerp(shotSource.y, targetPos.y, t);

                    // Check if the cell is roofed and if the shot is moving from above to below. Not going to bother checking from below to above since ship guns are always on top of roofs.
                    if (cell.Roofed(caster.Map) && shotHeightAtCell <= 2f && lastHeight > 2f) // Roof height is 2f
                    {
                        return false;
                    }

                    if (cover != null && cover != ShooterPawn && cover != caster && cover != targetThing && !cover.IsPlant() && !(cover is Pawn && cover.HostileTo(caster)))
                    {
                        //Shooter pawns don't attempt to shoot targets partially obstructed by their own faction members or allies, except when close enough to fire over their shoulder
                        if (cover is Pawn cellPawn && !cellPawn.Downed && cellPawn.Faction != null && ShooterPawn?.Faction != null && (ShooterPawn.Faction == cellPawn.Faction || ShooterPawn.Faction.RelationKindWith(cellPawn.Faction) == FactionRelationKind.Ally) && !cellPawn.AdjacentTo8WayOrInside(ShooterPawn))
                        {
                            return false;
                        }

                        // Skip this check entirely if we're doing suppressive fire and cell is adjacent to target
                        if ((VerbPropsCE.ignorePartialLoSBlocker || aimMode == AimMode.SuppressFire) && cover.def.Fillage != FillCategory.Full)
                        {
                            return true;
                        }

                        Bounds bounds = CE_Utility.GetBoundsFor(cover);

                        // Simplified calculations for adjacent cover for gameplay purposes
                        if (cover.def.Fillage != FillCategory.Full && cover.AdjacentTo8WayOrInside(caster))
                        {
                            // Sanity check to prevent stuff behind us blocking LoS
                            var cellTargDist = cell.DistanceTo(targetLoc);
                            var shotTargDist = shotSource.ToIntVec3().DistanceTo(targetLoc);

                            if (shotTargDist > cellTargDist)
                            {
                                return cover is Pawn || bounds.size.y < shotSource.y;
                            }
                        }

                        // Check for intersect
                        if (bounds.IntersectRay(shotLine))
                        {
                            if (Controller.settings.DebugDrawPartialLoSChecks)
                            {
                                caster.Map.debugDrawer.FlashCell(cell, 0, bounds.extents.y.ToString());
                            }
                            return false;
                        }

                        if (Controller.settings.DebugDrawPartialLoSChecks)
                        {
                            caster.Map.debugDrawer.FlashCell(cell, 0.7f, bounds.extents.y.ToString());
                        }
                    }

                    return true;
                };

                // Add validator to parameters
                foreach (IntVec3 curCell in GenSightCE.PointsOnLineOfSight(shotSource, targetLoc.ToVector3Shifted()))
                {
                    if (Controller.settings.DebugDrawPartialLoSChecks)
                    {
                        caster.Map.debugDrawer.FlashCell(curCell, 0.4f);
                    }
                    if (curCell != shotSource.ToIntVec3() && curCell != targetLoc && !CanShootThroughCell(curCell))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        #endregion

        #region SOS2 Functionality
        public void PointDefense(Building_ShipTurretCE turret) // PD removes from target map
        {
            var mapComp = turret.Map.GetComponent<ShipMapComp>();
            if (mapComp.ShipCombatTargetMap != null)
            {
                //pods
                /*List<TravelingTransportPods> podsinrange = new List<TravelingTransportPods>();
				foreach (TravelingTransportPods obj in Find.WorldObjects.TravelingTransportPods)
				{
					float rng = (float)Traverse.Create(obj).Field("traveledPct").GetValue();
					if (obj.destinationTile == turret.Map.Parent.Tile && obj.Faction != mapComp.ShipFaction && rng > 0.75)
					{
						podsinrange.Add(obj);
					}
				}*/
                if (mapComp.TargetMapComp.TorpsInRange.Any() && Rand.Chance(0.1f))
                {
                    ShipCombatProjectile projtr = mapComp.TargetMapComp.TorpsInRange.RandomElement();
                    mapComp.TargetMapComp.Projectiles.Remove(projtr);
                    mapComp.TargetMapComp.TorpsInRange.Remove(projtr);
                }
                else if (mapComp.TargetMapComp.ShuttlesInRange.Where(shuttle => shuttle.Faction != turret.Faction).Any())
                {
                    VehiclePawn shuttleHit = mapComp.TargetMapComp.ShuttlesInRange.Where(shuttle => shuttle.Faction != turret.Faction).RandomElement();
                    int? targetIntellectualSkill = (shuttleHit.FindPawnWithBestStat(StatDefOf.ResearchSpeed)?.skills?.GetSkill(SkillDefOf.Intellectual)?.Level);
                    int skill = 0;
                    if (targetIntellectualSkill.HasValue)
                    {
                        skill = targetIntellectualSkill.Value;
                    }

                    if (verbProps.defaultProjectile.thingClass != typeof(Projectile_ExplosiveShipLaser) && Rand.Chance(0.75f))
                    {
                        Log.Message("Shuttle dodged non-laser weapon");
                    }
                    else if (Rand.Chance(1f - (shuttleHit.GetStatValue(ResourceBank.VehicleStatDefOf.SoS2CombatDodgeChance) / Mathf.Lerp(120, 80, skill / 20f))))
                    {
                        CompVehicleHeatNet heatNet = shuttleHit.GetComp<CompVehicleHeatNet>();
                        if (shuttleHit.GetComp<CompShipHeatShield>() != null && shuttleHit.statHandler.componentsByKeys["shieldGenerator"].health > 10 && heatNet != null && heatNet.myNet.StorageUsed < heatNet.myNet.StorageCapacity) //Shield takes the hit
                        {
                            Projectile dummyProjectile = (Projectile)ThingMaker.MakeThing(verbProps.defaultProjectile);
                            shuttleHit.GetComp<CompShipHeatShield>().HitShield(dummyProjectile);
                            Log.Message("Shuttle's shield took a hit! Its internal heatsinks are at " + heatNet.myNet.StorageUsed + " of " + heatNet.myNet.StorageCapacity + " capacity.");
                            if (!dummyProjectile.Destroyed)
                            {
                                dummyProjectile.Destroy();
                            }
                        }
                        else
                        {
                            shuttleHit.TakeDamage(new DamageInfo(verbProps.defaultProjectile.projectile.damageDef, verbProps.defaultProjectile.projectile.GetDamageAmount(caster)), IntVec2.Zero);
                            //Log.Message("Shuttle hit! It currently has " + shuttleHit.statHandler.GetStatValue(VehicleStatDefOf.BodyIntegrity) + " health.");
                            if (shuttleHit.statHandler.GetStatValue(VehicleStatDefOf.BodyIntegrity) <= 0)
                            {
                                if (shuttleHit.Faction == Faction.OfPlayer)
                                {
                                    Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CombatPodDestroyedPlayer"), null, MessageTypeDefOf.NegativeEvent);
                                }
                                else
                                {
                                    Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CombatPodDestroyedEnemy"), null, MessageTypeDefOf.PositiveEvent);
                                }

                                mapComp.TargetMapComp.DeRegisterShuttleMission(mapComp.TargetMapComp.ShuttleMissions.Where(mission => mission.shuttle == shuttleHit).First(), true);
                                foreach (Pawn pawn in shuttleHit.AllPawnsAboard.ListFullCopy())
                                {
                                    Log.Message("Pawn " + pawn + " is having a real bad day.");
                                    if (shuttleHit.Faction == Faction.OfPlayer && (ModSettings_SoS.easyMode || Rand.Chance(0.5f)))
                                    {
                                        HealthUtility.DamageUntilDowned(pawn, false);
                                        shuttleHit.RemovePawn(pawn);
                                        DropPodUtility.DropThingsNear(DropCellFinder.RandomDropSpot(mapComp.ShipCombatOriginMap), mapComp.OriginMapComp.map, new List<Thing> { pawn });
                                    }
                                    else
                                    {
                                        shuttleHit.RemovePawn(pawn);
                                        pawn.Kill(new DamageInfo(DamageDefOf.Bomb, 100f));
                                        if (shuttleHit.Faction == Faction.OfPlayer)
                                        {
                                            DropPodUtility.DropThingsNear(DropCellFinder.RandomDropSpot(mapComp.ShipCombatOriginMap), mapComp.OriginMapComp.map, new List<Thing> { pawn.Corpse });
                                        }
                                    }
                                }
                                /*foreach (Thing cargo in shuttleHit.GetDirectlyHeldThings())
									cargo.Kill();*/
                            }
                            else if (shuttleHit.statHandler.GetStatValue(VehicleStatDefOf.BodyIntegrity) <= ((CompShuttleLauncher)shuttleHit.CompVehicleLauncher).retreatAtHealth)
                            {
                                ShipMapComp.ShuttleMissionData missionData = mapComp.TargetMapComp.ShuttleMissions.Where(mission => mission.shuttle == shuttleHit).First();
                                if (missionData.mission != ShipMapComp.ShuttleMission.RETURN)
                                {
                                    if (shuttleHit.Faction == Faction.OfPlayer)
                                    {
                                        Messages.Message("SoS.ShuttleRetreat".Translate(), MessageTypeDefOf.NegativeEvent);
                                    }
                                    else
                                    {
                                        Messages.Message("SoS.EnemyShuttleRetreat".Translate(), MessageTypeDefOf.PositiveEvent);
                                    }
                                }
                                missionData.mission = ShipMapComp.ShuttleMission.RETURN;
                            }
                        }
                    }
                }
            }
        }
        public void RegisterProjectile(Building_ShipTurretCE turret, LocalTargetInfo target, ThingDef spawnProjectile, IntVec3 burstLoc)
        {
            var mapComp = caster.Map.GetComponent<ShipMapComp>();

            //inc acc if any manning pawn shooting or aicore
            int accBoost = 0;
            if (turret.heatComp.myNet.TacCons.Any(b => b.mannableComp.MannedNow))
            {
                accBoost = turret.heatComp.myNet.TacCons.Where(b => b.mannableComp.MannedNow).Max(b => b.mannableComp.ManningPawn.skills.GetSkill(SkillDefOf.Shooting).Level);
            }

            if (accBoost < 10 && turret.heatComp.myNet.AICores.Any())
            {
                accBoost = 10;
            }
            ShipCombatProjectile proj = new ShipCombatProjectile
            {
                turret = turret,
                target = target,
                range = 0,
                //rangeAtStart = mapComp.Range,
                spawnProjectile = spawnProjectile,
                missRadius = this.verbProps.ForcedMissRadius,
                accBoost = accBoost,
                burstLoc = burstLoc,
                speed = turret.heatComp.Props.projectileSpeed,
                Map = turret.Map
            };
            mapComp.Projectiles.Add(proj);
        }
        public LocalTargetInfo shipTarget;

        #endregion

        #region SharedLogic
        public override bool CanHitTarget(LocalTargetInfo targ)
        {
            return true;  
        }

        public override ThingDef Projectile
        {
            get
            {
                if (GroundDefenseMode) {
                    if (CompAmmo != null && CompAmmo.CurrentAmmo != null)
                    {
                        return CompAmmo.CurAmmoProjectile;
                    }
                    if (CompChangeable != null && CompChangeable.Loaded)
                    {
                        return CompChangeable.Projectile;
                    }
                    return VerbPropsShip.defaultProjectileGround ?? VerbPropsShip.defaultProjectile; // Changed to allow for different default projectile
                }
                if (base.EquipmentSource != null)
                {
                    SaveOurShip2.CompChangeableProjectile comp = base.EquipmentSource.GetComp<SaveOurShip2.CompChangeableProjectile>();
                    if (comp != null && comp.LoadedNotPrevent)
                    {
                        return comp.Projectile;
                    }
                }
                return this.verbProps.spawnDef;
            }
        }


        public override bool TryCastShot()
        {
            if (GroundDefenseMode)
            {
                Retarget();
                repeating = true;
                doRetarget = true;
                storedShotReduction = null;
                var props = VerbPropsCE;
                var midBurst = numShotsFired > 0;
                var suppressing = CompFireModes?.CurrentAimMode == AimMode.SuppressFire;

                // Cases
                // 1: Can hit target, set our previous shoot line
                //    Cannot hit target
                //      Target exists
                //        Mid burst
                // 2:       Interruptible -> stop shooting
                // 3:       Not interruptible -> continue shooting at last position (do *not* shoot at target position as it will play badly with skip or other teleport effects)
                // 4:     Suppressing fire -> set our shoot line and continue
                // 5:     else -> stop
                //      Target missing
                //        Mid burst
                // 6:       Interruptible -> stop shooting
                // 7:       Not interruptible -> shoot along previous line
                // 8:     else -> stop
                if (TryFindCEShootLineFromTo(caster.Position, currentTarget, out var shootLine)) // Case 1
                {
                    lastShootLine = shootLine;
                }
                else // We cannot hit the current target
                {
                    if (midBurst) // Case 2,3,6,7
                    {
                        if (props.interruptibleBurst && !suppressing) // Case 2, 6
                        {
                            return false;
                        }
                        // Case 3, 7
                        if (lastShootLine == null)
                        {
                            return false;
                        }
                        shootLine = (ShootLine)lastShootLine;
                        currentTarget = new LocalTargetInfo(lastTargetPos);
                    }
                    else // case 4,5,8
                    {
                        if (suppressing) // case 4,5
                        {
                            if (currentTarget.IsValid && !currentTarget.ThingDestroyed)
                            {
                                lastShootLine = shootLine = new ShootLine(caster.Position, currentTarget.Cell);
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                if (projectilePropsCE.pelletCount < 1)
                {
                    Log.Error(EquipmentSource.LabelCap + " tried firing with pelletCount less than 1.");
                    return false;
                }
                bool instant = false;

                float spreadDegrees = 0;
                float aperatureSize = 0;

                if (Projectile.projectile is ProjectilePropertiesCE pprop)
                {
                    instant = pprop.isInstant;
                    spreadDegrees = (EquipmentSource?.GetStatValue(CE_StatDefOf.ShotSpread) ?? 0) * pprop.spreadMult;
                    aperatureSize = 0.03f;
                }

                ShiftVecReport report = ShiftVecReportFor(currentTarget);
                bool pelletMechanicsOnly = false;
                for (int i = 0; i < projectilePropsCE.pelletCount; i++)
                {
                    ProjectileCE projectileCE;
                    if (instant)
                    {
                        projectileCE = (ProjectileCE)ThingMaker.MakeThing(Projectile, null);
                    } else
                    {
                        projectileCE = (ShipProjectileCE)ThingMaker.MakeThing(Projectile, null);
                    }
                    GenSpawn.Spawn(projectileCE, shootLine.Source, caster.Map);
                    ShiftTarget(report, pelletMechanicsOnly, instant, midBurst);

                    //New aiming algorithm
                    projectileCE.canTargetSelf = false;

                    var targetDistance = (sourceLoc - currentTarget.Cell.ToIntVec2.ToVector2Shifted()).magnitude;

                    projectileCE.minCollisionDistance = GetMinCollisionDistance(targetDistance);
                    projectileCE.intendedTarget = currentTarget;
                    projectileCE.mount = caster.Position.GetThingList(caster.Map).FirstOrDefault(t => t is Pawn && t != caster);
                    projectileCE.AccuracyFactor = report.accuracyFactor * report.swayDegrees * ((numShotsFired + 1) * 0.75f);

                    if (instant)
                    {
                        var shotHeight = ShotHeight;
                        float tsa = AdjustShotHeight(caster, currentTarget, ref shotHeight);
                        projectileCE.RayCast(
                            Shooter,
                            verbProps,
                            sourceLoc,
                            shotAngle + tsa,
                            shotRotation,
                            shotHeight,
                            ShotSpeed,
                            spreadDegrees,
                            aperatureSize,
                            EquipmentSource);

                    }
                    else
                    {
                        ShipProjectileCE shipProjectile = projectileCE as ShipProjectileCE;
                        if (shipProjectile == null)
                        {
                            Log.Error("Ship Projectile cast failed");
                            return false;
                        }
                        shipProjectile.Launch(
                            Shooter,    //Shooter instead of caster to give turret operators' records the damage/kills obtained
                            sourceLoc,
                            shotAngle,
                            shotRotation,
                            ShotHeight,
                            ShotSpeed,
                            EquipmentSource,
                            distance);
                    }
                    pelletMechanicsOnly = true;
                }

                /*
                 * Notify the lighting tracker that shots fired with muzzle flash value of VerbPropsCE.muzzleFlashScale
                 */
                LightingTracker.Notify_ShotsFiredAt(caster.Position, intensity: VerbPropsCE.muzzleFlashScale);
                pelletMechanicsOnly = false;
                numShotsFired++;
                if (ShooterPawn != null)
                {
                    if (CompAmmo != null && !CompAmmo.CanBeFiredNow)
                    {
                        CompAmmo?.TryStartReload();
                        resetRetarget();
                    }
                    if (CompReloadable != null)
                    {
                        CompReloadable.UsedOnce();
                    }
                }
                lastShotTick = Find.TickManager.TicksGame;
                return true;

            }
            ThingDef projectile = Projectile;
            if (projectile == null)
            {
                return true;
            }
            Building_ShipTurretCE turret = this.caster as Building_ShipTurretCE;
            if (turret != null)
            {
                if (turret.PointDefenseMode) //remove registered torps/pods in range
                {
                    PointDefense(turret);
                }
                else //register projectile on mapComp
                {
                    if (turret.torpComp == null)
                    {
                        RegisterProjectile(turret, this.shipTarget, verbProps.defaultProjectile, turret.SynchronizedBurstLocation);
                    }
                    else
                    {
                        RegisterProjectile(turret, this.shipTarget, turret.torpComp.Projectile.interactionCellIcon, turret.SynchronizedBurstLocation); //This is a horrible kludge, but it's a way to attach one projectile's ThingDef to another projectile
                    }
                }
            }
            ShootLine resultingLine = new ShootLine(caster.Position, currentTarget.Cell);
            Thing launcher = caster;
            Thing equipment = base.EquipmentSource;
            Vector3 drawPos = caster.DrawPos;
            if (equipment != null)
            {
                base.EquipmentSource.GetComp<SaveOurShip2.CompChangeableProjectile>()?.Notify_ProjectileLaunched();
            }
            Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, resultingLine.Source, caster.Map);

            if (launcher is Building_ShipTurretTorpedo l)
            {
                projectile2.Launch(launcher, drawPos + l.TorpedoTubePos(), currentTarget.Cell, currentTarget.Cell, ProjectileHitFlags.None, false, equipment);
            }
            else
            {
                projectile2.Launch(launcher, currentTarget.Cell, currentTarget.Cell, ProjectileHitFlags.None, false, equipment);
            }

            if (projectile == ResourceBank.ThingDefOf.Bullet_Fake_Laser || projectile == ResourceBank.ThingDefOf.Bullet_Ground_Laser || projectile == ResourceBank.ThingDefOf.Bullet_Fake_Psychic)
            {
                ShipCombatLaserMote obj = (ShipCombatLaserMote)(object)ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipCombatLaserMote);
                obj.origin = drawPos;
                obj.destination = currentTarget.Cell.ToVector3Shifted();
                obj.large = this.caster.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier) > 1.0f;
                obj.color = turret.heatComp.Props.laserColor;
                obj.Attach(launcher);
                GenSpawn.Spawn(obj, launcher.Position, launcher.Map, 0);
            }
            projectile2.HitFlags = ProjectileHitFlags.None;
            return true;
        }
        #endregion
    }
    public class ShipProjectileCE : ProjectileCE_Explosive
    {
        // Need to override the projectile drawing since we are setting it so high up and the SOS2 turret graphics are fully top down
        // so it looks cooked with the normal logic
        // I can't think of another easy way to alter the drawing for just the projectiles shot by Verb_ShootShip

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (FlightTicks == 0 && launcher != null && launcher is Pawn)
            {
                //TODO: Draw at the end of the barrel on the pawn
            }
            else
            {
                Quaternion shadowRotation = ExactRotation;
                Quaternion projectileRotation = ExactRotation;
                if (def.projectile.spinRate != 0f)
                {
                    float num2 = GenTicks.TicksPerRealSecond / def.projectile.spinRate;
                    var spinRotation = Quaternion.AngleAxis(Find.TickManager.TicksGame % num2 / num2 * 360f, Vector3.up);
                    shadowRotation *= spinRotation;
                    projectileRotation *= spinRotation;
                }
                //Projectile
                Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), ExactPosition, projectileRotation, def.DrawMatSingle, 0);

                //Shadow - Not going to bother drawing a shadow as we're essentially rendering as if we're looking directly down on the bullet
                // (Despite the rest of rimworld not being directly top down it just matches the turrets better)
                //if (castShadow)
                //{
                //    var shadowPos = new Vector3(ExactPosition.x, def.Altitude - 0.001f, ExactPosition.z);
                    
                //    Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), shadowPos, shadowRotation, ShadowMaterial, 0);
                //}

                Comps_PostDraw();
            }
        }
    }
}
