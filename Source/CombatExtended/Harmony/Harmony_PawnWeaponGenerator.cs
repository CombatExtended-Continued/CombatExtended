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
    [HarmonyPatch(typeof(PawnWeaponGenerator), "TryGenerateWeaponFor")]
    static class Harmony_PawnWeaponGenerator_TryGenerateWeaponFor
    {
        public static void Postfix(Pawn pawn, PawnGenerationRequest request)
        {
            var loadoutProps = pawn.kindDef.GetModExtension<LoadoutPropertiesExtension>();
            if (loadoutProps != null)
            {
                float biocodeChance = (request.BiocodeWeaponChance > 0f) ? request.BiocodeWeaponChance : pawn.kindDef.biocodeWeaponChance;  //pass biocode weapon chance to generate loadout
                loadoutProps.GenerateLoadoutFor(pawn, biocodeChance);
            }
        }
    }
}
