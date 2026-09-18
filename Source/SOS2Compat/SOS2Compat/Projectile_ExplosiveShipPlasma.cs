using SaveOurShip2;
using Verse;

namespace CombatExtended.Compatibility.SOS2Compat;

// Plasma shuttle projectile. Reuses SOS2's ship-projectile behavior (explosion, shield interaction, heat)
// but deliberately omits the ShipCombatLaserMote that Projectile_ExplosiveShipLaser spawns on impact, so
// plasma reads as plasma instead of a laser beam. thingClass is pinned to this in Patches/Save Our Ship 2/ShuttleProjectiles.xml.
public class Projectile_ExplosiveShipPlasma : Projectile_ExplosiveShip
{
}
