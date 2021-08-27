using System;
using System.Linq;
using Verse;
using RimWorld;
using CombatExtended.AI;

namespace CombatExtended
{
    public class Verb_ShootCEOneUse : Verb_ShootCE
    {
        public override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                if (this.burstShotsLeft <= 1)
                {
                    this.SelfConsume();
                }
                return true;
            }
            if (CompAmmo != null && CompAmmo.HasMagazine && CompAmmo.CurMagCount <= 0)
            {
                this.SelfConsume();
            }
            else if (this.burstShotsLeft < this.verbProps.burstShotCount)
            {
                this.SelfConsume();
            }
            return false;
        }
        public override void Notify_EquipmentLost()
        {
            if (this.state == VerbState.Bursting && this.burstShotsLeft < this.verbProps.burstShotCount)
            {
                this.SelfConsume();
            }
        }
        private void SelfConsume()
        {
            var inventory = ShooterPawn?.TryGetComp<CompInventory>();
            if (this.EquipmentSource != null && !this.EquipmentSource.Destroyed)
            {
                if (this.EquipmentSource.stackCount > 1)
                {
                    this.EquipmentSource.stackCount--;
                }
                else
                {
                    this.EquipmentSource.Destroy(DestroyMode.Vanish);
                }
            }
            
            if (ShooterPawn?.equipment.Primary == null && inventory != null && ShooterPawn?.jobs.curJob.def != CE_JobDefOf.OpportunisticAttack)
            {
                var newGun = inventory.rangedWeaponList.FirstOrDefault(t => t.def == EquipmentSource.def);
                if (newGun != null)
                {
                    inventory.TrySwitchToWeapon(newGun);
                }
                else
                {
                    inventory.SwitchToNextViableWeapon();
                }
            }
        }
    }
}
