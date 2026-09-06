using CombatExtended;
using HarmonyLib;
using Verse;
#nullable enable
namespace CombatExtended.Compatibility.VFES
{
    // cover / shot-height tweaks for submerged turrets. we don't touch
    // pathfinding (VFE does that via PassThroughOnly in the def, pawns just
    // walk through). we only fix the shooty bits: a hidden turret shouldn't
    // hand out cover or block shots.
    internal static class ConcealedTurretCE_Patches
    {
        private static bool IsSubmerged(Thing t)
        {
            CompConcealedCE? comp = t.TryGetComp<CompConcealedCE>();
            return comp != null && comp.Submerged;
        }

        // base cover chance, used by vanilla and CE.
        [HarmonyPatch(typeof(CoverUtility), "BaseBlockChance", new[] { typeof(Thing) })]
        internal static class BaseBlockChance_Patch
        {
            private static bool Prefix(Thing thing, ref float __result)
            {
                if (IsSubmerged(thing))
                {
                    __result = 0f;
                    return false;
                }
                return true;
            }
        }

        // CE works out height from def.fillPercent / Fillage. zero it when
        // submerged so bullets just fly over the thing instead of hitting floor.
        [HarmonyPatch(typeof(CollisionVertical), "CalculateHeightRange")]
        internal static class CollisionVerticalHeight_Patch
        {
            private static bool Prefix(Thing thing, out FloatRange heightRange, out float shotHeight)
            {
                if (IsSubmerged(thing))
                {
                    heightRange = new FloatRange(0f, 0f);
                    shotHeight = 0f;
                    return false;
                }
                heightRange = default;
                shotHeight = 0f;
                return true;
            }
        }
    }
}
#nullable restore
