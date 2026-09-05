using System;
using System.Reflection;
using Verse;
using CombatExtended.Loader;
using System.Collections.Generic;
using HarmonyLib;
using VEF;


namespace CombatExtended.Compatibility.VEFCompat;
[StaticConstructorOnStartup]
public class VEFCompat : IModPart
{
    private static Harmony harmony;

    public Type GetSettingsType()
    {
        return null;
    }

    public IEnumerable<string> GetCompatList()
    {
        yield break;
    }

    public void PostLoad(ModContentPack content, ISettingsCE _)
    {
        harmony = new Harmony("CombatExtended.Compatibility.VEFCompat");
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        });

    }
}
