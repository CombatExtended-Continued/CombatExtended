using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE.Compatibility;

// Education launches a Verse.Projectile directly during lessons. A CE weapon can
// supply a separate projectile for that job while keeping its normal firing verb.
public class ClassroomProjectileExtension : DefModExtension
{
    public ThingDef projectile;
}

[HarmonyPatch]
internal static class Harmony_ProgressionEducation
{
    private static bool Prepare() => ModsConfig.IsActive("ferny.ProgressionEducation");

    private static MethodBase TargetMethod() =>
        AccessTools.Method("ProgressionEducation.JobDriver_AttendShootingClass:FireProjectile");

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // Substitute only the value read by the classroom job. The verb and ammo
        // definitions are shared, including with weapons elsewhere on the map.
        return new CodeMatcher(instructions)
            .MatchStartForward(CodeMatch.LoadsField(AccessTools.Field(typeof(VerbProperties), nameof(VerbProperties.defaultProjectile))))
            .ThrowIfInvalid("Combat Extended could not locate the classroom projectile in Progression: Education.")
            .Advance(1)
            .Insert(CodeInstruction.Call(typeof(Harmony_ProgressionEducation), nameof(ClassroomProjectile)))
            .InstructionEnumeration();
    }

    private static ThingDef ClassroomProjectile(ThingDef projectile) =>
        projectile?.GetModExtension<ClassroomProjectileExtension>()?.projectile ?? projectile;
}
