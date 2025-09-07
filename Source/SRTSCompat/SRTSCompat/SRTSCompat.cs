using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;
using CombatExtended.Loader;
using RimWorld;
using System.Collections.Generic;
using HarmonyLib;
using SRTS;
using UnityEngine;


namespace CombatExtended.Compatibility.SRTSCompat
{
    [StaticConstructorOnStartup]
    public class SRTSCompat : IModPart
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
            harmony = new Harmony("CombatExtended.Compatibility.SRTSCompat");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
