using Harmony;
using Verse;
using RimWorld;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(ShieldBelt), "AllowVerbCast")]
    internal static class ShieldBelt_PatchAllowVerbCast
    {
        internal static void Postfix(ref bool __result, Verb verb)
        {
            if (__result)
            {
                __result = !(verb is Verb_LaunchProjectileCE);
            }
        }
    }

    [HarmonyPatch(typeof(ShieldBelt), "Tick")]
    internal static class ShieldBelt_DisableOnOperateTurret
    {
        internal static void Postfix(ShieldBelt __instance, ref int ___ticksToReset, int ___StartingTicksToReset)
        {
            if (__instance.Wearer?.CurJob?.def == JobDefOf.ManTurret && (__instance.Wearer?.jobs?.curDriver?.OnLastToil ?? false))
            {
                if (__instance.ShieldState == ShieldState.Active)
                {
                    Traverse.Create(__instance).Method("Break").GetValue();
                }
                ___ticksToReset = ___StartingTicksToReset;
            }
        }
    }
}