using System;
using System.Diagnostics;
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
    [HarmonyPatch(typeof(FallingBomb),
            MethodType.Constructor,
            new Type[] { typeof(Thing), typeof(CompExplosive), typeof(Map), typeof(string) })]
    public static class Harmony_FallingBomb_Thing_CompExplosive_Map_string
    {
        // Adds an additional null check for def.projectileWhenLoaded of
        // this.projectile = def.projectileWhenLoaded.projectile;
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
                ILGenerator il)
        {
            bool found = false;
            Label projectileDoesntExist = il.DefineLabel();

            FieldInfo thingDefProjectileField = AccessTools.DeclaredField(typeof(ThingDef),
                    "projectile");

            List<CodeInstruction> codes = instructions.ToList();
            for (int i = 0; i < codes.Count(); ++i)
            {
                if (!found && codes[i].LoadsField(thingDefProjectileField))
                {
                    found = true;

                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Brfalse_S, projectileDoesntExist);
                    yield return codes[i];

                    ++i;
                    codes[i].labels.Add(projectileDoesntExist);
                }

                yield return codes[i];
            }
        }
    }
}
