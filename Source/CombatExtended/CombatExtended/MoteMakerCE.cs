using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public static class MoteMakerCE
    {
        public static void ThrowText(Vector3 loc, Map map, string text, float timeBeforeStartFadeout = -1f)
        {
            Rand.PushState();
            MoteMaker.ThrowText(loc, map, text, timeBeforeStartFadeout);
            Rand.PopState();
        }

        public static void ThrowText(Vector3 loc,  Map map, string text, Color color, float timeBeforeStartFadeout = -1f)
        {
            Rand.PushState();
            MoteMaker.ThrowText(loc, map, text, color, timeBeforeStartFadeout);
            Rand.PopState();
        }
    }
}
