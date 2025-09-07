using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CombatExtended
{
    public static class CE_ThingSetMakerUtility
    {
        public static bool CanGenerate(ThingDef d, bool allowBasic, bool allowAdvanced)
        {
            return d is AmmoDef ammodef && (ammodef.tradeTags?.Contains(AmmoInjector.destroyWithAmmoDisabledTag) ?? false)
                ? AmmoUtility.IsAmmoSystemActive(ammodef) && AdvancedOrBasic(ammodef, allowBasic, allowAdvanced)
                : true;
        }

        public static bool AdvancedOrBasic(AmmoDef d, bool allowBasic, bool allowAdvanced)
        {
            return d.ammoClass == null || (d.ammoClass.advanced ? allowBasic : allowAdvanced);
        }

        public static ThingDef GetAmmoDef(CompProperties_AmmoUser comp, bool random, bool canGenerateAdvanced)
        {
            if (random)
            {
                var ammoDefs = comp.ammoSet.ammoTypes.Select(v => v.ammo).Where(d => canGenerateAdvanced || !d.ammoClass.advanced);
                if (ammoDefs.EnumerableNullOrEmpty())
                {
                    ammoDefs = comp.ammoSet.ammoTypes.Select(v => v.ammo);
                }
                return ammoDefs.RandomElement();
            }
            return comp.ammoSet.ammoTypes.First().ammo;
        }

        public static void GenerateAmmoForWeapon(List<Thing> outThings, bool random, bool canGenerateAdvanced, IntRange magCount)
        {
            List<Thing> ammos = new List<Thing>();
            foreach (var thing in outThings)
            {
                if (thing.TryGetComp<CompAmmoUser>() is CompAmmoUser ammoUser && ammoUser.UseAmmo)
                {
                    Thing ammo = ThingMaker.MakeThing(GetAmmoDef(ammoUser.Props, random, canGenerateAdvanced));
                    ammo.stackCount = Math.Max(Math.Max(ammoUser.Props.AmmoGenPerMagOverride, ammoUser.Props.magazineSize), 1) * magCount.RandomInRange;
                    ammos.Add(ammo);
                }
            }
            foreach (var t in ammos)
            {
                outThings.Add(t);
            }
        }
    }
}
