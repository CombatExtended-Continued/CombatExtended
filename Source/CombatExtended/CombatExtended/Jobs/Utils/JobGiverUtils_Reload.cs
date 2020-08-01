using CombatExtended.CombatExtended.LoggerUtils;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;

namespace CombatExtended.CombatExtended.Jobs.Utils
{
	class JobGiverUtils_Reload
	{
		/// <summary>
		/// The maximum allowed pathing cost to reach potential ammo. 2 ingame hours.
		/// This is arbitrarily set. If you think this is too high or too low, feel free to change.
		/// </summary>
		private const float MaxPathCost = 2f * 60f * GenDate.TicksPerHour;
		/// <summary>
		/// Magic number. I took it from the now deprecated GenClosestAmmo class. Not sure why we would want 10 reservations, but there it is.
		/// </summary>
		private const int MagicMaxPawns = 10;

		public static Job MakeReloadJob(Pawn pawn, Building_TurretGunCE turret)
		{
			var compAmmo = turret.CompAmmo;
			if (compAmmo == null)
			{
				CELogger.Error($"{pawn} tried to create a reload job on a thing ({turret}) that's not reloadable.");
				return null;
			}

			if (!compAmmo.UseAmmo)
			{
				return MakeReloadJobNoAmmo(turret);
			}

			var ammo = FindBestAmmo(pawn, turret);
			if (ammo == null)
			{
				CELogger.Error($"{pawn} tried to create a reload job without ammo. This should have been checked earlier.");
				return null;
			}
			CELogger.Message($"Making a reload job for {pawn}, {turret} and {ammo}");

			Job job = JobMaker.MakeJob(CE_JobDefOf.ReloadTurret, turret, ammo);
			job.count = Mathf.Min(ammo.stackCount, turret.CompAmmo.MissingToFullMagazine);
			return job;
		}

		private static Job MakeReloadJobNoAmmo(Building_TurretGunCE turret)
		{
			var compAmmo = turret.CompAmmo;
			if (compAmmo == null)
			{
				CELogger.Error("Tried to create a reload job on a thing that's not reloadable.");
				return null;
			}

			return JobMaker.MakeJob(CE_JobDefOf.ReloadTurret, turret, null);
		}

		public static bool CanReload(Pawn pawn, Thing hopefullyTurret, bool forced = false, bool emergency = false)
		{
			if (pawn == null || hopefullyTurret == null)
			{
				CELogger.Warn($"{pawn?.ToString() ?? "null pawn"} could not reload {hopefullyTurret?.ToString() ?? "null thing"} one of the two was null.");
				return false;
			}
			if (!(hopefullyTurret is Building_TurretGunCE))
			{
				CELogger.Warn($"{pawn} could not reload {hopefullyTurret} because {hopefullyTurret} is not a Combat Extended Turret. If you are a modder, make sure to use {nameof(CombatExtended)}.{nameof(Building_TurretGunCE)} for your turret's compClass.");
				return false;
			}
			var turret = hopefullyTurret as Building_TurretGunCE;
			var compAmmo = turret.CompAmmo;

			if (compAmmo == null)
			{
				CELogger.Warn($"{pawn} could not reload {turret} because turret has no {nameof(CompAmmoUser)}.");
				return false;
			}
			if (turret.isReloading)
			{
				CELogger.Message($"{pawn} could not reload {turret} because turret is already reloading.");
				JobFailReason.Is("CE_TurretAlreadyReloading".Translate());
				return false;
			}
			if (turret.IsBurning() && !emergency)
			{
				CELogger.Message($"{pawn} could not reload {turret} because turret is on fire.");
				JobFailReason.Is("CE_TurretIsBurning".Translate());
				return false;
			}
			if (compAmmo.FullMagazine)
			{
				CELogger.Message($"{pawn} could not reload {turret} because it is full of ammo.");
				JobFailReason.Is("CE_TurretFull".Translate());
				return false;
			}
			if (turret.IsForbidden(pawn) || !pawn.CanReserve(turret, 1, -1, null, forced))
			{
				CELogger.Message($"{pawn} could not reload {turret} because it is forbidden or otherwise busy.");
				return false;
			}
			if (turret.Faction != pawn.Faction && (turret.Faction != null && pawn.Faction != null && turret.Faction.RelationKindWith(pawn.Faction) != FactionRelationKind.Ally))
			{
				CELogger.Message($"{pawn} could not reload {turret} because the turret is hostile to them.");
				JobFailReason.Is("CE_TurretNonAllied".Translate());
				return false;
			}
			if ((turret.MannableComp?.ManningPawn != pawn) && !pawn.CanReserveAndReach(turret, PathEndMode.ClosestTouch, forced ? Danger.Deadly : pawn.NormalMaxDanger(), MagicMaxPawns))
			{
				CELogger.Message($"{pawn} could not reload {turret} because turret is manned (or was recently manned) by someone else.");
				return false;
			}
			if (compAmmo.UseAmmo && FindBestAmmo(pawn, turret) == null)
			{
				JobFailReason.Is("CE_NoAmmoAvailable".Translate());
				return false;
			}
			return true;
		}

