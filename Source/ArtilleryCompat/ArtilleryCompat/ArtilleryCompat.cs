using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;
using CombatExtended.Loader;
using RimWorld;
using System.Collections.Generic;
using HarmonyLib;
using VFESecurity;

namespace CombatExtended.Compatibility {
    [StaticConstructorOnStartup]
    public class ArtilleryCompat: IModPart {
        public Type GetSettingsType() {
            return null;
        }
        public IEnumerable<string> GetCompatList() {
            yield break;
        }
        public void PostLoad(ModContentPack content, ISettingsCE _) {
            var harmony = new Harmony("CombatExtended.Artillery.HarmonyCE");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
