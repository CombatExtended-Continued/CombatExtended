﻿using System.Collections.Generic;
using System.Linq;
using CombatExtended.AI;
using CombatExtended.Utilities;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public static class SuppressionUtility
    {
        public const float maxCoverDist = 10f; //Maximum distance to run for cover to

        private static LightingTracker tracker;

        public static bool TryRequestHelp(Pawn pawn)
        {
            Map map = pawn.Map;
            float curLevel = pawn.TryGetComp<CompSuppressable>().CurrentSuppression;
            ThingWithComps grenade = null;
            foreach (Pawn other in pawn.Position.PawnsInRange(map, 8))
            {
                if (other.Faction != pawn.Faction)
                    continue;
                if (other.jobs?.curDriver is IJobDriver_Tactical)
                    continue;
                if (!(other.TryGetComp<CompInventory>()?.TryFindSmokeWeapon(out grenade) ?? false))
                    continue;
                CompSuppressable otherSup = other.TryGetComp<CompSuppressable>();
                if ((otherSup?.isSuppressed ?? true) || otherSup.CurrentSuppression > curLevel)
                    continue;
                if (!GenSight.LineOfSight(pawn.Position, other.Position, map))
                    continue;
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
            float distToSuppressor = (pawn.Position - comp.SuppressorLoc).LengthHorizontal;
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
            tracker = pawn.Map.GetLightingTracker();
            float bestRating = GetCellCoverRatingForPawn(pawn, pawn.Position, fromPosition);

            if (bestRating <= 0)
            {
                // Go through each cell in radius around the pawn
                Region pawnRegion = pawn.Position.GetRegion(pawn.Map);
                List<Region> adjacentRegions = pawnRegion.Neighbors.ToList();
                adjacentRegions.Add(pawnRegion);
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
            tracker = null;
            return bestRating >= 0;
        }

        private static float GetCellCoverRatingForPawn(Pawn pawn, IntVec3 cell, IntVec3 shooterPos)
        {
            // Check for invalid locations
            if (!cell.IsValid || !cell.Standable(pawn.Map) || !pawn.CanReserveAndReach(cell, PathEndMode.OnCell, Danger.Deadly) || cell.ContainsStaticFire(pawn.Map))
            {
                return -1;
            }

            float cellRating = 0;

            if (!GenSight.LineOfSight(shooterPos, cell, pawn.Map))
            {
                cellRating += 2f;
            }
            else
            {
                //Check if cell has cover in desired direction
                Vector3 coverVec = (shooterPos - cell).ToVector3().normalized;
                IntVec3 coverCell = (cell.ToVector3Shifted() + coverVec).ToIntVec3();
                Thing cover = coverCell.GetCover(pawn.Map);
                cellRating += GetCoverRating(cover);
            }

            //Check time to path to that location
            if (!pawn.Position.Equals(cell))
            {
                cellRating -= tracker.CombatGlowAtFor(shooterPos, cell) / 2f;
                // float pathCost = pawn.Map.pathFinder.FindPath(pawn.Position, cell, TraverseMode.NoPassClosedDoors).TotalCost;
                float pathCost = (pawn.Position - cell).LengthHorizontal;
                if (!GenSight.LineOfSight(pawn.Position, cell, pawn.Map))
                    pathCost *= 5;
                cellRating = cellRating / pathCost;
            }
            return cellRating;
        }

        private static float GetCoverRating(Thing cover)
        {
            if (cover == null) return 0;
            if (cover is Gas) return 0.8f;
            if (cover.def.category == ThingCategory.Plant) return cover.def.fillPercent; // Plant cover only has a random chance to block gunfire and is rated lower            
            return 1;
        }

        public static bool TryGetSmokeScreeningJob(Pawn pawn, IntVec3 suppressorLoc, out Job job)
        {
            job = null;
            ThingWithComps grenade = null;
            if (!pawn.TryGetComp<CompInventory>()?.TryFindSmokeWeapon(out grenade) ?? true)
                return false;
            var range = 5;
            var castTarget = pawn.Position;
            foreach (IntVec3 cell in GenSightCE.PartialLineOfSights(pawn, suppressorLoc))
            {
                if (cell.DistanceTo(pawn.Position) >= range)
                    break;
                castTarget = cell;
            }
            if (!castTarget.IsValid)
                castTarget = pawn.Position;
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
                || traits.DegreeOfTrait(TraitDefOf.Nerves) > 0
                || traits.DegreeOfTrait((CE_TraitDefOf.Bravery)) > 1))
            {
                breaks.Add(pawn.IsColonist ? MentalStateDefOf.Wander_OwnRoom : MentalStateDefOf.PanicFlee);
                breaks.Add(CE_MentalStateDefOf.ShellShock);
            }

            // Attack break
            if (!(pawn.WorkTagIsDisabled(WorkTags.Violent)
                  || traits.DegreeOfTrait(TraitDefOf.Nerves) < 0
                  || traits.DegreeOfTrait(CE_TraitDefOf.Bravery) < 0))
            {
                breaks.Add(CE_MentalStateDefOf.CombatFrenzy);
            }

            return breaks;
        }
    }
}
