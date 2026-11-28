using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE;

[HarmonyPatch(typeof(Building_TurretGun), "DrawAt")]
internal static class Harmony_DryadsReworked
{
    private static bool Prepare() => ModsConfig.IsActive("RedMattis.Dryads");

    private static bool ShouldDrawMortarRecoil(Building_TurretGun turret) =>
        turret.def.building.IsMortar && turret.AttackVerb is not Verb_DryadTurretCE;

    // Retain vanilla turret drawing. Skip the recoil call that casts the
    // attack verb to Verb_LaunchProjectile when the launcher uses our CE verb.
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var isMortar = AccessTools.PropertyGetter(typeof(Building_TurretGun), "IsMortar");
        var shouldDrawRecoil = AccessTools.Method(typeof(Harmony_DryadsReworked), nameof(ShouldDrawMortarRecoil));
        var matcher = new CodeMatcher(instructions).MatchStartForward(new CodeMatch(instruction => instruction.Calls(isMortar)));
        matcher.ThrowIfInvalid("Could not find the vanilla turret's mortar recoil check.");
        return matcher.Set(OpCodes.Call, shouldDrawRecoil).InstructionEnumeration();
    }
}
