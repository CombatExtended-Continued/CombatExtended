using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    /// <summary>
    /// Transpile LordToil_Siege.LordToilTick to support artillery pieces with different ammosets than the 81mm mortar.
    /// </summary>
    [HarmonyPatch(typeof(LordToil_Siege), "LordToilTick")]
    internal static class Harmony_LordToil_Siege_LordToilTick
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var methodCustomCondition = AccessTools.Method(typeof(SiegeUtility), nameof(SiegeUtility.IsValidShellType));
            var dropAdditionalShells = AccessTools.Method(typeof(SiegeUtility), nameof(SiegeUtility.DropAdditionalShells));

            var isShellMethod = AccessTools.PropertyGetter(typeof(ThingDef), nameof(ThingDef.IsShell));
            var harmsHealthField = AccessTools.Field(typeof(DamageDef), nameof(DamageDef.harmsHealth));
            var tryFindRandomShellDefMethod =
                AccessTools.Method(typeof(TurretGunUtility), nameof(TurretGunUtility.TryFindRandomShellDef));

            var codeMatcher = new CodeMatcher(instructions, generator);

            var isShellPos = codeMatcher
                .Start()
                .MatchStartForward(CodeMatch.Calls(isShellMethod))
                .ThrowIfInvalid("CombatExtended :: Harmony_LordToil_Siege_LordToilTick couldn't find call to IsShell")
                .Pos;

            var harmsHealthPos = codeMatcher.MatchStartForward(CodeMatch.LoadsField(harmsHealthField))
                .ThrowIfInvalid("CombatExtended :: Harmony_LordToil_Siege_LordToilTick couldn't find harmsHealth")
                .Pos;

            // Consider all shell types usable by the siege when assessing how many are available to the siege
            codeMatcher
                .Advance(isShellPos - harmsHealthPos - 1)
                .Insert(
                    CodeInstruction.LoadArgument(0),
                    new CodeInstruction(OpCodes.Call, methodCustomCondition)
                )
                .RemoveInstructionsInRange(isShellPos + 1, harmsHealthPos + 2);

            var ifBlockStart = codeMatcher.Start()
                .MatchStartForward(CodeMatch.Calls(tryFindRandomShellDefMethod))
                .ThrowIfInvalid("CombatExtended :: Harmony_LordToil_Siege_LordToilTick couldn't find call to TryFindRandomShellDef")
                .MatchEndBackwards(CodeMatch.Branches())
                .ThrowIfInvalid("CombatExtended :: Harmony_LordToil_Siege_LordToilTick couldn't find start of enclosing if block")
                .Pos;

            var ifBlockEndLabel = (Label)codeMatcher.Operand;

            // Drop shells for every type of artillery piece used by the siege
            codeMatcher.MatchStartForward(new CodeMatch(instruction => instruction.labels.Contains(ifBlockEndLabel)))
                .ThrowIfInvalid("CombatExtended :: Harmony_LordToil_Siege_LordToilTick couldn't find end of enclosing if block")
                .Insert(
                    CodeInstruction.LoadArgument(0),
                    new CodeInstruction(OpCodes.Call, dropAdditionalShells)
                )
                .RemoveInstructionsInRange(ifBlockStart + 1, codeMatcher.Pos - 1);

            return codeMatcher.Instructions();
        }

    }

    /// <summary>
    /// Transpile LordToil_Siege.Init to remove the market price cap on the initially dropped shell stacks.
    /// </summary>
    [HarmonyPatch(typeof(LordToil_Siege), nameof(LordToil_Siege.Init))]
    internal static class Harmony_LordToil_Siege_Init
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var codeMatcher = new CodeMatcher(instructions, generator);

            codeMatcher.MatchStartForward(CodeMatch.LoadsConstant(250f))
                .ThrowIfInvalid("CombatExtended :: Harmony_LordToil_Siege_Init couldn't find market price cap")
                .RemoveInstruction()
                .Insert(new CodeInstruction(OpCodes.Ldc_R4, -1f));

            return codeMatcher.Instructions();
        }
    }

    /// <summary>
    /// Transpile LordToil_Siege.SetAsBuilder to ensure that builders are capable enough to build any siege artillery.
    /// </summary>
    [HarmonyPatch(typeof(LordToil_Siege), nameof(LordToil_Siege.SetAsBuilder))]
    internal static class Harmony_LordToil_Siege_SetAsBuilder
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var codeMatcher = new CodeMatcher(instructions, generator);

            codeMatcher.MatchStartForward(
                    CodeMatch.LoadsField(AccessTools.Field(typeof(ThingDefOf), nameof(ThingDefOf.Turret_Mortar))),
                    CodeMatch.LoadsField(AccessTools.Field(typeof(BuildableDef), nameof(BuildableDef.constructionSkillPrerequisite)))
                    )
                .ThrowIfInvalid("CombatExtended :: Harmony_LordToil_Siege_SetAsBuilder couldn't find required construction skill")
                .RemoveInstructions(2)
                .Insert(CodeInstruction.LoadField(typeof(SiegeUtility), nameof(SiegeUtility.MinRequiredConstructionSkill)));

            return codeMatcher.Instructions();
        }
    }
}
