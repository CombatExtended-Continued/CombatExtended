using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE.Compatibility
{

    public static class Harmony_AlienRace
    {
        //private static Type TypeOf_PartGenerator
        //{
        //    get
        //    {
        //        return AccessTools.TypeByName("AlienRace.AlienPartGenerator");
        //    }
        //}
        //
        //private static Type TypeOf_ThingDef_AlienRace
        //{
        //    get
        //    {
        //        return AccessTools.TypeByName("AlienRace.ThingDef_AlienRace");
        //    }
        //}
        //
        //private static Type TypeOf_AlienSettings
        //{
        //    get
        //    {
        //        return AccessTools.TypeByName("AlienRace.ThingDef_AlienRace+AlienSettings");
        //    }
        //}
        //
        //private static Type TypeOf_GeneralSettings
        //{
        //    get
        //    {
        //        return AccessTools.TypeByName("AlienRace.GeneralSettings");
        //    }
        //}

        private static Type TypeOf_HarmonyPatches
        {
            get
            {
                return AccessTools.TypeByName("AlienRace.HarmonyPatches");
            }
        }

        //[HarmonyPatch]
        //public static class Harmony_AlienPartGenerator_customHeadDrawSize
        //{
        //    public static bool Prepare()
        //    {
        //        return TypeOf_PartGenerator != null;
        //    }
        //
        //    public static MethodBase TargetMethod()
        //    {
        //        return AccessTools.Method(typeof(Harmony_PawnRenderer), nameof(Harmony_PawnRenderer.GetHeadCustomSize));
        //    }
        //
        //    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        //    {
        //        var local = generator.DeclareLocal(TypeOf_ThingDef_AlienRace);
        //        var l1 = generator.DefineLabel();
        //
        //        yield return new CodeInstruction(OpCodes.Ldarg_0);
        //        yield return new CodeInstruction(OpCodes.Isinst, TypeOf_ThingDef_AlienRace);
        //        yield return new CodeInstruction(OpCodes.Stloc_S, local);
        //        yield return new CodeInstruction(OpCodes.Ldloc_S, local);
        //        yield return new CodeInstruction(OpCodes.Brfalse_S, l1);
        //
        //        yield return new CodeInstruction(OpCodes.Ldloc_S, local);
        //        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(TypeOf_ThingDef_AlienRace, "alienRace"));
        //        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(TypeOf_AlienSettings, "generalSettings"));
        //        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(TypeOf_GeneralSettings, "alienPartGenerator"));
        //        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(TypeOf_PartGenerator, "customHeadDrawSize"));
        //
        //        yield return new CodeInstruction(OpCodes.Ret);
        //        /*
        //         * Label for the default value
        //         */
        //        yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Vector2), nameof(Vector2.oneVector))) { labels = new List<Label>() { l1 } };
        //        yield return new CodeInstruction(OpCodes.Ret);
        //    }
        //}

        [HarmonyPatch]
        public static class Harmony_AlienPartGenerator_GetPawnHairMesh
        {
            public static bool Prepare()
            {
                return TypeOf_HarmonyPatches != null;
            }

            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(Harmony_PawnRenderer), nameof(Harmony_PawnRenderer.GetHeadMesh));
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return new CodeInstruction(OpCodes.Ldarg_3);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(TypeOf_HarmonyPatches, "GetPawnHairMesh"));
                yield return new CodeInstruction(OpCodes.Ret);
            }
        }
    }
}
