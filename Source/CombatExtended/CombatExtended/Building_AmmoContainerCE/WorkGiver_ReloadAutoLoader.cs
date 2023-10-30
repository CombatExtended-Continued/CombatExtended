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
    public class WorkGiver_ReloadAutoLoader : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn.Map.GetComponent<AutoLoaderTracker>().AutoLoaders;

        public override float GetPriority(Pawn pawn, TargetInfo t) => GetThingPriority(pawn, t.Thing);

        private float GetThingPriority(Pawn pawn, Thing t, bool forced = false)
        {
            Building_AutoloaderCE autoloader = t as Building_AutoloaderCE;

            if (!autoloader.CompAmmoUser.EmptyMagazine || autoloader.isActive)
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
            Building_AutoloaderCE AutoLoader = t as Building_AutoloaderCE;

            var priority = GetThingPriority(pawn, t, forced);

            var ammo = AutoLoader.CompAmmoUser;

            if (AutoLoader.isReloading || AutoLoader.isActive)
            {
                JobFailReason.Is("CE_AutoLoaderBusy".Translate());
                return false;
            }

            return CanReload(pawn, AutoLoader, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_AutoloaderCE AutoLoader = t as Building_AutoloaderCE;

            return JobGiverUtils_Reload.MakeReloadJob(pawn, AutoLoader);
        }

        public static bool CanReload(Pawn pawn, Thing thing, bool forced = false, bool emergency = false)
        {
            if (pawn == null || thing == null)
            {
                CELogger.Warn($"{pawn?.ToString() ?? "null pawn"} could not reload {thing?.ToString() ?? "null thing"} one of the two was null.");
                return false;
            }

            if (!(thing is Building_AutoloaderCE AutoLoader))
            {
                CELogger.Warn($"{pawn} could not reload {thing} because {thing} is not a Building_AutoLoaderCE. If you are a modder, make sure to use {nameof(CombatExtended)}.{nameof(Building_TurretGunCE)} for your turret's compClass.");
                return false;
            }
            var compAmmo = AutoLoader.CompAmmoUser;

            if (compAmmo == null)
            {
                CELogger.Warn($"{pawn} could not reload {AutoLoader} because Building_AutoLoaderCE has no {nameof(CompAmmoUser)}.");
                return false;
            }
            if (AutoLoader.IsBurning() && !emergency)
            {
                CELogger.Message($"{pawn} could not reload {AutoLoader} because Building_AutoLoaderCE is on fire.");
                JobFailReason.Is("CE_TurretIsBurning".Translate());
                return false;
            }
            if (compAmmo.FullMagazine)
            {
                CELogger.Message($"{pawn} could not reload {AutoLoader} because it is full of ammo.");
                JobFailReason.Is("CE_TurretFull".Translate());
                return false;
            }
            if (AutoLoader.IsForbidden(pawn) || !pawn.CanReserve(AutoLoader, 1, -1, null, forced))
            {
                CELogger.Message($"{pawn} could not reload {AutoLoader} because it is forbidden or otherwise busy.");
                return false;
            }
            if (AutoLoader.Faction != pawn.Faction && pawn.Faction != null && AutoLoader.Faction?.RelationKindWith(pawn.Faction) != FactionRelationKind.Ally)
            {
                CELogger.Message($"{pawn} could not reload {AutoLoader} because the Building_AutoLoaderCE is unclaimed or hostile to them.");
                JobFailReason.Is("CE_TurretNonAllied".Translate());
                return false;
            }
            if (compAmmo.UseAmmo && JobGiverUtils_Reload.FindBestAmmo(pawn, AutoLoader) == null)
            {
                JobFailReason.Is("CE_NoAmmoAvailable".Translate());
                return false;
            }
            return true;
        }
    }
}
