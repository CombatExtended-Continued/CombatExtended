using System;
using System.Reflection;
using Verse;
using CombatExtended.Loader;
using System.Collections.Generic;
using HarmonyLib;

namespace CombatExtended.Compatibility.VGE2Compat;

[StaticConstructorOnStartup]
public class VGE2Compat : IModPart
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
        harmony = new Harmony("CombatExtended.Compatibility.VGE2Compat");
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        });
    }
}
