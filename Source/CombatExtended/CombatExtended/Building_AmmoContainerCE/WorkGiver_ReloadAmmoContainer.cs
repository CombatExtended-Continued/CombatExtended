using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using CombatExtended;
using CombatExtended.CombatExtended.LoggerUtils;
using CombatExtended.CombatExtended.Jobs.Utils;
using CombatExtended.Compatibility;


namespace CombatExtended
{
    public class WorkGiver_ReloadAmmoContainer : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn.Map.GetComponent<AmmoContainerTracker>().AmmoContainers;

        public override float GetPriority(Pawn pawn, TargetInfo t) => GetThingPriority(pawn, t.Thing);

        private float GetThingPriority(Pawn pawn, Thing t, bool forced = false)
        {
            Building_AmmoContainerCE ammoContainer = t as Building_AmmoContainerCE;

            if (!ammoContainer.CompAmmoUser.EmptyMagazine || ammoContainer.isActive)
            {
                return 9f;
            }
            return 1f;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (forced)
            {
                return false;
            }
            if (pawn.CurJob == null)
            {
                return false;
            }
            if (pawn.CurJobDef == JobDefOf.ManTurret)
            {
                return false;
            }
            if (pawn.CurJob.playerForced)
            {
                return true;
            }
            return (pawn.CurJobDef == CE_JobDefOf.ReloadTurret || pawn.CurJobDef == CE_JobDefOf.ReloadWeapon);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_AmmoContainerCE ammoContainer = t as Building_AmmoContainerCE;

            var priority = GetThingPriority(pawn, t, forced);

            var ammo = ammoContainer.CompAmmoUser;

            if (ammoContainer.isReloading || ammoContainer.isActive)
            {
                JobFailReason.Is("CE_AmmoContainerBusy".Translate());
                return false;
            }

            return CanReload(pawn, ammoContainer, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_AmmoContainerCE ammoContainer = t as Building_AmmoContainerCE;

            return JobGiverUtils_Reload.MakeReloadJob(pawn, ammoContainer);
        }

        public static bool CanReload(Pawn pawn, Thing thing, bool forced = false, bool emergency = false)
        {
            if (pawn == null || thing == null)
            {
                CELogger.Warn($"{pawn?.ToString() ?? "null pawn"} could not reload {thing?.ToString() ?? "null thing"} one of the two was null.");
                return false;
            }

            if (!(thing is Building_AmmoContainerCE ammoContainer))
            {
                CELogger.Warn($"{pawn} could not reload {thing} because {thing} is not a Building_AmmoContainerCE. If you are a modder, make sure to use {nameof(CombatExtended)}.{nameof(Building_TurretGunCE)} for your turret's compClass.");
                return false;
            }
            var compAmmo = ammoContainer.CompAmmoUser;

            if (compAmmo == null)
            {
                CELogger.Warn($"{pawn} could not reload {ammoContainer} because Building_AmmoContainerCE has no {nameof(CompAmmoUser)}.");
                return false;
            }
            if (ammoContainer.IsBurning() && !emergency)
            {
                CELogger.Message($"{pawn} could not reload {ammoContainer} because Building_AmmoContainerCE is on fire.");
                JobFailReason.Is("CE_TurretIsBurning".Translate());
                return false;
            }
            if (compAmmo.FullMagazine)
            {
                CELogger.Message($"{pawn} could not reload {ammoContainer} because it is full of ammo.");
                JobFailReason.Is("CE_TurretFull".Translate());
                return false;
            }
            if (ammoContainer.IsForbidden(pawn) || !pawn.CanReserve(ammoContainer, 1, -1, null, forced))
            {
                CELogger.Message($"{pawn} could not reload {ammoContainer} because it is forbidden or otherwise busy.");
                return false;
            }
            if (ammoContainer.Faction != pawn.Faction && pawn.Faction != null && ammoContainer.Faction?.RelationKindWith(pawn.Faction) != FactionRelationKind.Ally)
            {
                CELogger.Message($"{pawn} could not reload {ammoContainer} because the Building_AmmoContainerCE is unclaimed or hostile to them.");
                JobFailReason.Is("CE_TurretNonAllied".Translate());
                return false;
            }
            if (compAmmo.UseAmmo && JobGiverUtils_Reload.FindBestAmmo(pawn, ammoContainer) == null)
            {
                JobFailReason.Is("CE_NoAmmoAvailable".Translate());
                return false;
            }
            return true;
        }
    }
}
