using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using CombatExtended.CombatExtended.LoggerUtils;
using CombatExtended.CombatExtended.Jobs.Utils;

namespace CombatExtended
{
    public class WorkGiver_ReloadTurret : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override float GetPriority(Pawn pawn, TargetInfo t) => GetThingPriority(pawn, t.Thing);

        private float GetThingPriority(Pawn pawn, Thing t, bool forced = false)
        {
            CELogger.Message($"pawn: {pawn}. t: {t}. forced: {forced}");
            Building_TurretGunCE turret = t as Building_TurretGunCE;

            if (!turret.Active) return 1f;
            if (turret.EmptyMagazine) return 9f;
            if (turret.AutoReloadableMagazine) return 5f;
            return 1f;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (forced)
            {
                CELogger.Message("Job is forced. Not skipping.");
                return false;
            }
            if (pawn.CurJob == null)
            {
                CELogger.Message($"Pawn {pawn.ThingID} has no job. Not skipping.");
                return false;
            }
            if (pawn.CurJobDef == JobDefOf.ManTurret)
            {
                CELogger.Message($"Pawn {pawn.ThingID}'s current job is {nameof(JobDefOf.ManTurret)}. Not skipping.");
                return false;
            }
            if (pawn.CurJob.playerForced)
            {
                CELogger.Message($"Pawn {pawn.ThingID}'s current job is forced by the player. Skipping.");
                return true;
            }
            CELogger.Message($"Pawn {pawn.ThingID}'s current job is {pawn.CurJobDef.reportString}. Skipping");
            return (pawn.CurJobDef == CE_JobDefOf.ReloadTurret || pawn.CurJobDef == CE_JobDefOf.ReloadWeapon);
        }

        /// <summary>
        /// Called as a scanner
        /// </summary>
        /// <param name="pawn">Never null</param>
        /// <param name="t">Can be non-turret</param>
        /// <param name="forced">Generally not true</param>
        /// <returns></returns>
        
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var priority = GetThingPriority(pawn, t, forced);
            CELogger.Message($"Priority check completed. Got {priority}");

            Building_TurretGunCE turret = t as Building_TurretGunCE;
            CELogger.Message($"Turret uses ammo? {turret.CompAmmo.UseAmmo}");
            if (!turret.CompAmmo.UseAmmo)
                return true;

            CELogger.Message($"Total magazine size: {turret.CompAmmo.Props.magazineSize}. Needed: {turret.CompAmmo.MissingToFullMagazine}");

            return JobGiverUtils_Reload.CanReload(pawn, turret, forced);
        }

        //Checks before called (ONLY when in SCANNER):
        // - PawnCanUseWorkGiver(pawn, this)
        //      - nonColonistCanDo || isColonist
        //      - !WorkTagIsDisabled(def.workTags)
        //      - !ShouldSkip(pawn, false)
        //      - MissingRequiredCapacity(pawn) == null
        // - !t.IsForbidden(pawn)
        // - this.PotentialWorkThingRequest.Accepts(t), 
        /// <summary>
        /// Called after HasJobOnThing by WorkGiver_Scanner, or by Building_TurretGunCE when turret tryReload with manningPawn
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="t"></param>
        /// <param name="forced"></param>
        /// <returns></returns>
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            //Do not check for NeedsReload etc. -- if forced, treat as if NeedsReload && AllowAutomaticReload

            Building_TurretGunCE turret = t as Building_TurretGunCE;
            if (!turret.CompAmmo.UseAmmo)
                return JobGiverUtils_Reload.MakeReloadJobNoAmmo(turret);

            // NOTE: The removal of the code that used to be here disables reloading turrets directly from one's inventory.
            // The player would need to drop the ammo the pawn is holding first.

            return JobGiverUtils_Reload.MakeReloadJob(pawn, turret);
        }

        private Thing PawnClosestAmmo(Pawn pawn, Building_TurretGunCE turret, out bool fromInventory, bool forced = false)
        {
            var testAmmo = turret.nearestViableAmmo;

            int amount = turret.CompAmmo.Props.magazineSize;
            if (turret.CompAmmo.CurrentAmmo == turret.CompAmmo.SelectedAmmo) amount -= turret.CompAmmo.CurMagCount;

            if (testAmmo == null
                || testAmmo.IsForbidden(pawn)
                || !pawn.CanReserveAndReach(new LocalTargetInfo(testAmmo), PathEndMode.ClosestTouch, forced ? Danger.Deadly : pawn.NormalMaxDanger(),
                    Mathf.Max(1, testAmmo.stackCount - amount), Mathf.Min(testAmmo.stackCount, amount), null, forced))
            {
                testAmmo = turret.InventoryAmmo(pawn?.TryGetComp<CompInventory>());
                fromInventory = true;
            }
            else
                fromInventory = false;

            return testAmmo;
        }

        public Pawn BestNonJobUser(IEnumerable<Pawn> pawns, Building_TurretGunCE turret, out bool fromInventory, bool forced = false)
        {
            fromInventory = false;

            if (!pawns.Any()) return null;

            //Cut a significant portion of pawns
            pawns = pawns.Where(p => !ShouldSkip(p) && !p.Downed && !p.Dead && p.Spawned && (p.mindState?.Active ?? true)
                && (!p.mindState?.mentalStateHandler?.InMentalState ?? true) && !turret.IsForbidden(p)
                && (turret.Faction == p.Faction || turret.Faction?.RelationKindWith(p.Faction) == FactionRelationKind.Ally)
                && p.CanReserveAndReach(new LocalTargetInfo(turret), PathEndMode.InteractionCell, forced ? Danger.Deadly : p.NormalMaxDanger(), GenClosestAmmo.pawnsPerTurret, -1, null, forced));

            //No pawns remaining => quit
            if (!pawns.Any())
                return null;

            //If no ammo is used, minimize distance
            if (!turret.CompAmmo?.UseAmmo ?? true)
                return pawns.MinBy(x => x.Position.DistanceTo(turret.Position));

            //ELSE: ammo is used, pawns remain
            turret.UpdateNearbyAmmo(forced);
            var hasNearbyAmmo = turret.nearestViableAmmo != null;

            int amount = turret.CompAmmo.Props.magazineSize;
            if (turret.CompAmmo.CurrentAmmo == turret.CompAmmo.SelectedAmmo) amount -= turret.CompAmmo.CurMagCount;

            if (hasNearbyAmmo)
                pawns = pawns.OrderBy(x => x.Position.DistanceTo(turret.Position));

            var bestNonInventoryDistance = 9999f;
            Pawn bestPawn = null;
            var minimumDistance = hasNearbyAmmo ? turret.Position.DistanceTo(turret.nearestViableAmmo.Position) : 0f;

            //Sort through ASCENDING DISTANCE TO TURRET -- e.g, inventory ammo is preferred
            foreach (var p in pawns)
            {
                if (PawnClosestAmmo(p, turret, out fromInventory, forced) == null)
                    continue;

                if (fromInventory)
                {
                    if (p.Position.DistanceTo(turret.Position) < bestNonInventoryDistance + minimumDistance)
                        return p;
                }
                else if (hasNearbyAmmo)
                {
                    var dist = p.Position.DistanceTo(turret.nearestViableAmmo.Position);

                    if (dist < bestNonInventoryDistance)
                    {
                        bestNonInventoryDistance = dist;
                        bestPawn = p;
                    }
                }
            }

            fromInventory = false;
            return bestPawn;
        }
    }
}