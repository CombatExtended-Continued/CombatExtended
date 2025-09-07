using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class SiegeUtility
    {
        /// <summary>
        /// The minimum required construction skill to be able to build any potential siege artillery in the game.
        /// </summary>
        public static readonly int MinRequiredConstructionSkill;

        static SiegeUtility()
        {
            MinRequiredConstructionSkill = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.building?.buildingTags.Contains("Artillery_BaseDestroyer") ?? false)
                .Select(def => def.constructionSkillPrerequisite)
                .Max();
        }

        /// <summary>
        /// Determine whether the given thing is a valid shell usable by this siege.
        /// </summary>
        /// <param name="thing">The thing to check.</param>
        /// <param name="siege">The siege.</param>
        /// <returns>true if at least one type of artillery piece taking part in the siege can use this shell, false otherwise.</returns>
        public static bool IsValidShellType(Thing thing, LordToil_Siege siege)
        {
            if (thing.def is AmmoDef { spawnAsSiegeAmmo: true } ammoDef)
            {
                return UniqueArtilleryDefs(siege)
                    .SelectMany(def => def.building.turretGunDef.comps)
                    .OfType<CompProperties_AmmoUser>()
                    .SelectMany(props => props.ammoSet.ammoTypes)
                    .Any(ammoLink => ammoLink.ammo == ammoDef);
            }

            return false;
        }

        /// <summary>
        /// Supply additional shells for each type of artillery piece taking part in this siege.
        /// </summary>
        /// <param name="siege">The siege to resupply.</param>
        public static void DropAdditionalShells(LordToil_Siege siege)
        {
            Lord lord = siege.lord;
            bool allowToxGas = false;
            if (ModsConfig.BiotechActive && lord.faction.def == FactionDefOf.PirateWaster)
            {
                allowToxGas = true;
            }

            foreach (var artilleryDef in UniqueArtilleryDefs(siege))
            {
                // NOTE: Vanilla applies a hardcoded market price cap of 250 here, while we do not.
                // Since we already limit the number of shells to be dropped in and also filter by tech level,
                // such a hardcoded price cap would only serve to cause issues with modded shells that may be pricier.
                var shellDef = TurretGunUtility.TryFindRandomShellDef(
                    artilleryDef,
                    allowEMP: false,
                    allowToxGas: allowToxGas,
                    mustHarmHealth: true,
                    lord.faction.def.techLevel,
                    allowAntigrainWarhead: false,
                    faction: lord.faction
                );

                if (shellDef != null)
                {
                    siege.DropSupplies(shellDef, LordToil_Siege.ShellReplenishCount);
                }
            }
        }

        /// <summary>
        /// Get the unique artillery types taking part in this siege.
        /// </summary>
        /// <param name="siege">The siege to get artillery types for.</param>
        /// <returns>Enumerable of unique artillery defs taking part in this siege.</returns>
        private static IEnumerable<ThingDef> UniqueArtilleryDefs(LordToil_Siege siege) => siege.lord.ownedBuildings
            .Select(t => t.def)
            .Where(def => def.building.buildingTags.Contains("Artillery_BaseDestroyer"))
            .Distinct();
    }
}
