using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Thing), "SmeltProducts")]
    public class Harmony_Thing_SmeltProducts
    {
        public static void Postfix(Thing __instance, ref IEnumerable<Thing> __result)
        {
            var ammoUser = (__instance as ThingWithComps)?.TryGetComp<CompAmmoUser>();

            if (ammoUser != null && (ammoUser.HasMagazine && ammoUser.CurMagCount > 0 && ammoUser.CurrentAmmo != null))
            {
                var ammoThing = ThingMaker.MakeThing(ammoUser.CurrentAmmo, null);
                ammoThing.stackCount = ammoUser.CurMagCount;
                __result = __result.AddItem(ammoThing);
            }
        }
    }
}
