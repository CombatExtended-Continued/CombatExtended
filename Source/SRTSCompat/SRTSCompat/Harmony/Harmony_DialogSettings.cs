using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using CombatExtended.Loader;
using RimWorld;
using System.Collections.Generic;
using HarmonyLib;
using SRTS;
using UnityEngine;

namespace CombatExtended.Compatibility.SRTSCompat
{
    [HarmonyPatch(typeof(Dialog_AllowedBombs),
            nameof(Dialog_AllowedBombs.DoWindowContents),
            new Type[] { typeof(Rect) })]
    public static class Harmony_Dialog_AllowedBombs_DoWindowContents_Rect
    {
        static FieldInfo explosivesStringInfo = AccessTools.DeclaredField(typeof(Dialog_AllowedBombs),
                "explosivesString");
        public static List<ThingDef> SearchForExplosives(Dialog_AllowedBombs __instance)
        {
            // You think this is bad? Imagine how it'd be as a single line
            return DefDatabase<ThingDef>.AllDefs
                .Where(thingDef => !SRTSMod.mod.settings.allowedBombs.Contains(thingDef.defName)
                        && thingDef.building is null
                        && !thingDef.IsWeapon
                        && !thingDef.IsApparel
                        && thingDef.projectile == null
                        && 0 <= CultureInfo.CurrentCulture.CompareInfo.IndexOf(thingDef.defName,
                            explosivesStringInfo.GetValue(__instance) as string,
                            CompareOptions.IgnoreCase)
                        && (thingDef.GetCompProperties<CompProperties_Explosive>() != null
                            || ((thingDef as AmmoDef)?.AmmoSetDefs?
                                .Any(ammoSetDef => ammoSetDef.ammoTypes?
                                    .Any(ammoLink => ammoLink.projectile.GetCompProperties<CompProperties_ExplosiveCE>() != null
                                        || ammoLink.projectile.projectile?.explosionRadius > 0.5f)
                                    ?? false)
                                ?? false)
                            )
                        )
                .ToList();
        }

        /// Replaces
        //  if (SRTSHelper.CEModLoaded)
        //  {
        //      explosivesSearched = DefDatabase<ThingDef>.AllDefs
        //          .Where(x => x.HasComp(Type.GetType("CombatExtended.CompExplosiveCE,CombatExtended"))
        //                  && !SRTSMod.mod.settings.allowedBombs.Contains(x.defName)
        //                  && CultureInfo.CurrentCulture.CompareInfo.IndexOf(x.defName, explosivesString, CompareOptions.IgnoreCase) >= 0)
        //          .ToList();
        //  }
        //  else
        //  {
        //      explosivesSearched = DefDatabase<ThingDef>.AllDefs
        //          .Where(x => x.GetCompProperties<CompProperties_Explosive>() != null
        //                  && x.building is null
        //                  && !SRTSMod.mod.settings.allowedBombs.Contains(x.defName)
        //                  && CultureInfo.CurrentCulture.CompareInfo.IndexOf(x.defName, explosivesString, CompareOptions.IgnoreCase) >= 0)
        //          .ToList();
        //  }
        /// With
        //  explosivesSearched = Harmony_Dialog_AllowedBombs_DoWindowContents_Rect.SearchForExplosives(this);
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
                ILGenerator il)
        {
            bool found = false;

            FieldInfo explosivesChangedField = AccessTools.DeclaredField(typeof(Dialog_AllowedBombs),
                    "explosivesChanged");
            CodeInstruction callIns = CodeInstruction.Call(
                    typeof(Harmony_Dialog_AllowedBombs_DoWindowContents_Rect),
                    "SearchForExplosives",
                    new Type[] { typeof(Dialog_AllowedBombs) });

            List<CodeInstruction> codes = instructions.ToList();
            for (int i = 0; i < codes.Count(); ++i)
            {
                if (!found && i > 1
                        && codes[i - 2].opcode == OpCodes.Ldc_I4_0
                        && codes[i - 1].StoresField(explosivesChangedField))
                {
                    found = true;

                    if (callIns == null)
                    {
                        Log.Error("Combat Extended :: SRTSCompat Dialog_AllowedBombs.DoWindowContents(Rect) - "
                                + "tried patching, but no function to patch with");
                        goto SkipTranspile;
                    }

                    Label? skipToLabel = null;
                    for (int j = i; j >= 0; --j)
                    {
                        if (codes[j].Branches(out skipToLabel))
                        {
                            break;
                        }
                    }
                    if (skipToLabel == null)
                    {
                        Log.Error("Combat Extended :: SRTSCompat Dialog_AllowedBombs.DoWindowContents(Rect) - "
                                + "didn't find any branch to labels before the target; did we even find the target?");
                        goto SkipTranspile;
                    }

                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return callIns;
                    yield return CodeInstruction.StoreField(typeof(Dialog_AllowedBombs), "explosivesSearched");

                    while (i < codes.Count() && !codes[i].labels.Any(label => label == skipToLabel))
                    {
                        ++i;
                    }
                    if (codes.Count() == i)
                    {
                        Log.Error("Combat Extended :: SRTSCompat Dialog_AllowedBombs.DoWindowContents(Rect) - "
                                + "we skipped over the entire code without finding the skip label. "
                                + "If you're reading this, know that your bombs selection menu is now screwed :)");
                    }
                }

            SkipTranspile:
                yield return codes[i];
            }
        }
    }

}
