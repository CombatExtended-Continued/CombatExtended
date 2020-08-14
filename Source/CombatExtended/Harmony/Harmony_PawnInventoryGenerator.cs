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
    [HarmonyPatch(typeof(PawnInventoryGenerator), "GenerateInventoryFor")]
    static class Harmony_PawnInventoryGenerator_GenerateInventoryFor
    {
        public static void Postfix(Pawn p, PawnGenerationRequest request)
        {
            var loadoutProps = p.kindDef.GetModExtension<LoadoutPropertiesExtension>();
            if (loadoutProps != null)
            {
                loadoutProps.GenerateLoadoutFor(p);
            }
        }
    }
}
