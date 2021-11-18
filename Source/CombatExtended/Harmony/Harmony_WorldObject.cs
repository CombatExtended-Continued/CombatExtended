using System;
using RimWorld.Planet;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE
{
    public static class Harmony_WorldObject
    {
        [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.SpawnSetup))]
        public static class Harmony_WorldObject_SpawnSetup
        {
            public static void Postfix(WorldObject __instance)
            {
                try
                { 
                    if (!__instance.Spawned)
                    {
                        return;
                    }
                    Find.World.GetComponent<WorldObjects.WorldObjectTrackerCE>().TryRegister(__instance);
                }
                catch (Exception er)
                {
                    Log.Error($"CE: Harmony_WorldObject_SpawnSetup {er}");
                }
            }
        }

        [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.Destroy))]
        public static class Harmony_WorldObject_Destroy
        {
            public static void Prefix(WorldObject __instance)
            {               
                try
                {
                    if (!__instance.Spawned)
                    {
                        return;
                    }
                    Find.World.GetComponent<WorldObjects.WorldObjectTrackerCE>().TryDeRegister(__instance);
                }
                catch (Exception er)
                {
                    Log.Error($"CE: Harmony_WorldObject_Destroy {er}");
                }
            }
        }
    }
}

