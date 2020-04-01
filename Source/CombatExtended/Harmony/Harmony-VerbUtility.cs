using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(VerbUtility), "GetProjectile")]
    internal static class Harmony_VerbUtility
    {
        internal static bool Prefix(Verb verb, ref ThingDef __result)
        {
            if (verb is Verb_LaunchProjectileCE verbCE)
            {
                __result = verbCE.Projectile;
                return false;
            }

            return true;
        }
    }
}
