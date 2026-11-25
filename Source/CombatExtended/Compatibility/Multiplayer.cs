using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;
using System.Collections.Generic;

namespace CombatExtended.Compatibility;

public class Multiplayer : IPatch
{
    private static bool isMultiplayerActive = false;

    public bool CanInstall()
    {
        Log.Message("Combat Extended :: Checking Multiplayer Compat");
        return ModLister.HasActiveModWithName("Multiplayer") || ModLister.GetActiveModWithIdentifier("rwmt.Multiplayer") != null || ModLister.HasActiveModWithName("Multiplayer [Continuous]");
    }

    public void Install()
    {
        Log.Message("CombatExtended :: Installing Multiplayer Compat");
        isMultiplayerActive = true;
    }

    public static bool InMultiplayer
    {
        get
        {
            if (isMultiplayerActive)
            {
                return _inMultiplayer();
            }
            return false;
        }
    }

    public static bool IsExecutingCommands
    {
        get
        {
            if (isMultiplayerActive)
            {
                return _isExecutingCommands();
            }

            return false;
        }
    }

    public static bool IsExecutingCommandsIssuedBySelf
    {
        get
        {
            if (isMultiplayerActive)
            {
                return _isExecutingCommandsIssuedBySelf();
            }
            return false;
        }
    }

    public static void syncField<T>(T target, string field, object val)
    {
        if (InMultiplayer)
        {
            _syncField(target, typeof(T).FullName + "/" + field, val);
        }
    }

    public static void registerCallbacks(Func<bool> inMP, Func<bool> iec, Func<bool> iecibs, Func<object, string, object, bool> sf)
    {
        _inMultiplayer = inMP;
        _isExecutingCommands = iec;
        _isExecutingCommandsIssuedBySelf = iecibs;
        _syncField = sf;
    }

    private static Func<object, string, object, bool> _syncField = null;

    private static Func<bool> _inMultiplayer = null;

    private static Func<bool> _isExecutingCommands = null;

    private static Func<bool> _isExecutingCommandsIssuedBySelf = null;

    [AttributeUsage(AttributeTargets.Method)]
    public class SyncMethodAttribute : Attribute
    {
        public int syncContext = -1;
        public int[] exposeParameters = null;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SyncFieldAttribute : Attribute
    {
    }


}
