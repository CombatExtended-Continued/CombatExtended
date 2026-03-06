using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Vehicles;
using Verse;

namespace CombatExtended.Compatibility.VehiclesCompat;

/// <summary>
/// Transpiler to inject the projectile-appropriate gravity factor when calculating the shot angle for vehicle turrets.
/// This should be replaced by passing the entire projectile ThingDef to the shot angle callback instead.
/// </summary>
[HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.FireTurretCE))]
public class GravityFactorPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var flyOverheadField = AccessTools.Field(typeof(ProjectileProperties), nameof(ProjectileProperties.flyOverhead));
        var getGravityFactor = AccessTools.Method(typeof(GravityFactorPatch), nameof(GetGravityFactor));
        CodeMatcher codeMatcher = new(instructions);

        return codeMatcher.MatchEndForward(
                new CodeMatch(instr => instr.LoadsField(flyOverheadField)),
                new CodeMatch(OpCodes.Ldc_R4, 1f)
            )
            .RemoveInstruction()
            .Insert(
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, getGravityFactor)
                )
            .Instructions();
    }

    public static float GetGravityFactor(ThingDef projectileDef)
    {
        if (projectileDef.projectile is ProjectilePropertiesCE projectile)
        {
            return projectile.GravityPerWidth;
        }

        return CE_Utility.GravityConst / CE_Utility.MetersPerCellWidth;
    }
}
