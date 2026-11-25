using System;
using System.Globalization;
using AOCTCB;
using CombatExtended;
using HarmonyLib;
using Verse;

namespace AOC17CE
{
    // CE's normal readout cannot see AOC's map-component delayed explosion.
    // Append it only for this weapon's projectile, reading the same settings as
    // Bullet_DelayedTCBCE rather than treating shared slug ammo as explosive.
    [HarmonyPatch(typeof(AmmoUtility), nameof(AmmoUtility.GetProjectileReadout),
        new[] { typeof(ThingDef), typeof(Thing) })]
    public static class TCBDelayedBlastReadout
    {
        public static void Postfix(ThingDef __0, ref string __result)
        {
            if (__0?.defName != "AOC17CE_TCBAP")
            {
                return;
            }

            var settings = __0.GetModExtension<DelayedBlastExtension>() ?? new DelayedBlastExtension();
            string delay = (Math.Max(1, settings.delayTicks) / 60f).ToString("0.##", CultureInfo.CurrentCulture);
            string radius = Math.Max(0, settings.radius).ToString("0.##", CultureInfo.CurrentCulture);
            string extra = "AOC17CE_TCBDelayedBlastReadout".Translate(delay,
                Math.Max(0, settings.damage), radius);
            __result = (__result ?? "").TrimEnd() + "\n\n" + extra;
        }
    }
}
