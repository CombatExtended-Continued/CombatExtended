using CombatExtended;
using Verse;
#nullable enable
namespace CombatExtended.Compatibility.VFES
{
    // submerged turret cover/height fixes only. pathfinding's VFE's problem
    // (PassThroughOnly in the def, pawns just walk through). height zeroing
    // lives in CollisionVertical.
    internal static class ConcealedTurretCE_Patches
    {
        internal static bool IsSubmerged(Thing t)
        {
            CompConcealedCE? comp = t.TryGetComp<CompConcealedCE>();
            return comp != null && comp.Submerged;
        }
    }
}
#nullable restore
