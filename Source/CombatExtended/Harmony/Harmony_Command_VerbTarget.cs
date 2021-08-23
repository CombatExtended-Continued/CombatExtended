using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Command_VerbTarget), nameof(Command_VerbTarget.ProcessInput))]
    public class Harmony_Command_VerbTarget
    {
        public static void Prefix(Command_VerbTarget __instance)
        {
            if(__instance?.verb?.EquipmentSource is WeaponPlatform weapon)
                weapon.SelectedVerb = __instance.verb;
        }
    }   
}
