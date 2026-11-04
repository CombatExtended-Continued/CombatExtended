using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended;

// Migrates legacy smoke to new gas grid system
// Remove later after we are confident bulk of users have been migrated
public class Smoke : Gas
{
    private float density;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref density, "density");
    }

    public override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
    }

    public override void TickInterval(int delta)
    {
        if (Spawned)
        {
            Map.GetComponent<CEGasGrid>().AddDensity(Position, CE_GasDefOf.CE_BlackSmoke, density);
        }
        density = 0f;
        Destroy();
    }
}
