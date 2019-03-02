using Harmony;
using Verse;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(RimWorld.ShieldBelt), "AllowVerbCast")]
    internal static class ShieldBelt
    {
        internal static void Postfix(ref bool __result, Verb verb)
        {
            if (__result)
            {
                __result = !(verb is Verb_LaunchProjectileCE);
            }
        }
    }
}