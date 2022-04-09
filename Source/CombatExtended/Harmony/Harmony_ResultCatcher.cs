using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Thing), "TakeDamage")]
    public static class PatchCETakeDamage
    {
        static void Postfix(Thing __instance, DamageWorker.DamageResult __result)
        {
            if (__instance is Pawn compHolder)
            {
                var exploder = compHolder.TryGetComp<CompAmmoExploder>();
                if (exploder != null)
                {
                    exploder.PostDamageResult(__result);
                }
            }
            
        }
    }
}
