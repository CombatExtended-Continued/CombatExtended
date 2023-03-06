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
                float biocodeChance = (request.BiocodeWeaponChance > 0f) ? request.BiocodeWeaponChance : p.kindDef.biocodeWeaponChance;  //pass biocode weapon chance to generate loadout
                loadoutProps.GenerateLoadoutFor(p, biocodeChance);
            }
        }
    }
}
