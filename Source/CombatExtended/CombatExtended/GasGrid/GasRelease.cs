using Verse;

namespace CombatExtended;

/// <summary>
/// Gas an explosion releases, set as <c>postExplosionCEGas</c> on <see cref="CompProperties_ExplosiveCE"/> or
/// <see cref="ProjectilePropertiesCE"/>.
/// </summary>
public class GasRelease
{
    public GasDef gas;

    /// <summary>Density added to every cell the gas reaches. A cell holds at most the gas's <see cref="GasDef.maxDensity"/>.</summary>
    public float densityPerCell = 1000f;

    /// <summary>Radius of the cloud in cells. 0 uses the radius of the explosion.</summary>
    public float radius = 0f;

    public void Release(IntVec3 center, Map map, float explosionRadius)
    {
        CEGasGrid grid = map.GetComponent<CEGasGrid>();
        float cloudRadius = radius > 0f ? radius : explosionRadius;

        foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, cloudRadius, true))
        {
            // Walls stop the cloud, so it fills the room it went off in instead of leaking through them.
            bool reachesCell = cell.InBounds(map)
                               && grid.CanMoveTo(map.cellIndices.CellToIndex(cell))
                               && GenSight.LineOfSight(center, cell, map, skipFirstCell: true);
            if (reachesCell)
            {
                grid.AddDensity(cell, gas, densityPerCell);
            }
        }
    }
}
