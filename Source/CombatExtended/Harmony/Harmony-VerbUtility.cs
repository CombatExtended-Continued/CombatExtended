using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Harmony;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(VerbUtility), "GetProjectile")]
    internal static class Harmony_VerbUtility
    {
        internal static void Postfix(ref ThingDef __result, Verb verb)
        {
            if (__result == null)
            {
                var verbCE = verb as Verb_LaunchProjectileCE;
                __result = verbCE?.Projectile;
            }
        }
    }
}
