using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;
using VFESecurity;
using CombatExtended;
using RimWorld;
using Verse.Sound;


namespace CombatExtended.Compatibility
{
    [HarmonyPatch(typeof(ArtilleryStrikeSkyfaller), "Tick")]
    public class Harmony_ArtilleryStrikeSkyfaller_Tick {
        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator gen, IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> ins = instructions.ToList();
            int idx = ins.Count() - 4;
            while (idx< ins.Count()) {
                yield return ins[idx++];
            }
        }
        public static bool Prefix(ArtilleryStrikeSkyfaller __instance) {
            if (__instance.ShellDef is AmmoDef ShellDef) {
                if (__instance.ambientSustainer == null && !ShellDef.detonateProjectile.projectile.soundAmbient.NullOrUndefined())
                    __instance.ambientSustainer = ShellDef.detonateProjectile.projectile.soundAmbient.TrySpawnSustainer(SoundInfo.InMap(__instance, MaintenanceType.PerTick));
                if (__instance.ambientSustainer != null)
                    __instance.ambientSustainer.Maintain();
            }
            return true;

        }
    }
}
