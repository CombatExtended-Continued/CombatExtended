using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class JobDriver_ReloadTurret : JobDriver
    {
        #region Fields
        private CompAmmoUser _compReloader;
        #endregion Fields

        #region Properties
        private string errorBase => this.GetType().Assembly.GetName().Name + " :: " + this.GetType().Name + " :: ";

        private Building_TurretGunCE turret => TargetThingA as Building_TurretGunCE;
        private AmmoThing ammo => TargetThingB as AmmoThing;

        private CompAmmoUser compReloader
        {
            get
            {
                if (_compReloader == null && turret != null)
                    _compReloader = turret.CompAmmo;
                return _compReloader;
            }
        }
        #endregion

        #region Methods

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job);
        }

        public override string GetReport()
        {
            string text = CE_JobDefOf.ReloadTurret.reportString;
            string turretType = (turret.def.hasInteractionCell ? "CE_MannedTurret" : "CE_AutoTurret").Translate();
            text = text.Replace("TurretType", turretType);
            text = text.Replace("TargetA", TargetThingA.def.label);
            if (compReloader.UseAmmo)
                text = text.Replace("TargetB", TargetThingB.def.label);
            else
                text = text.Replace("TargetB", "CE_ReloadingGenericAmmo".Translate());
            return text;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Error checking/input validation.
            if (turret == null)
            {
                Log.Error(string.Concat(errorBase, "TargetThingA isn't a Building_TurretGunCE"));
                yield return null;
            }
           	if (compReloader == null)
            {
                Log.Error(string.Concat(errorBase, "TargetThingA (Building_TurretGunCE) is missing it's CompAmmoUser."));
                yield return null;
            }
            if (compReloader.UseAmmo && ammo == null)
            {
                Log.Error(string.Concat(errorBase, "TargetThingB is either null or not an AmmoThing."));
                yield return null;
            }

            // Set fail condition on turret.
            if (pawn.Faction != Faction.OfPlayer)
                this.FailOnDestroyedOrNull(TargetIndex.A);
            else
                this.FailOnDestroyedNullOrForbidden(TargetIndex.A);

            if (compReloader.UseAmmo)
            {
                // Perform ammo system specific activities, failure condition and hauling
                if (pawn.Faction != Faction.OfPlayer)
                {
                    ammo.SetForbidden(false, false);
                    this.FailOnDestroyedOrNull(TargetIndex.B);
                }
                else
                {
                    this.FailOnDestroyedNullOrForbidden(TargetIndex.B);
                }

                // Haul ammo
                yield return Toils_Reserve.Reserve(TargetIndex.B, 1);
                yield return Toils_Goto.GotoCell(ammo.Position, PathEndMode.ClosestTouch);
                yield return Toils_Haul.StartCarryThing(TargetIndex.B);
                yield return Toils_Goto.GotoCell(turret.Position, PathEndMode.ClosestTouch);
                //yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.A, null, false);
            } else
            {
                // If ammo system is turned off we just need to go to the turret.
                yield return Toils_Goto.GotoCell(turret.Position, PathEndMode.ClosestTouch);
            }

            // Wait in place
            Toil waitToil = new Toil() { actor = pawn };
            waitToil.initAction = delegate
            {
                // Initial relaod process activities.
                waitToil.actor.pather.StopDead();
                turret.isReloading = true;
                if (compReloader.ShouldThrowMote)
                    MoteMaker.ThrowText(turret.Position.ToVector3Shifted(), turret.Map, string.Format("CE_ReloadingTurretMote".Translate(), TargetThingA.LabelCapNoCount));
                Thing newAmmo;
                compReloader.TryUnload(out newAmmo);
                if (newAmmo?.CanStackWith(ammo) ?? false)
                    pawn.carryTracker.TryStartCarry(newAmmo, Mathf.Min(newAmmo.stackCount, compReloader.Props.magazineSize - ammo.stackCount));
            };
            waitToil.AddFinishAction(() => turret.isReloading = false);
            waitToil.defaultCompleteMode = ToilCompleteMode.Delay;
            waitToil.defaultDuration = Mathf.CeilToInt(compReloader.Props.reloadTime.SecondsToTicks() / pawn.GetStatValue(CE_StatDefOf.ReloadSpeed));
            yield return waitToil.WithProgressBarToilDelay(TargetIndex.A);

            //Actual reloader
            Toil reloadToil = new Toil();
            reloadToil.defaultCompleteMode = ToilCompleteMode.Instant;
            reloadToil.initAction = delegate
            {
                compReloader.LoadAmmo(ammo);
            };
            //if (compReloader.useAmmo) reloadToil.EndOnDespawnedOrNull(TargetIndex.B);
            yield return reloadToil;
        }
        #endregion Methods
    }
}
