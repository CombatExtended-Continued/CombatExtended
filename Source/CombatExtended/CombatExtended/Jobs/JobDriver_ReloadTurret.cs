using System.Collections.Generic;
using CombatExtended.CombatExtended.LoggerUtils;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using CombatExtended.Compatibility;


namespace CombatExtended
{
    public class JobDriver_ReloadTurret : JobDriver
    {
        #region Fields
        private CompAmmoUser _compReloader;
        #endregion Fields

        #region Properties
        private string errorBase => this.GetType().Assembly.GetName().Name + " :: " + this.GetType().Name + " :: ";

        private Building_Turret turret => TargetThingA as Building_Turret;
        private AmmoThing ammo => TargetThingB as AmmoThing;

        private CompAmmoUser compReloader
        {
            get
            {
                if (_compReloader == null && turret != null)
                {
                    _compReloader = turret.GetAmmo();
                }
                return _compReloader;
            }
        }
        #endregion

        #region Methods

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(TargetA, job))
            {
                CELogger.Message("Combat Extended: Could not reserve turret for reloading job.");
                return false;
            }

            var compAmmo = turret?.GetAmmo();

            if (compAmmo == null)
            {
                CELogger.Error($"{TargetA} has no CompAmmo, this should not have been reached.");
                return false;
            }

            if (!compAmmo.UseAmmo)
            {
                return true;
            }

            if (ammo == null)
            {
                CELogger.Message("Combat Extended: Ammo is null");
                return false;
            }
            if (!pawn.Reserve(TargetB, job, Mathf.Max(1, TargetThingB.stackCount - job.count), job.count))
            {
                CELogger.Message("Combat Extended: Could not reserve " + Mathf.Max(1, TargetThingB.stackCount - job.count) + " of ammo.");
                return false;
            }
            CELogger.Message("Combat Extended: Managed to reserve everything successfully.");
            return true;
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

        public override IEnumerable<Toil> MakeNewToils()
        {
            // Error checking/input validation.
            if (turret == null)
            {
                Log.Error(string.Concat(errorBase, "TargetThingA isn't a Building_TurretGunCE"));
                yield return null;
            }
            if (compReloader == null)
            {
                Log.Error(string.Concat(errorBase, "TargetThingA (Building_TurretGunCE) is missing its CompAmmoUser."));
                yield return null;
            }
            if (compReloader.UseAmmo && ammo == null)
            {
                Log.Error(string.Concat(errorBase, "TargetThingB is either null or not an AmmoThing."));
                yield return null;
            }

            AddEndCondition(delegate
            {
                return (pawn.Downed || pawn.Dead || pawn.InMentalState || pawn.IsBurning()) ? JobCondition.Incompletable : JobCondition.Ongoing;
            });

            this.FailOnIncapable(PawnCapacityDefOf.Manipulation);

            // Set fail condition on turret.
            if (pawn.Faction != Faction.OfPlayer)
                this.FailOnDestroyedOrNull(TargetIndex.A);
            else
                this.FailOnDestroyedNullOrForbidden(TargetIndex.A);

            // If someone else magically reloaded our turret while we were reloading, fail.
            // This happens when Project RimFactory's refueling machine is set up.
            this.FailOn(() => compReloader.MissingToFullMagazine == 0);

            // Perform ammo system specific activities, failure condition and hauling
            if (compReloader.UseAmmo)
            {
                var toilGoToCell = Toils_Goto.GotoCell(ammo.Position, PathEndMode.Touch).FailOnBurningImmobile(TargetIndex.B);
                var toilCarryThing = Toils_Haul.StartCarryThing(TargetIndex.B).FailOnBurningImmobile(TargetIndex.B);

                if (TargetThingB is AmmoThing)
                {
                    toilGoToCell.AddEndCondition(delegate { return (TargetThingB as AmmoThing).IsCookingOff ? JobCondition.Incompletable : JobCondition.Ongoing; });
                    toilCarryThing.AddEndCondition(delegate { return (TargetThingB as AmmoThing).IsCookingOff ? JobCondition.Incompletable : JobCondition.Ongoing; });
                }

                if (pawn.Faction != Faction.OfPlayer)
                {
                    ammo.SetForbidden(true, false);
                    toilGoToCell.FailOnDestroyedOrNull(TargetIndex.B);
                    toilCarryThing.FailOnDestroyedOrNull(TargetIndex.B);
                }
                else
                {
                    toilGoToCell.FailOnDestroyedNullOrForbidden(TargetIndex.B);
                    toilCarryThing.FailOnDestroyedNullOrForbidden(TargetIndex.B);
                }

                //yield return Toils_Reserve.Reserve(TargetIndex.B, Mathf.Max(1, TargetThingB.stackCount - job.count), job.count);
                yield return toilGoToCell;
                yield return toilCarryThing;
                //yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.A, null, false);
            }

            // If ammo system is turned off we just need to go to the turret.
            yield return Toils_Goto.GotoCell(turret.Position, PathEndMode.Touch);

            //If pawn fails reloading from this point, reset isReloading
            this.AddFinishAction(delegate { turret.SetReloading(false); });

            // Wait in place
            Toil waitToil = new Toil() { actor = pawn };
            waitToil.initAction = delegate
            {
                // Initial relaod process activities.
                turret.SetReloading(true);
                waitToil.actor.pather.StopDead();
                if (compReloader.ShouldThrowMote)
                {
                    MoteMaker.ThrowText(turret.Position.ToVector3Shifted(), turret.Map, string.Format("CE_ReloadingTurretMote".Translate(), TargetThingA.LabelCapNoCount));
                }
                //Thing newAmmo;
                //compReloader.TryUnload(out newAmmo);
                //if (newAmmo?.CanStackWith(ammo) ?? false)
                //{
                //    pawn.carryTracker.TryStartCarry(newAmmo, Mathf.Min(newAmmo.stackCount, compReloader.MagSize - ammo.stackCount));
                //}
                AmmoDef currentAmmo = compReloader.CurrentAmmo;
                if (currentAmmo != ammo?.def)    //Turrets are reloaded without unloading the mag first (if using same ammo type), to support very high capacity magazines
                {
                    compReloader.TryUnload(out Thing newAmmo);
                }
            };
            waitToil.defaultCompleteMode = ToilCompleteMode.Delay;
            waitToil.defaultDuration = Mathf.CeilToInt(compReloader.Props.reloadTime.SecondsToTicks() / pawn.GetStatValue(CE_StatDefOf.ReloadSpeed));
            yield return waitToil.WithProgressBarToilDelay(TargetIndex.A);

            //Actual reloader
            Toil reloadToil = new Toil();
            reloadToil.defaultCompleteMode = ToilCompleteMode.Instant;
            reloadToil.initAction = delegate
            {
                compReloader.LoadAmmo(ammo);
                turret.SetReloading(false);
            };
            //if (compReloader.useAmmo) reloadToil.EndOnDespawnedOrNull(TargetIndex.B);
            yield return reloadToil;
        }
        #endregion Methods
    }
}
