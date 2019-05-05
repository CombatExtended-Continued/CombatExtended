using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Harmony;
using RimWorld;
using Verse;
using Verse.Noise;

namespace CombatExtended.Harmony
{
    [HarmonyPatch]
    internal static class HarmonyThingDef
    {
        private static readonly string[] StatsToCull = { "ArmorPenetration", "StoppingPower", "Damage" };
        private const string BurstShotStatName = "BurstShotCount";
        private const string CoverStatName = "CoverEffectiveness";

        static MethodBase TargetMethod()
        {
            var type = AccessTools.Inner(typeof(ThingDef), "<SpecialDisplayStats>c__Iterator1");
            return AccessTools.Method(type, "MoveNext");
        }

        public static void Postfix(IEnumerator<StatDrawEntry> __instance, ref bool __result)
        {
            if (__result)
            {
                var entry = __instance.Current;
                if (entry.LabelCap.Contains(BurstShotStatName.Translate().CapitalizeFirst()))
                {
                    var def = (ThingDef)AccessTools.Field(__instance.GetType(), "$this").GetValue(__instance);
                    var compProps = def.GetCompProperties<CompProperties_FireModes>();

                    if (compProps != null)
                    {
                        var aimedBurstCount = compProps.aimedBurstShotCount;
                        var burstShotCount = ((VerbProperties)AccessTools.Field(__instance.GetType(), "<verb>__4").GetValue(__instance)).burstShotCount;

                        // Append aimed burst count
                        if (aimedBurstCount != burstShotCount)
                        {
                            var valueStringField = AccessTools.Field(typeof(StatDrawEntry), "valueStringInt");
                            valueStringField.SetValue(entry, $"{aimedBurstCount} / {burstShotCount}", BindingFlags.NonPublic | BindingFlags.Instance, null, CultureInfo.InvariantCulture);
                        }
                    }
                }
                // Override cover effectiveness with collision height
                else if (entry.LabelCap.Contains(CoverStatName.Translate().CapitalizeFirst()))
                {
                    // Determine collision height
                    var def = (ThingDef)AccessTools.Field(__instance.GetType(), "$this").GetValue(__instance);
                    if (def.plant?.IsTree ?? false)
                        return;

                    var height = def.Fillage == FillCategory.Full
                        ? CollisionVertical.WallCollisionHeight
                        : def.fillPercent;

                    var newEntry = new StatDrawEntry(entry.category, "CE_CoverHeight".Translate(), height.ToStringByStyle(ToStringStyle.FloatMaxTwo), entry.DisplayPriorityWithinCategory)
                    {
                        overrideReportText = "CE_CoverHeightExplanation".Translate()
                    };

                    AccessTools.Field(__instance.GetType(), "$current").SetValue(__instance, newEntry);
                }
                // Remove obsolete vanilla stats
                else if (StatsToCull.Select(s => s.Translate().CapitalizeFirst()).Contains(entry.LabelCap))
                {
                    __result = __instance.MoveNext();
                }
            }
        }
    }
}