using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    /// <summary>
    /// Replace <see cref="TurretGunUtility.TryFindRandomShellDef" /> to support turrets other than the 81mm mortar.
    /// </summary>
    [HarmonyPatch(typeof(TurretGunUtility), nameof(TurretGunUtility.TryFindRandomShellDef))]
    public static class Harmony_TurretGunUtility
    {
        public static bool Prefix(
            ThingDef turret,
            bool allowEMP,
            bool allowToxGas,
            TechLevel techLevel,
            bool allowAntigrainWarhead,
            ref ThingDef __result
        )
        {
            if (!TurretGunUtility.NeedsShells(turret))
            {
                __result = null;
                return false;
            }

            // Fall back to the vanilla logic if we have no ammo configured for this turret (unpatched?)
            var ammoUserProps = turret.building.turretGunDef.comps.OfType<CompProperties_AmmoUser>()
                .FirstOrDefault();
            if (ammoUserProps == null)
            {
                return true;
            }

            IEnumerable<AmmoDef> potentialAmmoDefs = from ammoLink in ammoUserProps.ammoSet.ammoTypes
                                                     let ammoDef = ammoLink.ammo
                                                     where ammoDef.spawnAsSiegeAmmo
                                                     let projectileDef = ammoLink.projectile
                                                     let explosiveDamageDef =
                                                         projectileDef.GetCompProperties<CompProperties_ExplosiveCE>()?.explosiveDamageType ??
                                                         projectileDef.GetCompProperties<CompProperties_Explosive>()?.explosiveDamageType
                                                     let projectileDamageDef = projectileDef.projectile.damageDef
                                                     where explosiveDamageDef != null || projectileDamageDef != null

                                                     // Only allow EMP or tox gas shells if explicitly allowed and relevant DLC is available
                                                     where allowEMP || (explosiveDamageDef != DamageDefOf.EMP && projectileDamageDef != DamageDefOf.EMP)
                                                     where allowToxGas || !ModsConfig.BiotechActive || (explosiveDamageDef != DamageDefOf.ToxGas &&
                                                                                                         projectileDamageDef != DamageDefOf.ToxGas)

                                                     // No antigrain warheads
                                                     where allowAntigrainWarhead || ammoDef != ThingDefOf.Shell_AntigrainWarhead

                                                     // No higher tech shells than the tech level of the requesting faction
                                                     where techLevel == TechLevel.Undefined || ammoDef.techLevel <= techLevel
                                                     select ammoDef;

            // Respect individual weighting of matching shells within the ammoset
            potentialAmmoDefs.TryRandomElementByWeight(def => def.generateAllowChance, out __result);

            return false;
        }
    }
}
