using Verse;

namespace CombatExtended.Compatibility
{
    class Patches
    {
        public static void Init()
        {
            if (EDShields.CanInstall())
            {
                EDShields.Install();
            }

            Log.Message($"VanillaFurnitureExpandedShields.CanInstall() - {VanillaFurnitureExpandedShields.CanInstall()}");
            if (VanillaFurnitureExpandedShields.CanInstall())
            {
                VanillaFurnitureExpandedShields.Install();
            }
        }
    }
}
