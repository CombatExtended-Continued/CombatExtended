using System;
using RimWorld;
using RimWorld.Planet;

namespace CombatExtended
{
    public static class FactionHosilityUtility
    {
        public static ShellingResponseDef GetShellingResponseDef(this Faction faction) => GetShellingResponseDef(faction?.def ?? null);

        public static ShellingResponseDef GetShellingResponseDef(this FactionDef factionDef)
        {
            if(factionDef == null)
            {
                return CE_ShellingResponseDefOf.CE_ShellingPreset_Undefined;
            }
            FactionDefExtensionCE extension = null;
            if (factionDef.HasModExtension<FactionDefExtensionCE>())
            {
                extension = factionDef.GetModExtension<FactionDefExtensionCE>();
                if(extension.shellingResponse != null)
                {
                    return extension.shellingResponse;
                }
            }
            switch (factionDef.techLevel)
            {
                case TechLevel.Undefined:
                    return CE_ShellingResponseDefOf.CE_ShellingPreset_Undefined;
                case TechLevel.Animal:
                    return CE_ShellingResponseDefOf.CE_ShellingPreset_Animal;
                case TechLevel.Neolithic:
                    return CE_ShellingResponseDefOf.CE_ShellingPreset_Neolithic;
                case TechLevel.Medieval:
                    return CE_ShellingResponseDefOf.CE_ShellingPreset_Medieval;
                case TechLevel.Industrial:
                    return CE_ShellingResponseDefOf.CE_ShellingPreset_Industrial;
                case TechLevel.Spacer:
                    return CE_ShellingResponseDefOf.CE_ShellingPreset_Spacer;
                case TechLevel.Ultra:
                    return CE_ShellingResponseDefOf.CE_ShellingPreset_Ultra;
                case TechLevel.Archotech:
                    return CE_ShellingResponseDefOf.CE_ShellingPreset_Archotech;
            }
            throw new NotImplementedException($"CE: GetShellingResponseDef() {factionDef.label} tech level {factionDef.techLevel} has no preset!");
        }       
    }
}

