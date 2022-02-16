using Multiplayer.API;

namespace CombatExtended.Compatibility
{
    internal class Multiplayer
    {
        public static bool CanInstall()
        {
            return MP.enabled;
        }

        public static void Install()
        {

        }
    }
}
