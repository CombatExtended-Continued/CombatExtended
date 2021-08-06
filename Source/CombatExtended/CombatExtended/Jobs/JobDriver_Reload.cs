using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class JobDriver_Reload : JobDriver
    {
        #region Fields
        private CompAmmoUser _compReloader = null;
        private ThingWithComps initEquipment = null;
        private Thing initAmmo = null;
        private bool reloadingInventory = false;
        private bool reloadingEquipment = false;
        #endregion Fields

        #region Properties
        private string errorBase => this.GetType().Assembly.GetName().Name + " :: " + this.GetType().Name + " :: ";

        // TargetA == Pawn holder/reloader (equipment or inventory)
        private TargetIndex indReloader => TargetIndex.A;
        private Pawn holder => TargetThingA as Pawn;
        // TargetB == ThingWithComps (weapon)
        private TargetIndex indWeapon => TargetIndex.B;
        private ThingWithComps weapon => TargetThingB as ThingWithComps; //intentionally non-caching.

        private bool weaponEquipped { get { return pawn?.equipment?.Primary == weapon; } }
        private bool weaponInInventory { get { return pawn?.inventory?.innerContainer.Contains(weapon) ?? false; } }

        /// <summary>
        /// Gets and caches the CompAmmoUser.
        /// </summary>
        private CompAmmoUser compReloader
        {
            get
            {
                if (_compReloader == null) _compReloader = weapon.TryGetComp<CompAmmoUser>();
                return _compReloader;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Generates the string which is shown in the interface when a pawn is selected while they are performing this job.
        /// </summary>
        /// <returns>string representing the current activity of the pawn doing this job.</returns>
        public override string GetReport()
        {
            string text = CE_JobDefOf.ReloadWeapon.reportString;
            string flagSource = "";
            if (reloadingEquipment) flagSource = "CE_ReloadingEquipment".Translate();
            if (reloadingInventory) flagSource = "CE_ReloadingInventory".Translate();
            text = text.Replace("FlagSource", flagSource);
            text = text.Replace("TargetB", weapon.def.label);
            if (Controller.settings.EnableAmmoSystem && initAmmo != null)
                text = text.Replace("AmmoType", initAmmo.LabelNoCount);
            else
                text = text.Replace("AmmoType", "CE_ReloadingGenericAmmo".Translate());
            return text;
        }

        /// <summary>
        /// Additional failure conditions that we want to end this job on.
        /// </summary>
        /// <returns>bool, true indicates the job should be halted</returns>
        private bool HasNoGunOrAmmo()
        {
            //if (TargetThingB.DestroyedOrNull() || pawn.equipment == null || pawn.equipment.Primary == null || pawn.equipment.Primary != TargetThingB)
            if ((reloadingEquipment && (pawn?.equipment?.Primary == null || pawn.equipment.Primary != weapon))
                   || (reloadingInventory && (pawn.inventory == null || !pawn.inventory.innerContainer.Contains(weapon)))
                || (initEquipment != pawn?.equipment?.Primary))
                return true;

            //CompAmmoUser comp = pawn.equipment.Primary.TryGetComp<CompAmmoUser>();
            //return comp != null && comp.useAmmo && !comp.hasAmmo;
            //return comp != null && !comp.hasAndUsesAmmoOrMagazine;
            // expecting true ends the job.  if comp == null then will return false from the first part and not test the second.  Job will continue (bad).

            return compReloader == null || !compReloader.HasAndUsesAmmoOrMagazine;
        }

        /// <summary>
        /// Generates a series of actions (toils) that the pawn should perform.
        /// </summary>
        /// <returns>Ienumerable of type Toil</returns>
        /// <remarks>Remember that, in the case of jobs, effectively the entire method is executed before any actual activity occurs.</remarks>
        public override IEnumerable<Toil> MakeNewToils()
        {
            // Error checking and 'helpful' messages for what is wrong.
            if (holder == null) // A later check will catch this (failon) but that fails silently.
            {
                Log.Error(errorBase + "TargetThingA is null.  A Pawn is required to perform a reload.");
                yield return null;
            }
            if (weapon == null) // Required.
            {
                Log.Error(errorBase + "TargetThingB is null.  A weapon (ThingWithComps) is required to perform a reload.");
                yield return null;
            }
            if (compReloader == null) // Required.
            {
                Log.Error(errorBase + pawn + " tried to do reload job on " + weapon.LabelCap + " which doesn't have a required CompAmmoUser.");
                yield return null;
            }
            if (holder != pawn) // Helps restrict what this job does, not really necessary and may work fine (though possibly undesirable behavior) without this check.
            {
                Log.Error(errorBase + "TargetThingA (" + holder.Name + ") is not the same Pawn (" + pawn.Name + ") that was given the job.");
                yield return null;
            }
            // get the state of the job (inventory vs other) at the start.
            reloadingInventory = weaponInInventory;
            reloadingEquipment = weaponEquipped;
            // A couple more more 'helpful' error check/message.
            if (!reloadingInventory && !reloadingEquipment) // prevent some bad states/behavior on FailOn and job text.
            {
                Log.Error(errorBase + "Unable to find the weapon to be reloaded (" + weapon.LabelCap + ") in the inventory nor equipment of " + pawn.Name);
                yield return null;
            }
            if (reloadingInventory && reloadingEquipment) // prevent incorrect information on job text.  If somehow this was true may cause a FailOn to trip.
            {
                Log.Error(errorBase + "Something went spectacularly wrong as the weapon to be reloaded was found in both the Pawn's equipment AND inventory at the same time.");
                yield return null;
            }

            // current state of equipment, want to interrupt the reload if a pawn's equipment changes.
            initEquipment = pawn.equipment?.Primary;

            // choose ammo to be loaded and set failstate for no ammo in inventory
            if (compReloader.UseAmmo)
            {
                this.FailOn(() => !compReloader.TryFindAmmoInInventory(out initAmmo));
            }

            // setup fail states, if something goes wrong with the pawn performing the reload, the weapon, or something else that we want to fail on.
            this.FailOnDespawnedOrNull(indReloader);
            this.FailOnMentalState(indReloader);
            this.FailOnDestroyedOrNull(indWeapon);
            this.FailOn(HasNoGunOrAmmo);

            // Throw mote
            if (compReloader.ShouldThrowMote && holder.Map != null)     //holder.Map is temporarily null after game load, skip mote if a pawn was reloading when game was saved
            {
                MoteMaker.ThrowText(pawn.Position.ToVector3Shifted(), holder.Map, string.Format("CE_ReloadingMote".Translate(), weapon.def.LabelCap));
            }

            //Toil of do-nothing		
            Toil waitToil = new Toil() { actor = pawn }; // actor was always null in testing...
            waitToil.initAction = () => waitToil.actor.pather.StopDead();
            waitToil.defaultCompleteMode = ToilCompleteMode.Delay;
            waitToil.defaultDuration = Mathf.CeilToInt(compReloader.Props.reloadTime.SecondsToTicks() / pawn.GetStatValue(CE_StatDefOf.ReloadSpeed));
            yield return waitToil.WithProgressBarToilDelay(indReloader);

            //Actual reloader
            Toil reloadToil = new Toil();
            reloadToil.AddFinishAction(() => compReloader.LoadAmmo(initAmmo));
            yield return reloadToil;

            // If reloading one shot at a time and if possible to reload, jump back to do-nothing toil
            System.Func<bool> jumpCondition =
                () => compReloader.Props.reloadOneAtATime &&
                      compReloader.CurMagCount < compReloader.MagSize &&
                      (!compReloader.UseAmmo || compReloader.TryFindAmmoInInventory(out initAmmo));
            Toil jumpToil = Toils_Jump.JumpIf(waitToil, jumpCondition);
            yield return jumpToil;

            //Continue previous job if possible
            Toil continueToil = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return continueToil;
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        #endregion Methods
    }
}
