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
        public static List<ActiveArtilleryStrike> HarmfulStrikes(List<ActiveArtilleryStrike> artilleryStrikes)
        {
            return artilleryStrikes.Where(s => (s.shellDef.projectile?.damageDef?.harmsHealth ?? false) || ((s.shellDef is AmmoDef) && ((AmmoDef)s.shellDef).detonateProjectile.projectile.damageDef.harmsHealth)).ToList();
        }

        public static ProjectileProperties GetProjectileProperties(this ThingDef thingDef) {
            return thingDef.GetProjectile()?.projectile;
        }

        public static void ArtilleryTick(ArtilleryComp artilleryComp, ThingDef shellDef)
        {
            artilleryComp.bombardmentDurationTicks -= (int)(shellDef.BaseMarketValue * ((int)shellDef.techLevel) * artilleryComp.artilleryCount);
            if (artilleryComp.bombardmentDurationTicks < 1)
            {
                artilleryComp.bombardmentDurationTicks = 1;
            }
            artilleryComp.artilleryCooldownTicks += (((int)shellDef.techLevel * (int)shellDef.techLevel) + 0.0f).SecondsToTicks();
        }
    }
}
