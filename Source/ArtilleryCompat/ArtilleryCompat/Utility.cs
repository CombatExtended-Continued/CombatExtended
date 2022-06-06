using CombatExtended;
using System.Collections.Generic;
using System.Linq;
using VFESecurity;
using RimWorld;
using Verse;

namespace CombatExtended.Compatibility.Artillery
{
    public static class Utility
    {
	public static List<ActiveArtilleryStrike> HarmfulStrikes(List<ActiveArtilleryStrike> artilleryStrikes) {
            return artilleryStrikes.Where(s => (s.shellDef.projectile?.damageDef?.harmsHealth ?? false) || ((s.shellDef is AmmoDef) && ((AmmoDef)s.shellDef).detonateProjectile.projectile.damageDef.harmsHealth)).ToList();
        }

        public static ThingDef GetProjectile(this ThingDef thingDef) {
            if (thingDef.projectile != null) {
                return thingDef;
            }
            if (thingDef is AmmoDef ammoDef) {
                ThingDef user;
                if ((user = ammoDef.Users.FirstOrFallback(null)) != null) {
                    CompProperties_AmmoUser props = user.GetCompProperties<CompProperties_AmmoUser>();
                    AmmoSetDef asd = props.ammoSet;
                    AmmoLink ammoLink;
                    if ((ammoLink = asd.ammoTypes.FirstOrFallback(null)) != null) {
                        return ammoLink.projectile;
                    }
                }
                else {
                    return ammoDef.detonateProjectile;
                }
            }
            return thingDef;
        }

        public static ProjectileProperties GetProjectileProperties(this ThingDef thingDef) {
            return GetProjectile(thingDef)?.projectile;
        }

	public static void ArtilleryTick(ArtilleryComp artilleryComp, ThingDef shellDef)
	{
	    artilleryComp.bombardmentDurationTicks -= (int) (shellDef.BaseMarketValue * ((int)shellDef.techLevel) * artilleryComp.artilleryCount);
	    if (artilleryComp.bombardmentDurationTicks < 1)
	    {
		artilleryComp.bombardmentDurationTicks = 1;
	    }
	    artilleryComp.artilleryCooldownTicks += (((int)shellDef.techLevel * (int)shellDef.techLevel) + 0.0f).SecondsToTicks();
	}
    }
}
