using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended.Compatibility.WeaponProficiency.HarmonyPatches
{
    [StaticConstructorOnStartup]
    public static class HarmonyStarter
    {
        static HarmonyStarter()
        {
            Harmony harmony = new Harmony("CombatExtended.Compatibility.WeaponProficiency");
            harmony.PatchAll();
        }
    }
}
