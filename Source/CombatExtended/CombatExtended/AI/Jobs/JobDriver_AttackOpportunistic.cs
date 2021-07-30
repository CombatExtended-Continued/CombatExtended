using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public class JobDriver_AttackOpportunistic : IJobDriver_Tactical
    {
        private ThingWithComps _weapon;
        public ThingWithComps Weapon
        {
            get
            {
                if (_weapon == null)
                    _weapon = (ThingWithComps)TargetA.Thing;
                return _weapon;
            }
        }

        private ThingWithComps _nextWeapon;
        public ThingWithComps NextWeapon
        {
            get
            {
                if (_nextWeapon == null)
                    _nextWeapon = (ThingWithComps)TargetB.Thing;
                return _nextWeapon;
            }
        }

        private ThingWithComps oldWeapon;

        public override IEnumerable<Toil> MakeToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOn(() => pawn.equipment == null);

            if (pawn.equipment == null)
            {
                Log.Warning("CE: JobDriver_AttackStaticOnce recived a pawn with equipment = null");
                yield break;
            }
            yield return Toils_General.Do(() =>
            {
                // Force the old weapon into inventory
                if (this.pawn.equipment.Primary != null)
                {
                    this.oldWeapon = this.pawn.equipment.Primary;
                    this.pawn.equipment.TryTransferEquipmentToContainer(this.oldWeapon, this.CompInventory.container);
                }
                // Force equip the new weapon
                this.pawn.equipment.equipment.TryAddOrTransfer(this.Weapon);
                this.ReadyForNextToil();
            }).FailOn(() => Weapon == null || Weapon.Destroyed);

            CompAmmoUser compAmmo = Weapon.TryGetComp<CompAmmoUser>();
            if (compAmmo != null && compAmmo.EmptyMagazine)
            {
                foreach (Toil toil in Toils_CombatCE.ReloadEquipedWeapon(pawn, compAmmo, TargetIndex.A))
                    yield return toil;
            }
            Toil attackToil = Toils_CombatCE.AttackStatic(this, TargetIndex.B);
            attackToil.AddFinishAction(() =>
            {
                if (oldWeapon != null)
                {
                    // Force the new weapon into inventory
                    if (Weapon != null && Weapon.holdingOwner != this.CompInventory.container && Weapon.holdingOwner == this.pawn.equipment.equipment)
                    {
                        this.oldWeapon = Weapon;
                        this.pawn.equipment.TryTransferEquipmentToContainer(this.Weapon, this.CompInventory.container);
                    }
                    // Force equip the old weapon                   
                    this.pawn.equipment.equipment.TryAddOrTransfer(this.oldWeapon);
                }
            });
            yield return attackToil;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref oldWeapon, "oldWeapon");
        }
    }
}
