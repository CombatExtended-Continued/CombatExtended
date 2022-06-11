using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;
using VFESecurity;
using CombatExtended;
using RimWorld;
using Verse.Sound;


namespace CombatExtended.Compatibility.Artillery
{
    [HarmonyPatch(typeof(CompProperties_LongRangeArtillery), MethodType.Constructor)]
    public class Harmony_CompProperties_LongRangeArtillery_Constructor {
        public static void Postfix(CompProperties_LongRangeArtillery __instance) {
            __instance.compClass = typeof(CompLongRangeArtilleryCE);
        }
    }
}
