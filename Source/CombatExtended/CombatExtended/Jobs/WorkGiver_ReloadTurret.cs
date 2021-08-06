using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using CombatExtended.CombatExtended.LoggerUtils;
using CombatExtended.CombatExtended.Jobs.Utils;
using CombatExtended.Compatibility;


namespace CombatExtended
{
    public class WorkGiver_ReloadTurret : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn.Map.GetComponent<TurretTracker>().Turrets;

        public override float GetPriority(Pawn pawn, TargetInfo t) => GetThingPriority(pawn, t.Thing);

        private float GetThingPriority(Pawn pawn, Thing t, bool forced = false)
        {
            CELogger.Message($"pawn: {pawn}. t: {t}. forced: {forced}");
            Building_Turret turret = t as Building_Turret;

	    

            if (!((turret as Building_TurretGunCE)?.Active ?? true)) return 1f;
            if (turret.GetAmmo()?.EmptyMagazine ?? false) return 9f;
            if (turret.GetMannable()==null) return 5f;
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
            // Should never happen anymore, as only BuildingTurrets should be returned from PotentialWorkThingsGlobal
	    if (!(t is Building_Turret turret))
	    {
                return false;
            }
            
            var priority = GetThingPriority(pawn, t, forced);
            CELogger.Message($"Priority check completed. Got {priority}");

	    var ammo = turret.GetAmmo();
            CELogger.Message($"Turret uses ammo? {ammo?.UseAmmo}");
            if (!turret.GetReloadable())
            {
                return false;
            }
            CELogger.Message($"Total magazine size: {ammo.MagSize}. Needed: {ammo.MissingToFullMagazine}");

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
            Building_Turret turret = t as Building_Turret;
            if (turret == null)
            {
                CELogger.Error($"{pawn} tried to make a reload job on a {t} which isn't a turret. This should never be reached.");
            }

            // NOTE: The removal of the code that used to be here disables reloading turrets directly from one's inventory.
            // The player would need to drop the ammo the pawn is holding first.

            return JobGiverUtils_Reload.MakeReloadJob(pawn, turret);
        }
    }
}
