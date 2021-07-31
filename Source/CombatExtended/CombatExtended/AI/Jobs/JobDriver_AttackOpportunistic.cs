using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public class JobDriver_AttackOpportunistic : IJobDriver_Tactical
    {
        private ThingWithComps oldWeapon;

        private ThingWithComps _jobWeapon;
        public ThingWithComps JobWeapon
        {
            get
            {
                if (_jobWeapon == null)
                    _jobWeapon = (ThingWithComps)TargetA.Thing;
                return _jobWeapon;
            }
        }

        protected override IEnumerable<Toil> MakeToils()
        {
            if (pawn.equipment == null)
            {
                Log.Warning("CE: JobDriver_AttackStaticOnce recived a pawn with equipment = null");
                yield break;
            }
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOn(() => pawn.equipment == null);

            yield return Toils_General.Do(() =>
            {
                // Force the old weapon into inventory
                if (this.pawn.equipment.Primary != null)
                {
                    this.oldWeapon = this.pawn.equipment.Primary;
                    this.pawn.equipment.TryTransferEquipmentToContainer(this.oldWeapon, this.CompInventory.container);
                }
                // Force equip the new weapon
                this.pawn.equipment.equipment.TryAddOrTransfer(this.JobWeapon);
                this.ReadyForNextToil();
            }).FailOn(() => JobWeapon == null || JobWeapon.Destroyed);

            // Reload if needed
            CompAmmoUser compAmmo = JobWeapon.TryGetComp<CompAmmoUser>();
            if (compAmmo != null && compAmmo.EmptyMagazine)
                yield return Toils_CombatCE.ReloadEquipedWeapon(this, TargetIndex.A);

            // Start the attack
            foreach (Toil toil in Toils_CombatCE.AttackStatic(this, TargetIndex.B))
                yield return toil;

            // switch back action
            this.AddFinishAction(() =>
            {
                if (oldWeapon != null)
                {
                    if (oldWeapon != pawn.equipment?.Primary && !oldWeapon.Destroyed)
                    {
                        // Force the new weapon into inventory
                        if (pawn.equipment?.Primary != null)
                            this.pawn.equipment.TryTransferEquipmentToContainer(pawn.equipment?.Primary, this.CompInventory.container);
                        // Force equip the old weapon                   
                        this.pawn.equipment.equipment.TryAddOrTransfer(this.oldWeapon);
                    }
                }
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref oldWeapon, "oldWeapon");
        }
    }
}
