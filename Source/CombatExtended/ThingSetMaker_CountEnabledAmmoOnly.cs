using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public class ThingSetMaker_CountEnabledAmmoOnly : ThingSetMaker_Count
    {
        bool basic = true;

        bool advanced = false;

        bool CanSpawn(AmmoDef def)
        {
            if (def.ammoClass != null)
            {
                return def.ammoClass.advanced ? basic : advanced;
            }
            return true;
        }

        protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
        {
            parms.validator = delegate (ThingDef d)
            {
                if (d is AmmoDef ammodef && (ammodef.tradeTags?.Contains(AmmoInjector.destroyWithAmmoDisabledTag) ?? false))
                {
                    return AmmoUtility.IsAmmoSystemActive(ammodef) && CanSpawn(ammodef);
                }
                return true;
            };
            base.Generate(parms, outThings);
        }
    }
    public class ThingSetMaker_StackCountEnabledAmmoOnly : ThingSetMaker_StackCount
    {
        bool basic = true;

        bool advanced = false;

        bool CanSpawn(AmmoDef def)
        {
            if (def.ammoClass != null)
            {
                return def.ammoClass.advanced ? basic : advanced;
            }
            return true;
        }

        protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
        {
            parms.validator = delegate (ThingDef d)
            {
                if (d is AmmoDef ammodef && (ammodef.tradeTags?.Contains(AmmoInjector.destroyWithAmmoDisabledTag) ?? false))
                {
                    return AmmoUtility.IsAmmoSystemActive(ammodef) && CanSpawn(ammodef);
                }
                return true;
            };
            base.Generate(parms, outThings);
        }
    }
    public class ThingSetMaker_CountWithAmmo : ThingSetMaker_Count
    {
        IntRange magCount = new IntRange(2, 5);

        bool random;

        bool canGenerateAdvanced;

        protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
        {
            base.Generate(parms, outThings);
            List<Thing> ammos = new List<Thing>();
            foreach (var thing in outThings)
            {
                if (thing.TryGetComp<CompAmmoUser>() is CompAmmoUser ammoUser && ammoUser.UseAmmo)
                {
                    Thing ammo = ThingMaker.MakeThing(GetAmmoDef(ammoUser.Props));
                    ammo.stackCount = Math.Max(Math.Max(ammoUser.Props.AmmoGenPerMagOverride, ammoUser.Props.magazineSize), 1) * magCount.RandomInRange;
                    ammos.Add(ammo);
                }
            }
            foreach (var t in ammos)
            {
                outThings.Add(t);
            }
        }

        ThingDef GetAmmoDef(CompProperties_AmmoUser comp)
        {
            if (random)
            {
                var ammoDefs = comp.ammoSet.ammoTypes.Select(v => v.ammo).Where(d => canGenerateAdvanced ? true : !d.ammoClass.advanced);
                if (ammoDefs.EnumerableNullOrEmpty())
                {
                    ammoDefs = comp.ammoSet.ammoTypes.Select(v => v.ammo);
                }
                return ammoDefs.RandomElement();
            }
            return comp.ammoSet.ammoTypes.First().ammo;
        }
    }
}
