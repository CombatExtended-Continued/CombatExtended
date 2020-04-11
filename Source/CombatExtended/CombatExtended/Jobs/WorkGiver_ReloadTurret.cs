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

        private bool Checks(Pawn pawn, Building_TurretGunCE turret, bool forced)
        {
            //WorkTagIsDisabled check inherent to WorkGiver_Scanner
            //MissingRequiredCapacity check inherent to WorkGiver_Scanner
            if (turret.isReloading)
            {
                CELogger.Message(pawn.ThingID + " failed " + turret.ThingID + ": turret is already reloading");
                return false;  //Turret is already reloading
            }
            if (turret.FullMagazine)
            {
                CELogger.Message(pawn.ThingID + " failed " + turret.ThingID + ": turret doesn't need reload");
                return false;  //Turret doesn't need reload
            }
            if (turret.IsBurning())
            {
                CELogger.Message(pawn.ThingID + " failed " + turret.ThingID + ": turret on fire");
                return false;   //Turret on fire
            }
            if (turret.Faction != pawn.Faction && (turret.Faction != null && pawn.Faction != null && turret.Faction.RelationKindWith(pawn.Faction) != FactionRelationKind.Ally))
            {
                CELogger.Message(pawn.ThingID + " failed " + turret.ThingID + ": non-allied turret");
                return false;     //Allies reload turrets
            }
            if ((turret.MannableComp?.ManningPawn != pawn) && !pawn.CanReserveAndReach(turret, PathEndMode.ClosestTouch, forced ? Danger.Deadly : pawn.NormalMaxDanger(), GenClosestAmmo.pawnsPerTurret))
            {
                CELogger.Message(pawn.ThingID + " failed " + turret.ThingID + ": turret unreachable");
                return false;    //Pawn cannot reach turret
            }

            return true;
        }

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
        /*public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
          {
              Building_TurretGunCE turret = t as Building_TurretGunCE;
              if (turret == null || !Checks(pawn, turret, forced))  return false;

              if (!turret.CompAmmo.UseAmmo)
                  return true;

              turret.UpdateNearbyAmmo();

              Thing ammo = turret.nearestViableAmmo;

              int amountNeeded = turret.CompAmmo.Props.magazineSize;
              if (turret.CompAmmo.CurrentAmmo == turret.CompAmmo.SelectedAmmo) amountNeeded -= turret.CompAmmo.CurMagCount;

              if (ammo == null || ammo.IsForbidden(pawn) || !pawn.CanReserveAndReach(new LocalTargetInfo(ammo), PathEndMode.ClosestTouch, pawn.NormalMaxDanger(), Mathf.Max(1, ammo.stackCount - amountNeeded), amountNeeded, null, forced))
              {
                  ammo = turret.InventoryAmmo(pawn.TryGetComp<CompInventory>());
              }

              return ammo != null;
          }*/

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var priority = GetThingPriority(pawn, t, forced);
            if (priority == 0f) return false;

            Building_TurretGunCE turret = t as Building_TurretGunCE;
            if (!turret.CompAmmo.UseAmmo)
                return true;

            int amountNeeded = turret.CompAmmo.Props.magazineSize;
            if (turret.CompAmmo.CurrentAmmo == turret.CompAmmo.SelectedAmmo) amountNeeded -= turret.CompAmmo.CurMagCount;

            turret.UpdateNearbyAmmo();
            Thing ammo = PawnClosestAmmo(pawn, turret, out bool _, forced);

            if (ammo == null)
                return false;

            // Update selected ammo if necessary
            if (ammo.def != turret.CompAmmo.SelectedAmmo
                && priority < 9f && ammo.stackCount < amountNeeded)    //New option can fill magazine completely (and is otherwise closer)
                    return false;

            return true;
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
            var priority = GetThingPriority(pawn, t, forced);
            if (priority == 0f) return null;

            //Do not check for NeedsReload etc. -- if forced, treat as if NeedsReload && AllowAutomaticReload

            Building_TurretGunCE turret = t as Building_TurretGunCE;
            if (!turret.CompAmmo.UseAmmo)
                return new Job(CE_JobDefOf.ReloadTurret, t, null);
            
            int amountNeeded = turret.CompAmmo.Props.magazineSize;
            if (turret.CompAmmo.CurrentAmmo == turret.CompAmmo.SelectedAmmo) amountNeeded -= turret.CompAmmo.CurMagCount;

            turret.UpdateNearbyAmmo();

            var ammo = PawnClosestAmmo(pawn, turret, out bool fromInventory, forced);
            
            if (ammo == null)
                return null;
            
            // Update selected ammo if necessary
            if (ammo.def != turret.CompAmmo.SelectedAmmo)    //New option can fill magazine completely (and is otherwise closer)
            {
                if ((priority >= 9f || ammo.stackCount >= amountNeeded))
                    turret.CompAmmo.SelectedAmmo = ammo.def as AmmoDef;
                else
                    return null;
            }
            
            if (fromInventory && !pawn.TryGetComp<CompInventory>().container.TryDrop(ammo, pawn.Position, pawn.Map, ThingPlaceMode.Direct, Mathf.Min(ammo.stackCount, amountNeeded), out ammo))
            {
                Log.ErrorOnce("Found optimal ammo (" + ammo.LabelCap + "), but could not drop from " + pawn.LabelCap, 8164528);
                return null;
            }

            return new Job(CE_JobDefOf.ReloadTurret, t, ammo) { count = Mathf.Min(amountNeeded, ammo.stackCount) };
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