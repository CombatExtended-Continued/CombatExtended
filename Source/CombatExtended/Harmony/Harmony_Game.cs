using System;
using CombatExtended.AI;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE;
[HarmonyPatch(typeof(Game), nameof(Game.ExposeData))]
public static class Harmony_Game_ExposeData
{
    public static void Postfix()
    {
        try
        {
            CE_Scriber.ExecuteLateScribe();
        }
        catch (Exception er)
        {
            Log.Error($"CE: Late scriber is really broken {er}!!");
        }
    }
}

[HarmonyPatch(typeof(Game), nameof(Game.LoadGame))]
public static class Harmony_Game_LoadGame
{
    public static void Postfix()
    {
        try
        {
            CE_Scriber.Reset();
        }
        catch (Exception er)
        {
            Log.Error($"CE: Late scriber is really broken {er}!!");
        }
    }
}

[HarmonyPatch(typeof(Game), nameof(Game.ClearCaches))]
public static class Harmony_Game_ClearCaches
{
    public static void Postfix()
    {
        try
        {
            AI_Utility.ResetCache();
        }
        catch (Exception er)
        {
            Log.Error($"CE: Harmony_Game_ClearCaches {er}");
        }
    }
}
