using CombatExtended.CombatExtended.Jobs.Utils;
using CombatExtended.CombatExtended.LoggerUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld;
using CombatExtended.Compatibility;

namespace CombatExtended
{
    public class JobGiver_DefenderReloadTurret : ThinkNode_JobGiver
    {
        /// <summary>
        /// How low can ammo get before we want to reload the turret?
        /// Set arbitrarily, balance if needed.
        /// </summary>
        private const float ammoReloadThreshold = .5f;
        public override Job TryGiveJob(Pawn pawn)
        {
            var turret = TryFindTurretWhichNeedsReloading(pawn);
            if (turret == null)
            {
                return null; // signals ThinkResult.NoJob.
            }
            return JobGiverUtils_Reload.MakeReloadJob(pawn, turret);
        }

        private Building_TurretGunCE TryFindTurretWhichNeedsReloading(Pawn pawn)
        {
            Predicate<Thing> _isTurretThatNeedsReloadingNow = (Thing t) =>
            {
                var turret = t as Building_Turret;
                if (turret == null) { return false; }
                if (!JobGiverUtils_Reload.CanReload(pawn, turret, forced: false, emergency: true)) { return false; }
                return turret.ShouldReload(ammoReloadThreshold);
            };

            var hopefullyTurret = pawn.Map.GetComponent<TurretTracker>().ClosestTurret(
                pawn.Position,
                PathEndMode.Touch,
                TraverseParms.For(pawn),
                100f,
                _isTurretThatNeedsReloadingNow);

            return hopefullyTurret as Building_TurretGunCE;
        }

    
    }
}
