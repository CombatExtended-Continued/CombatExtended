using System;
using CombatExtended;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AOC17CE
{
    [HarmonyPatch(typeof(ArmorUtilityCE), "TryPenetrateArmor")]
    public static class SpearParryZeroArmor
    {
        public static bool Prefix(DamageDef def, float armorAmount, ref float penAmount,
            ref float dmgAmount, Thing armor, float partDensity, ref bool __result)
        {
            string name = armor?.def?.defName;
            if (name != "AOC_MonoSpear" && name != "AOC_LongMonoSpear" && name != "AOC_Monojavelin")
            {
                return true;
            }

            if (armorAmount != 0f || partDensity != 0f || def == null ||
                (def.armorCategory != DamageArmorCategoryDefOf.Sharp &&
                 def.armorCategory != CE_DamageArmorCategoryDefOf.Blunt) ||
                float.IsNaN(penAmount) || float.IsInfinity(penAmount) || penAmount < 0f ||
                float.IsNaN(dmgAmount) || float.IsInfinity(dmgAmount) || dmgAmount < 0f)
            {
                return true;
            }

            // Holder stat multipliers can legitimately reduce weapon toughness
            // to zero. Resolve that boundary without CE's armor/penetration
            // divisions. At zero armor CE retains the incoming damage/AP and
            // computes zero weapon wear; neither the stats nor parry odds change.
            penAmount = Math.Max(0f, penAmount);
            dmgAmount = Math.Max(0f, dmgAmount);
            __result = true;
            return false;
        }
    }
}
