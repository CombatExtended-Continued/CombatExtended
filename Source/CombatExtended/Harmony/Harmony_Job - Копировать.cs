using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(IngredientCount), nameof(IngredientCount.CountRequiredOfFor))]
    public static class Harmony_IngredientCount_CountRequiredOfFor
    {
        public static bool Prefix(ThingDef thingDef, RecipeDef recipe, Bill bill, ref int __result)
        {
            if (recipe.defName == "BurnWeapon" && thingDef is AmmoDef && bill != null)
            {
                try
                {
                    __result = bill.Map.listerThings.listsByDef.FirstOrDefault(x => x.Key == thingDef).Value.Where(x => !x.IsForbidden(Faction.OfPlayer)).Sum(x => x.stackCount);
                    return false;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    __result = default(int);
                }
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Log), nameof(Log.Message), new Type[] {typeof(string)})]
    public static class Harmony_Message
    {
        public static void Prefix(string text)
        {
            if(text == null)
            {
                throw new Exception("Logged null");
            }
        }
    }
}
