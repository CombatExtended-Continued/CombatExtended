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
            Building_AmmoContainerCE turret = t as Building_AmmoContainerCE;

            if (!turret.CompAmmoUser.EmptyMagazine || turret.isActive)
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
            Building_AmmoContainerCE turret = t as Building_AmmoContainerCE;

            var priority = GetThingPriority(pawn, t, forced);

            var ammo = turret.CompAmmoUser;

            return CanReload(pawn, turret, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_AmmoContainerCE turret = t as Building_AmmoContainerCE;

            return JobGiverUtils_Reload.MakeReloadJob(pawn, turret);
        }

        public static bool CanReload(Pawn pawn, Thing thing, bool forced = false, bool emergency = false)
        {
            if (pawn == null || thing == null)
            {
                CELogger.Warn($"{pawn?.ToString() ?? "null pawn"} could not reload {thing?.ToString() ?? "null thing"} one of the two was null.");
                return false;
            }

            if (!(thing is Building_AmmoContainerCE turret))
            {
                CELogger.Warn($"{pawn} could not reload {thing} because {thing} is not a Turret. If you are a modder, make sure to use {nameof(CombatExtended)}.{nameof(Building_TurretGunCE)} for your turret's compClass.");
                return false;
            }
            var compAmmo = turret.CompAmmoUser;

            if (compAmmo == null)
            {
                CELogger.Warn($"{pawn} could not reload {turret} because turret has no {nameof(CompAmmoUser)}.");
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
            if (turret.Faction != pawn.Faction && pawn.Faction != null && turret.Faction?.RelationKindWith(pawn.Faction) != FactionRelationKind.Ally)
            {
                CELogger.Message($"{pawn} could not reload {turret} because the turret is unclaimed or hostile to them.");
                JobFailReason.Is("CE_TurretNonAllied".Translate());
                return false;
            }
            if (compAmmo.UseAmmo && JobGiverUtils_Reload.FindBestAmmo(pawn, turret) == null)
            {
                JobFailReason.Is("CE_NoAmmoAvailable".Translate());
                return false;
            }
            return true;
        }
    }
}
