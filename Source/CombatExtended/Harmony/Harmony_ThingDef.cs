using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Noise;

namespace CombatExtended.HarmonyCE
{
    /*
     *  Removes the stats in StatsToCull from the ThingDef info screen
     *  Acts as a StatWorker for BurstShotCount (which is normally handled by ThingDef)
     *  Acts as a StatWorker for CoverEffectiveness (which is normally handled by ThingDef)
     */

    [HarmonyPatch]
    internal static class Harmony_ThingDef
    {
        private static readonly string[] StatsToCull = { "ArmorPenetration", "StoppingPower", "Damage" };
        private const string BurstShotStatName = "BurstShotCount";
        private const string CoverStatName = "CoverEffectiveness";

        private static System.Type type;
        private static AccessTools.FieldRef<object, VerbProperties> weaponField;
        private static AccessTools.FieldRef<object, ThingDef> thisField;
        private static AccessTools.FieldRef<object, StatDrawEntry> currentField;

        static MethodBase TargetMethod()
        {
            type = typeof(ThingDef).GetNestedTypes(AccessTools.all).FirstOrDefault(x => x.Name.Contains("<SpecialDisplayStats>"));
            weaponField = AccessTools.FieldRefAccess<VerbProperties>(type, AccessTools.GetFieldNames(type).FirstOrDefault(x => x.Contains("<verb>")));
            thisField = AccessTools.FieldRefAccess<ThingDef>(type, AccessTools.GetFieldNames(type).FirstOrDefault(x => x.Contains("this")));
            currentField = AccessTools.FieldRefAccess<StatDrawEntry>(type, AccessTools.GetFieldNames(type).FirstOrDefault(x => x.Contains("current")));

            return AccessTools.Method(type, "MoveNext");
        }

        public static void Postfix(IEnumerator<StatDrawEntry> __instance, ref bool __result)
        {
            if (__result)
            {
                var entry = __instance.Current;
                if (entry.LabelCap.Contains(BurstShotStatName.Translate().CapitalizeFirst()))
                {
                    var def = thisField(__instance);
                    var compProps = def.GetCompProperties<CompProperties_FireModes>();

                    if (compProps != null)
                    {
                        var aimedBurstCount = compProps.aimedBurstShotCount;
                        var burstShotCount = weaponField(__instance).burstShotCount;

                        // Append aimed burst count
                        if (aimedBurstCount != burstShotCount)
                        {
                            entry.valueStringInt = $"{aimedBurstCount} / {burstShotCount}";
                        }
                    }
                }
                // Override cover effectiveness with collision height
                else if (entry.LabelCap.Contains(CoverStatName.Translate().CapitalizeFirst()))
                {
                    // Determine collision height
                    var def = thisField(__instance);
                    if (def.plant?.IsTree ?? false)
                        return;

                    var height = def.Fillage == FillCategory.Full
                        ? CollisionVertical.WallCollisionHeight
                        : def.fillPercent;
                    height *= CollisionVertical.MeterPerCellHeight;

                    var newEntry = new StatDrawEntry(entry.category, "CE_CoverHeight".Translate(), height.ToStringByStyle(ToStringStyle.FloatMaxTwo) + " m", (string)"CE_CoverHeightExplanation".Translate(), entry.DisplayPriorityWithinCategory);

                    currentField(__instance) = newEntry;
                }
                // Remove obsolete vanilla stats
                else if (StatsToCull.Select(s => s.Translate().CapitalizeFirst()).Contains(entry.LabelCap))
                {
                    __result = __instance.MoveNext();
                }
            }
        }
    }

    // To test if it works:
    //      See if the displayed stats in the info card are for the TURRET GUN, rather than for the TURRET BUILDING

    [HarmonyPatch(typeof(ThingDef), "SpecialDisplayStats")]
    static class Harmony_ThingDef_SpecialDisplayStats_Patch
    {
        public static void Postfix(ThingDef __instance, ref IEnumerable<StatDrawEntry> __result, StatRequest req)
        {
            var turretGunDef = __instance.building?.turretGunDef ?? null;

            if (turretGunDef != null)
            {
                var statRequestGun = StatRequest.For(turretGunDef, null);
                
                var cache = __result;
                // :/
                var newStats1 = DefDatabase<StatDef>.AllDefs
                    .Where(x => x.category == StatCategoryDefOf.Weapon
                        && x.Worker.ShouldShowFor(statRequestGun)
                        && !x.Worker.IsDisabledFor(req.Thing)
                        && !(x.Worker is StatWorker_MeleeStats))
                    .Where(x => !cache.Any(y => y.stat == x))
                    .Select(x => new StatDrawEntry(StatCategoryDefOf.Weapon, x, turretGunDef.GetStatValueAbstract(x), statRequestGun, ToStringNumberSense.Undefined))
                    .Where(x => x.ShouldDisplay);
                
                __result = __result.Concat(newStats1);
            }
        }
    }
}