		private static Thing FindBestAmmo(Pawn pawn, Building_TurretGunCE reloadable)
		{
			//ThingFilter filter = refuelable.TryGetComp<CompRefuelable>().Props.fuelFilter;
			AmmoDef requestedAmmo = reloadable.CompAmmo.SelectedAmmo;
			// try to find currently selected ammo first
			var bestAmmo = FindBestAmmo(pawn, requestedAmmo);
			// this code is mostly for siege raids, so they can use all the ammo dropped (and get more ammo)
			// otherwise, they will wait forever for an HE ammo drop, without using the incendiary shells next to them
			if (bestAmmo == null && !pawn.IsColonist && requestedAmmo.AmmoSetDefs != null)
			{
				// if there isn't any, try to find some ammo from same ammo set
				foreach (AmmoSetDef set in requestedAmmo.AmmoSetDefs)
				{
					foreach (AmmoLink link in set.ammoTypes)
					{
						bestAmmo = FindBestAmmo(pawn, link.ammo);
						if (bestAmmo != null) return bestAmmo;
					}
				}
			}
			return bestAmmo;
		}

		private static Thing FindBestAmmo(Pawn pawn, AmmoDef requestedAmmo)
        {
			Predicate<Thing> validator = (Thing potentialAmmo) =>
			{
				if (potentialAmmo.IsForbidden(pawn) || !pawn.CanReserve(potentialAmmo))
				{
					return false;
				}
				return GetPathCost(pawn, potentialAmmo) <= MaxPathCost;
			};

			return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(requestedAmmo), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, validator);
		}

		/// <summary>
		/// This method is a direct copy/paste of the <see cref="RefuelWorkGiverUtility"/> private FindAllFuel method.
		/// 
		/// Finds all relevant ammo in order of distance.
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="reloadable"></param>
		/// <returns></returns>
		private static List<Thing> FindAllAmmo(Pawn pawn, Building_TurretGunCE reloadable)
		{
			int quantity = reloadable.CompAmmo.MissingToFullMagazine;
			var ammoKind = reloadable.CompAmmo.SelectedAmmo;
			Predicate<Thing> validator = (Thing potentialAmmo) =>
			{
				if (potentialAmmo.IsForbidden(pawn) || !pawn.CanReserve(potentialAmmo))
				{
					return false;
				}
				return GetPathCost(pawn, potentialAmmo) <= MaxPathCost;
			};
			Region region = reloadable.Position.GetRegion(pawn.Map);
			TraverseParms traverseParams = TraverseParms.For(pawn);
			Verse.RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParams, isDestination: false);
			var chosenThings = new List<Thing>();
			int accumulatedQuantity = 0;
			Verse.RegionProcessor regionProcessor = (Region r) =>
			{
				List<Thing> list = r.ListerThings.ThingsMatching(ThingRequest.ForDef(ammoKind));
				foreach (var thing in list)
				{
					if (validator(thing) && !chosenThings.Contains(thing) && ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, r, PathEndMode.ClosestTouch, pawn))
					{
						chosenThings.Add(thing);
						accumulatedQuantity += thing.stackCount;
						if (accumulatedQuantity >= quantity)
						{
							return true;
						}
					}
				}
				return false;
			};
			RegionTraverser.BreadthFirstTraverse(region, entryCondition, regionProcessor, 99999);
			if (accumulatedQuantity >= quantity)
			{
				return chosenThings;
			}
			return null;
		}

		private static float GetPathCost(Pawn pawn, Thing thing)
		{
			var cell = thing.Position;
			var pos = pawn.Position;
			var traverseParams = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassDoors, false);
			
			using (PawnPath path = pawn.Map.pathFinder.FindPath(pos, cell, traverseParams, PathEndMode.Touch)) {
				return path.TotalCost;
			}
		}
	}

}
