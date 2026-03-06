using RimWorld;

namespace CombatExtended;
/// <summary>
/// CE-specific gas properties for the object gas system.
/// </summary>
/// <remarks>
/// This effectively reimplements the fields that were contained in <see cref="GasProperties"/>
/// prior to the RimWorld 1.4 update, which replaced core object gases with a hardcoded system.
/// </remarks>
public class GasPropertiesCE : GasProperties
{
    /// <summary>
    /// Accuracy penalty multiplier applied by the presence of this gas (0 to 1)
    /// </summary>
    public float accuracyPenalty;
    /// <summary>
    /// Whether automated turrets should be able to track targets across this gas.
    /// </summary>
    public bool blockTurretTracking;
}
