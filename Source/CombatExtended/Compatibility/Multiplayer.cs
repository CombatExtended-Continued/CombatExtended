using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;

namespace CombatExtended.Compatibility
{
    public class Multiplayer
    {
        private static bool isMultiplayerActive = false;

        public static bool CanInstall()
        {
	    Log.Message("Checking Multiplayer Compat");
            return ModLister.HasActiveModWithName("Multiplayer");
        }

        public static void Install()
        {
	    Log.Message("CombatExtended :: Installing Multiplayer Compat");
	    isMultiplayerActive = true;
	    Loader.Loader.LoadCompatAssembly("MultiplayerCompat");
	}

        public static bool InMultiplayer
        {
            get
            {
                if (isMultiplayerActive)
                    return _inMultiplayer();
                return false;
            }
        }

        public static bool IsExecutingCommandsIssuedBySelf
        {
            get
            {
                if (isMultiplayerActive)
                    return _isExecutingCommandsIssuedBySelf();
                return false;
            }
        }

	public static void registerCallbacks(Func<bool> inMP, Func<bool> iecibs) {
	    _inMultiplayer = inMP;
	    _isExecutingCommandsIssuedBySelf = iecibs;
	}

        private static Func<bool> _inMultiplayer = null;

        private static Func<bool> _isExecutingCommandsIssuedBySelf = null;

        [AttributeUsage(AttributeTargets.Method)]
        public class SyncMethodAttribute : Attribute
        {
            public int syncContext = -1;
            public int[] exposeParameters = null;
        }
	

    }
}
