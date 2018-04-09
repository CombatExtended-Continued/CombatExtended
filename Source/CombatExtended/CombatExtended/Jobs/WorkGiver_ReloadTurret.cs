using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace CombatExtended
{
    public class WorkGiver_ReloadTurret : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial); ;
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_TurretGunCE turret = t as Building_TurretGunCE;
            if (turret == null || (!forced && !turret.AllowAutomaticReload)) return false;
            
            if (!turret.NeedsReload
                || !pawn.CanReserveAndReach(turret, PathEndMode.ClosestTouch, Danger.Deadly)
                || turret.IsForbidden(pawn.Faction))
            {
                return false;
            }
            if (!turret.CompAmmo.UseAmmo) return true;
            Thing ammo = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                            ThingRequest.ForDef(turret.CompAmmo.SelectedAmmo),
                            PathEndMode.ClosestTouch,
                            TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn),
                            80,
                            x => !x.IsForbidden(pawn) && pawn.CanReserve(x));
            return ammo != null;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_TurretGunCE turret = t as Building_TurretGunCE;
            if (turret == null || turret.CompAmmo == null) return null;

            if (!turret.CompAmmo.UseAmmo)
            {
                return new Job(CE_JobDefOf.ReloadTurret, t, null);
            }

            // Iterate through all possible ammo types for NPC's to find whichever is available, starting with currently selected
            Thing ammo = null;
            var ammoTypes = turret.CompAmmo.Props.ammoSet.ammoTypes.Select(l => l.ammo).ToList();
            var index = ammoTypes.IndexOf(turret.CompAmmo.SelectedAmmo);
            for (int i = 0; i < ammoTypes.Count; i++)
            {
                index = (index + i) % ammoTypes.Count;
                ammo = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                                ThingRequest.ForDef(ammoTypes[index]),
                                PathEndMode.ClosestTouch,
                                TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn),
                                80,
                                x => !x.IsForbidden(pawn) && pawn.CanReserve(x));
                if (ammo != null || pawn.Faction == Faction.OfPlayer)
                    break;
            }
            if (ammo == null)
                return null;

            // Update selected ammo if necessary
            if (ammo.def != turret.CompAmmo.SelectedAmmo)
                turret.CompAmmo.SelectedAmmo = ammo.def as AmmoDef;

            // Create the actual job
            int amountNeeded = turret.CompAmmo.Props.magazineSize;
            if (turret.CompAmmo.CurrentAmmo == turret.CompAmmo.SelectedAmmo) amountNeeded -= turret.CompAmmo.CurMagCount;
            return new Job(DefDatabase<JobDef>.GetNamed("ReloadTurret"), t, ammo) { count = Mathf.Min(amountNeeded, ammo.stackCount) };
        }


    }
}