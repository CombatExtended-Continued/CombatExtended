//using CombatExtended.Loader;
//using HarmonyLib;
//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Reflection;
//using Verse;

//namespace CombatExtended.Compatibility.VGECompat;

//public class VGECompat : IModPart
//{
//    private static Harmony harmony;

//    public Type GetSettingsType()
//    {
//        return null;
//    }
//    public IEnumerable<string> GetCompatList()
//    {
//        yield break;
//    }
//    public void PostLoad(ModContentPack content, ISettingsCE _)
//    {
//        harmony = new Harmony("CombatExtended.Compatibility.VGECompat");
//        harmony.PatchAll(Assembly.GetExecutingAssembly());
//    }
//}
