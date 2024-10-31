using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using Mono.Cecil.Cil;
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
                    {
                        return;
                    }

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
            if (__instance.GetModExtension<ApparelDefExtension>()?.isRadioPack ?? false)
            {
                __result = __result.Concat(new StatDrawEntry(StatCategoryDefOf.BasicsNonPawn, "CE_Long_Range_Radio".Translate(), "CE_Yes".Translate(), "CE_Long_Range_Radio_Desc".Translate(), 899));
            }
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
                                .Where(x => x.ShouldDisplay());

                __result = __result.Concat(newStats1);
            }
        }
    }

    [HarmonyPatch(typeof(ThingDef), "PostLoad")]
    static class PostLoad_PostFix
    {
        [HarmonyPostfix]
        public static void Postfix(ThingDef __instance)
        {
            if (__instance.HasModExtension<PartialArmorExt>())
            {
                List<DefModExtension> list = __instance.modExtensions.Where(e => e.GetType() == typeof(PartialArmorExt)).ToList();
                if (list.Count > 1)
                {
                    for (int i = 0; i < list.Count - 1; i++)
                    {
                        __instance.modExtensions.Remove(list[i]);
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(ThingDef), "DescriptionDetailed", MethodType.Getter)]

    internal static class ThingDef_DescriptionDetailed
    {
        private static StringBuilder AddShieldCover(ThingDef thingDef, StringBuilder stringBuilder)
        {
            if (thingDef.GetModExtension<ShieldDefExtension>()?.shieldCoverage != null)
            {
                stringBuilder.Append(string.Format("{0}: {1}", "CE_Shield_Coverage".Translate(), ShieldDefExtension.GetShieldProtectedAreas(BodyDefOf.Human, thingDef)));
            }
            else
            {
                stringBuilder.Append(string.Format("{0}: {1}", "Covers".Translate(), thingDef.apparel.GetCoveredOuterPartsString(BodyDefOf.Human)));
            }
            return stringBuilder;
        }
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
                                                       ILGenerator generator)
        {
            var code = new List<CodeInstruction>(instructions);
            int startIndex = -1;
            int endIndex = -1;
            bool foundCovers = false;

            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].opcode == OpCodes.Ldloc_0)
                {
                    startIndex = i;

                    // Search for the next Pop, and check if "Covers" is in between
                    for (int j = i + 1; j < code.Count; j++)
                    {
                        if (code[j].opcode == OpCodes.Ldstr && code[j].operand as string == "Covers")
                        {
                            foundCovers = true;
                        }

                        if (code[j].opcode == OpCodes.Pop)
                        {
                            if (foundCovers)
                            {
                                endIndex = j;
                                break;
                            }
                            else
                            {
                                // If no "Covers" was found, reset startIndex and move to the next possible sequence
                                startIndex = -1;
                                break;
                            }
                        }
                    }
                }

                if (endIndex > -1)
                {
                    break;
                }
            }

            // Remove the code between startIndex and endIndex if a valid range was found
            if (startIndex > -1 && endIndex > -1)
            {
                code[startIndex].opcode = OpCodes.Nop;
                //code[endIndex].opcode = OpCodes.Nop;
                code.RemoveRange(startIndex + 1, endIndex - startIndex - 1);
                code.Insert(startIndex + 1, new CodeInstruction(OpCodes.Ldarg_0));
                code.Insert(startIndex + 2, new CodeInstruction(OpCodes.Ldloc_0));
                code.Insert(startIndex + 3, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ThingDef_DescriptionDetailed), "AddShieldCover", null, null)));
            }
            foreach (var c in code)
            {
                yield return c;
            }
        }
    }
}
