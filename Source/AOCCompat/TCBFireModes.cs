using CombatExtended;
using HarmonyLib;

namespace AOC17CE
{
    // CE normally exposes burst only below the automatic burst length. TCB has
    // exactly two modes. Also keep KILLAKILL's requested Single/Burst/Auto order;
    // its actual 1/2/5 shot counts still come from CE and the XML properties.
    [HarmonyPatch(typeof(CompFireModes), nameof(CompFireModes.InitAvailableFireModes))]
    public static class TCBTwoRoundBurstCE
    {
        public static bool Prefix(CompFireModes __instance, ref bool ___newComp)
        {
            string name = __instance.parent?.def?.defName;
            bool killaKill = name == "AOC_KILLAKILL";
            if (name != "TCB_26K" && !killaKill) return true;

            var modes = __instance.AvailableFireModes;
            modes.Clear();
            modes.Add(FireMode.SingleFire);
            modes.Add(FireMode.BurstFire);
            if (killaKill) modes.Add(FireMode.AutoFire);

            var aims = __instance.AvailableAimModes;
            aims.Clear();
            aims.Add(AimMode.AimedShot);
            if (!__instance.Props.noSnapshot)
            {
                aims.Add(AimMode.Snapshot);
                aims.Add(AimMode.SuppressFire);
            }

            // Keep valid saved selections. Reset new/obsolete selections using
            // CE's normal default handling; this also migrates an old AutoFire.
            if (___newComp || !modes.Contains(__instance.CurrentFireMode) ||
                !aims.Contains(__instance.CurrentAimMode))
            {
                ___newComp = false;
                __instance.ResetModes();
            }
            return false;
        }
    }
}
