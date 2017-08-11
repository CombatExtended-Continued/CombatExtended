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
    [HarmonyPatch(typeof(RaidStrategyWorker_ImmediateAttackSappers), "CanBeSapper")]
    public static class Harmony_RaidStrategyWorker_ImmediateAttackSappers_CanBeSapper
    {
        public static void Postfix(ref bool __result, PawnKindDef kind)
        {
            if (!__result)
            {
                __result = !kind.weaponTags.NullOrEmpty() && kind.weaponTags.Contains("CE_GrenadeNeolithic");
            }
        }
    }
}
