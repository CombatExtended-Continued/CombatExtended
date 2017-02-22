using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse.AI;

namespace CombatExtended
{
    public class JobDriver_ReloadTurret : JobDriver
    {
        private CompAmmoUser _compReloader;
        private CompAmmoUser compReloader
        {
            get
            {
                if (_compReloader == null)
                {
                    Building_TurretGunCE turret = TargetThingA as Building_TurretGunCE;
                    if (turret != null)
                    {
                        _compReloader = turret.compAmmo;
                    }
                }
                return _compReloader;
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            if (compReloader.useAmmo)
            {
                if (pawn.Faction != Faction.OfPlayer)
                {
                    TargetThingB.SetForbidden(false, false);
                    this.FailOnDestroyedOrNull(TargetIndex.A);
                    this.FailOnDestroyedOrNull(TargetIndex.B);
                }
                else
                {
                    this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
                    this.FailOnDestroyedNullOrForbidden(TargetIndex.B);
                }

                // Haul ammo
                yield return Toils_Reserve.Reserve(TargetIndex.A, 1);
                yield return Toils_Reserve.Reserve(TargetIndex.B, 1);
                yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
                yield return Toils_Haul.StartCarryThing(TargetIndex.B);
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
                yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.A, null, false);
            }

            // Wait in place
            Toil waitToil = new Toil();
            waitToil.initAction = delegate
            {
                waitToil.actor.pather.StopDead();
                compReloader.TryStartReload();
            };
            waitToil.defaultCompleteMode = ToilCompleteMode.Delay;
            waitToil.defaultDuration = Mathf.CeilToInt(compReloader.Props.reloadTicks / pawn.GetStatValue(CE_StatDefOf.ReloadSpeed));
            yield return waitToil.WithProgressBarToilDelay(TargetIndex.A);

            //Actual reloader
            Toil reloadToil = new Toil();
            reloadToil.defaultCompleteMode = ToilCompleteMode.Instant;
            reloadToil.initAction = delegate
            {
                Building_TurretGunCE turret = TargetThingA as Building_TurretGunCE;
                if (compReloader != null && turret.compAmmo != null)
                {
                    compReloader.LoadAmmo(TargetThingB);
                }
            };
            if (compReloader.useAmmo) reloadToil.EndOnDespawnedOrNull(TargetIndex.B);
            yield return reloadToil;
        }
    }
}
