using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE;
[HarmonyPatch(typeof(QuestPart_Notify_PlayerRaidedSomeone), nameof(QuestPart_Notify_PlayerRaidedSomeone.Notify_QuestSignalReceived))]
internal static class Harmony_QuestPart_Notify_PlayerRaidedSomeone
{
    internal static bool Prefix(Signal signal, QuestPart_Notify_PlayerRaidedSomeone __instance)
    {
        if (signal.tag == __instance.inSignal && signal.args.TryGetArg("MAP", out Map map))
        {
            IdeoUtility.Notify_PlayerRaidedSomeone(map.mapPawns.FreeColonistsSpawned);
            return false; //potentially destructive patch...
        }
        return true;
    }
}
