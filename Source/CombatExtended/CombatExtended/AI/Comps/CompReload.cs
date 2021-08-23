using System;
using RimWorld;
using Verse;

namespace CombatExtended.AI
{
    public class CompReload : ICompTactics
    {
        public override int Priority => 200;

        public CompReload()
        {
        }

        public override bool StartCastChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            // try to reload attachment
            if(verb is Verb_ShootUseAttachment useAttachment && verb.EquipmentSource is WeaponPlatform weapon)
            {
                if (useAttachment.AmmoUser == null)
                    return true;
                if (useAttachment.AmmoUser.HasMagazine)
                    return true;
                if (useAttachment.AmmoUser.HasAmmo && SelPawn.jobs.curJob?.def != CE_JobDefOf.ReloadWeaponAttachment)                
                    useAttachment.AmmoUser.TryStartReload();
                else // no ammo has been found so we need to switch to the primary weapon verb.
                    weapon.verbManager.SelectedVerb = null;
                return false;
            }
            // gun isn't an ammo user that stores ammo internally or isn't out of bullets.
            CompAmmoUser gun = verb.EquipmentSource.TryGetComp<CompAmmoUser>();

            // check if gun has 
            if (gun == null || !gun.HasMagazine || gun.CurMagCount > 0)
                return true;

            // if out of ammo 
            if (verb.EquipmentSource == CurrentWeapon && !gun.HasAmmo)
            {
                CompInventory.SwitchToNextViableWeapon(true, useAOE: !SelPawn.Faction.IsPlayer, stopJob: false);
                return false;
            }

            // start the reloading process
            gun.TryStartReload();
            return false;
        }
    }
}
