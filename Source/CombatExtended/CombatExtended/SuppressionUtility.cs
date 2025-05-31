using System.Collections.Generic;
using System.Linq;
using CombatExtended.AI;
using CombatExtended.Compatibility;
using CombatExtended.Utilities;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public static class SuppressionUtility
    {
        private const float maxCoverDist = 10f; // Maximum distance to run for cover to;

        public const float linearDangerAmountFactor = 0.2f; // The coefficient for linear danger amount when calculating cell cover attractiveness
        public const float squaredDangerAmountFactor = 0.01f; // The coefficient for squared danger amount when calculating cell cover attractiveness
        private const float pathCostFactor = 2f; // How important is the distance to travel
        private const float obstaclePathCostFactor = 2f; // How important is travelling over a path that has mantling or doors involved
        private const float distanceFromThreatFactor = 0.5f; // How important is the distance change from the new cell to the shooter position versus the current cell to the shooter position

        private static LightingTracker lightingTracker;

        private static DangerTracker dangerTracker;

        public static bool TryRequestHelp(Pawn pawn)
        {
            //TODO: 1.5
            if (pawn != null)
            {
                return false;
            }
            Map map = pawn.Map;
            float curLevel = pawn.TryGetComp<CompSuppressable>().CurrentSuppression;
            ThingWithComps grenade = null;
            foreach (Pawn other in pawn.Position.PawnsInRange(map, 8))
            {
                if (other.Faction != pawn.Faction)
                {
                    continue;
                }
                if (other.jobs?.curDriver is IJobDriver_Tactical)
                {
                    continue;
                }
                if (!(other.TryGetComp<CompInventory>()?.TryFindSmokeWeapon(out grenade) ?? false))
                {
                    continue;
                }
                CompSuppressable otherSup = other.TryGetComp<CompSuppressable>();
                if ((otherSup?.isSuppressed ?? true) || otherSup.CurrentSuppression > curLevel)
                {
                    continue;
                }
                if (!GenSight.LineOfSight(pawn.Position, other.Position, map))
                {
                    continue;
                }
                Job job = JobMaker.MakeJob(CE_JobDefOf.OpportunisticAttack, grenade, pawn.Position);
                job.maxNumStaticAttacks = 1;
                other.jobs.StartJob(job, JobCondition.InterruptForced);
                return true;
            }
            return false;
        }

        public static Job GetRunForCoverJob(Pawn pawn)
        {
            //Calculate cover position
            CompSuppressable comp = pawn.TryGetComp<CompSuppressable>();
            if (comp == null)
            {
                return null;
            }
            IntVec3 coverPosition;
            //Try to find cover position to move up to
            if (!GetCoverPositionFrom(pawn, comp.SuppressorLoc, maxCoverDist, out coverPosition))
            {
                return null;
            }
            //Sanity check
            if (pawn.Position.Equals(coverPosition))
            {
                return null;
            }
            //Tell pawn to move to position
            var job = JobMaker.MakeJob(CE_JobDefOf.RunForCover, coverPosition);
            job.locomotionUrgency = LocomotionUrgency.Sprint;
            job.playerForced = true;
            return job;
        }

        private static bool GetCoverPositionFrom(Pawn pawn, IntVec3 fromPosition, float maxDist, out IntVec3 coverPosition)
        {
            List<IntVec3> cellList = new List<IntVec3>(GenRadial.RadialCellsAround(pawn.Position, maxDist, true));
            IntVec3 bestPos = pawn.Position;
            lightingTracker = pawn.Map.GetLightingTracker();
            dangerTracker = pawn.Map.GetDangerTracker();

            float bestRating = GetCellCoverRatingForPawn(pawn, pawn.Position, fromPosition);
            if (bestRating <= 0)
            {
                // Go through each cell in radius around the pawn
                Region pawnRegion = pawn.Position.GetRegion(pawn.Map);
                List<Region> adjacentRegions = pawnRegion.Neighbors.ToList();
                adjacentRegions.Add(pawnRegion);
                List<Region> tempRegions = adjacentRegions.ListFullCopy();
                foreach (Region region in tempRegions)
                {
                    adjacentRegions.AddRange(region.Neighbors.Except(adjacentRegions));
                }
                // Make sure only cells within bounds are evaluated
                foreach (IntVec3 cell in cellList.Where(x => x.InBounds(pawn.Map)))
                {
                    // Check for adjacency so we don't path to the other side of a wall or some such
                    if (cell.InBounds(pawn.Map) && adjacentRegions.Contains(cell.GetRegion(pawn.Map)))
                    {
                        float cellRating = GetCellCoverRatingForPawn(pawn, cell, fromPosition);
                        if (cellRating > bestRating)
                        {
                            bestRating = cellRating;
                            bestPos = cell;
                        }
                    }
                }
            }
            coverPosition = bestPos;
            //Log.Warning("best cell at " + bestPos.ToString() + ' ' + bestRating.ToString());
            lightingTracker = null;
            return bestRating >= -999f && pawn.Position != coverPosition;
        }

        private static float GetCellCoverRatingForPawn(Pawn pawn, IntVec3 cell, IntVec3 shooterPos)
        {
            // Check for invalid locations
            if (!cell.IsValid || !cell.Standable(pawn.Map) || !pawn.CanReserveAndReach(cell, PathEndMode.OnCell, Danger.Deadly) || cell.ContainsStaticFire(pawn.Map))
            {
                return -1000f;
            }

            //Ignore cells that aren't directly reachable
            foreach (var pathCell in GenSight.PointsOnLineOfSight(pawn.Position, cell))
            {
                if (!pathCell.Walkable(pawn.Map))
                {
                    return -1000f;
                }
            }

            float cellRating = 0f, bonusCellRating = 1f,
                  pawnHeightFactor = CE_Utility.GetCollisionBodyFactors(pawn).y,
                  pawnVisibleOverCoverFillPercent = pawnHeightFactor * (1f - CollisionVertical.BodyRegionMiddleHeight) + 0.01f,
                  pawnLowestCrouchFillPercent = pawnHeightFactor * CollisionVertical.BodyRegionBottomHeight + pawnVisibleOverCoverFillPercent,
                  pawnCrouchFillPercent = pawnLowestCrouchFillPercent;

            Vector3 coverVec = (shooterPos - cell).ToVector3().normalized;

            // Line of sight is extremely important;
            if (!GenSight.LineOfSight(shooterPos, cell, pawn.Map))
            {
                cellRating = 20f;
            }
            else
            {
                // Check if cell has cover in desired direction;
                IntVec3 coverCell = (cell.ToVector3Shifted() + coverVec).ToIntVec3();
                Thing cover = coverCell.GetCover(pawn.Map);
                // Recheck with priority given to diagonal cover;
                if (cover == null || cover is Plant || cover is Gas)
                {
                    coverCell = (cell.ToVector3Shifted() + coverVec.ToVec3Gridified()).ToIntVec3();
                    cover = coverCell.GetCover(pawn.Map);
                }
                // Only account for solid cover;
                if (cover != null)
                {
                    // Landmines would never be used for cover
                    if (cover is Building_TrapExplosive)
                    {
                        return -1000f;
                    }
                    if (!(cover is Plant || cover is Gas))
                    {
                        pawnCrouchFillPercent = Mathf.Clamp(cover.def.fillPercent + pawnVisibleOverCoverFillPercent, pawnLowestCrouchFillPercent, pawnHeightFactor);
                        var coverRating = 1f - ((pawnCrouchFillPercent - cover.def.fillPercent) / pawnHeightFactor);
                        cellRating = Mathf.Min(coverRating, 1f) * 10f;
                    }
                }
            }

            foreach (IntVec3 pos in pawn.Map.PartialLineOfSights(cell, shooterPos))
            {
                Thing cover = pos.GetCover(pawn.Map);
                if (cover == null)
                {
                    continue;
                }
                // Check if the pawn already has hard cover and the cover currently is hard cover; then do custom formula for increasing cell rating;
                if (!(cover is Gas || cover is Plant))
                {
                    // A pawn crouches down only next to nearby cover, therefore they can hide behind distanced cover that is higher than their current crouch height;
                    var distancedCoverRating = cover.def.fillPercent / pawnCrouchFillPercent;
                    if (distancedCoverRating * 10f > cellRating)
                    {
                        cellRating = Mathf.Min(distancedCoverRating, 1f) * 10f;
                    }
                }
                else
                {
                    bonusCellRating *= 1f - GetCoverRating(cover);
                }
            }

            cellRating += 10f - (bonusCellRating * 10f);

            // If the cell is covered by a shield and there are no enemies inside, then increases by 15 (for each such shield)
            cellRating += CalculateShieldRating(cell, pawn);

            // Avoid bullets and other danger sources;
            // Yet do not discard cover that is extremely good, even if it may be dangerous
            float dangerAmount = dangerTracker.DangerAt(cell) * DangerTracker.DANGER_TICKS_MAX;
            cellRating -= (dangerAmount * linearDangerAmountFactor + dangerAmount * dangerAmount * squaredDangerAmountFactor) / (cellRating + 1);

            //Check time to path to that location
            if (!pawn.Position.Equals(cell))
            {
                cellRating -= lightingTracker.CombatGlowAtFor(shooterPos, cell) * 5f;
                //float pathCost = pawn.Map.pathFinder.FindPath(pawn.Position, cell, TraverseMode.PassDoors).TotalCost;
                float pathCost = (pawn.Position - cell).LengthHorizontal;
                // Reduce the chances of mantling over cover when running from danger
                foreach (var pathCell in GenSight.PointsOnLineOfSight(pawn.Position, cell))
                {
                    if (!pathCell.Standable(pawn.Map) || pathCell.GetDoor(pawn.Map) != null)
                    {
                        pathCost *= obstaclePathCostFactor;
                        break;
                    }
                }

                cellRating -= pathCost * pathCostFactor;

                // Moving away from the threat is preferred.
                cellRating += ((cell - shooterPos).LengthHorizontal - (pawn.Position - shooterPos).LengthHorizontal) * distanceFromThreatFactor;
            }


            if (Controller.settings.DebugDisplayCellCoverRating)
            {
                pawn.Map.debugDrawer.FlashCell(cell, cellRating, $"{cellRating}");
            }
            return cellRating;
        }

        /// <summary>
        /// Calculate the additional cover rating from shields covering the given cell.
        /// </summary>
        /// <param name="cell">The cell to compute the cover rating for.</param>
        /// <param name="pawn">The pawn seeking cover.</param>
        /// <returns>The computed cover rating (15 for each shield covering the cell).</returns>
        private static int CalculateShieldRating(IntVec3 cell, Pawn pawn)
        {
            int rating = 0;
            foreach (var zone in InterceptorZonesFor(pawn))
            {
                foreach (var zoneCell in zone)
                {
                    if (zoneCell == cell)
                    {
                        if (!IsOccupiedByEnemies(zone, pawn))
                        {
                            rating += 15;
                        }

                        break;
                    }
                }
            }

            return rating;
        }

        /// <summary>
        /// Get areas covered by a shield that may be suitable for protecting the given pawn.
        /// </summary>
        /// <param name="pawn">The pawn seeking cover.</param>
        /// <returns>An enumerator of areas covered by shields on the map that may protect the pawn.</returns>
        public static IEnumerable<IEnumerable<IntVec3>> InterceptorZonesFor(Pawn pawn)
        {
            foreach (var interceptor in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor))
            {
                var comp = interceptor.TryGetComp<CompProjectileInterceptor>();
                if (comp.Active && (comp.Props.interceptNonHostileProjectiles || !interceptor.HostileTo(pawn)))
                {
                    yield return GenRadial.RadialCellsAround(interceptor.Position, comp.Props.radius, true);
                }
            }

            foreach (var zone in BlockerRegistry.ShieldZonesCallback(pawn))
            {
                yield return zone;
            }
        }

        /// <summary>
        /// Check whether the given area contains any objects hostile to the given pawn.
        /// </summary>
        /// <param name="cells">The area to scan for hostile objects.</param>
        /// <param name="pawn">The pawn.</param>
        /// <returns>true if the area contained any hostile objects, false otherwise.</returns>
        private static bool IsOccupiedByEnemies(IEnumerable<IntVec3> cells, Pawn pawn)
        {
            foreach (var cell in cells)
            {
                var things = pawn.Map.thingGrid.ThingsListAt(cell);
                foreach (var thing in things)
                {
                    if (thing.HostileTo(pawn))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        private static float GetCoverRating(Thing cover)
        {
            // Higher values mean more effective at being considered cover.
            if (cover == null)
            {
                return 0f;
            }
            if (cover is Gas)
            {
                return 0.2f;
            }
            // Plant cover has a random chance to block projectiles and span a long height, therefore efficiency stacks
            if (cover.def.category == ThingCategory.Plant)
            {
                return 0.45f * cover.def.fillPercent;
            }
            return 0f;
        }

        public static bool TryGetSmokeScreeningJob(Pawn pawn, IntVec3 suppressorLoc, out Job job)
        {
            job = null;
            ThingWithComps grenade = null;
            if (!pawn.TryGetComp<CompInventory>()?.TryFindSmokeWeapon(out grenade) ?? true)
            {
                return false;
            }
            var range = 5;
            var castTarget = pawn.Position;
            foreach (IntVec3 cell in GenSightCE.PartialLineOfSights(pawn, suppressorLoc))
            {
                if (cell.DistanceTo(pawn.Position) >= range)
                {
                    break;
                }
                castTarget = cell;
            }
            if (!castTarget.IsValid)
            {
                castTarget = pawn.Position;
            }
            job = JobMaker.MakeJob(CE_JobDefOf.OpportunisticAttack, grenade, castTarget);
            job.maxNumStaticAttacks = 1;
            return true;
        }

        public static List<MentalStateDef> GetPossibleBreaks(Pawn pawn)
        {
            var breaks = new List<MentalStateDef>();
            var traits = pawn.story.traits;

            // Panic break
            if (!(traits.HasTrait(TraitDefOf.Bloodlust)
                    || traits.DegreeOfTrait((CE_TraitDefOf.Bravery)) > 1))
            {
                breaks.Add(pawn.IsColonist ? MentalStateDefOf.Wander_OwnRoom : MentalStateDefOf.PanicFlee);
                breaks.Add(CE_MentalStateDefOf.ShellShock);
            }

            // Attack break
            if (!(pawn.WorkTagIsDisabled(WorkTags.Violent)
                    || traits.DegreeOfTrait(CE_TraitDefOf.Bravery) < 0))
            {
                breaks.Add(CE_MentalStateDefOf.CombatFrenzy);
            }

            return breaks;
        }
    }
}
