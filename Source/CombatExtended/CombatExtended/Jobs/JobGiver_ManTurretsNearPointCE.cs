using RimWorld;
using Verse;
using Verse.AI;

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

namespace CombatExtended
{
    public class JobGiver_ManTurretsNearPointCE : JobGiver_ManTurretsNearPoint
    {
        /// <inheritdoc cref="JobGiver_ManTurrets.TryGiveJob" />
        /// <remarks>Overriden to avoid invalid type cast exception.</remarks>
        protected override Job TryGiveJob(Pawn pawn)
        {
            var thing = GenClosest.ClosestThingReachable(
                GetRoot(pawn),
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.InteractionCell, TraverseParms.For(pawn),
                maxDistFromPoint,
                t => t.def.hasInteractionCell &&
                     t.def.HasComp(typeof(CompMannable)) &&
                     pawn.CanReserve(t) &&
                     FindAmmoForTurret(pawn, t) != null);

            if (thing == null)
            {
                return null;
            }

            return new Job(JobDefOf.ManTurret, thing)
            {
                expiryInterval = 2000,
                checkOverrideOnExpire = true
            };
        }

        /// <remarks>Copied from <see cref="JobDriver_ManTurret.FindAmmoForTurret" />.</remarks>
        private static Thing FindAmmoForTurret(Pawn pawn, Thing turret)
        {
            var allowedShellsSettings = pawn.IsColonist
                ? null
                : turret.TryGetComp<CompChangeableProjectile>()?.allowedShellsSettings;

            return GenClosest.ClosestThingReachable(
                turret.Position,
                turret.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Shell),
                PathEndMode.OnCell,
                TraverseParms.For(pawn),
                40f,
                t => !t.IsForbidden(pawn) &&
                     pawn.CanReserve(t, 10, 1) &&
                     (allowedShellsSettings == null || allowedShellsSettings.AllowedToAccept(t)));
        }
    }
}