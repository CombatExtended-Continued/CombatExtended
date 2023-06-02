using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public static class FleckMakerCE
    {
        public static void Static(IntVec3 cell, Map map, FleckDef fleckDef, float scale = 1f)
        {
            Rand.PushState();
            FleckMaker.Static(cell, map, fleckDef, scale);
            Rand.PopState();
        }

        public static void Static(Vector3 loc, Map map, FleckDef fleckDef, float scale = 1f)
        {
            Rand.PushState();
            FleckMaker.Static(loc, map, fleckDef, scale);
            Rand.PopState();
        }

        public static void ThrowLightningGlow(Vector3 loc, Map map, float size)
        {
            Rand.PushState();
            FleckMaker.ThrowLightningGlow(loc, map, size);
            Rand.PopState();
        }

        public static void WaterSplash(Vector3 loc, Map map, float size, float velocity)
        {
            Rand.PushState();
            FleckMaker.WaterSplash(loc, map, size, velocity);
            Rand.PopState();
        }
    }
}
