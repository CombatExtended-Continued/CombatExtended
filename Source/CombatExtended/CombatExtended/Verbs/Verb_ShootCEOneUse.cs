using System;
using System.Linq;
using Verse;
using RimWorld;

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
                this.EquipmentSource.Destroy(DestroyMode.Vanish);
			}
            if (inventory != null)
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
