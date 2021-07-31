using System;
using RimWorld;
using Verse;

namespace CombatExtended.AI
{
    public class CompReload : ICompTactics
    {
        public override int Priority => 200;

        public override bool StartCastChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
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
