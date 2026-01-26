using System;
using System.Reflection;
using Verse;
using CombatExtended.Loader;
using System.Collections.Generic;
using HarmonyLib;

namespace CombatExtended.Compatibility.VFES
{
    public class VFESecurityCompat : IModPart
    {
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
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                var harmony = new Harmony("CombatExtended.Compatibility.VFES");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            });
        }
    }
}
