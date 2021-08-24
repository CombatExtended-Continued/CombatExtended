using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class JobDriver_ReloadAttachment : JobDriver
    {
        /// <summary>
        /// The target weapon
        /// </summary>
        public WeaponPlatform Weapon
        {
            get
            {
                return job.GetTarget(TargetIndex.A).Thing as WeaponPlatform;
            }
        }
        /// <summary>
        /// Ammo to be used in reloading.
        /// </summary>
        public Thing Ammo
        {
            get
            {
                return job.GetTarget(TargetIndex.B).Thing;
            }
        }
        private Attachment_AmmoUser _reloader;
        /// <summary>
        /// The ammoUser that will accept this ammo. 
        /// </summary>
        public Attachment_AmmoUser Reloader
        {
            get
            {
                if (_reloader == null)
                {
                    AmmoDef ammoDef = (AmmoDef)job.GetTarget(TargetIndex.B).Thing.def;
                    _reloader = Weapon.verbManager.ammoUsers.First(a => a.AmmoProps?.ammoSet?.ammoTypes?.Any(l => l.ammo == ammoDef) ?? false);
                }
                return _reloader;
            }
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !Reloader.HasAmmo || (Ammo?.Destroyed ?? true));            
            Toil reload = new Toil();
            // the actual wait time.
            reload.initAction = () => Reloader.TryUnload();
            reload.defaultDuration = Mathf.CeilToInt(Reloader.AmmoProps.reloadTime / pawn.GetStatValue(CE_StatDefOf.ReloadSpeed)) * 60;
            reload.defaultCompleteMode = ToilCompleteMode.Delay;            
            yield return reload.WithProgressBarToilDelay(TargetIndex.A);
            // jump back to start if this attachment can be reloaded one by one.
            yield return Toils_Jump.JumpIf(reload, () => !(Reloader?.MagazineFull ?? true) && Reloader.TryReloadAndResume(Ammo) && !Ammo.Destroyed);            
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {            
            return true;
        }
    }
}
