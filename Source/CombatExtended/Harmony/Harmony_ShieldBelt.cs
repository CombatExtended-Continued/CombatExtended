using HarmonyLib;
using Verse;
using RimWorld;

namespace CombatExtended.HarmonyCE
{

    /// <summary>
    /// Prevent using ranged verbs other than binoculars (artillery spotting) for shield belt users.
    /// </summary>
    [HarmonyPatch(typeof(ShieldBelt), nameof(ShieldBelt.AllowVerbCast))]
    internal static class ShieldBelt_PatchAllowVerbCast
    {
        internal static bool Prefix(ref bool __result, Verb verb, ShieldBelt __instance)
        {
            __result = (__instance.ShieldState != ShieldState.Active) || verb is Verb_MarkForArtillery || !(verb is Verb_LaunchProjectileCE || verb is Verb_LaunchProjectile);
            return false;
        }
    }

    [HarmonyPatch(typeof(ShieldBelt), "Tick")]
    internal static class ShieldBelt_DisableOnOperateTurret
    {
        private const int SHORT_SHIELD_RECHARGE_TIME = 2 * GenTicks.TicksPerRealSecond;
        internal static void Postfix(ShieldBelt __instance, ref int ___ticksToReset, int ___StartingTicksToReset)
        {
            if (!Controller.settings.TurretsBreakShields)
            {
                return;
            }
            if (__instance.Wearer?.CurJob?.def == JobDefOf.ManTurret && (__instance.Wearer?.jobs?.curDriver?.OnLastToil ?? false))
            {
                if (__instance.ShieldState == ShieldState.Active)
                {
                    Traverse.Create(__instance).Method("Break").GetValue();
                    ___ticksToReset = SHORT_SHIELD_RECHARGE_TIME;
                }
                if (___ticksToReset < SHORT_SHIELD_RECHARGE_TIME)
                {
                    ___ticksToReset = SHORT_SHIELD_RECHARGE_TIME;
                }
            }
        }
    }
}
