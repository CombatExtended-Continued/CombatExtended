using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended.CombatExtended
{
    [StaticConstructorOnStartup]
    public static class HideTacticalVestsAndBackpacksPostfix
    {
        static HideTacticalVestsAndBackpacksPostfix()
        {
            var harmony = new Harmony("com.combatextended.hidetacticalvestandbackpackspostfix");

            var original = typeof(PawnRenderNodeWorker_Body).GetMethod(
                "CanDrawNow",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
            );

            if (original != null)
            {
                var postfix = typeof(HideTacticalVestsAndBackpacksPostfix).GetMethod(nameof(CanDrawNowPostfix));
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            }
        }

        // Postfix
        public static void CanDrawNowPostfix(PawnRenderNode node, PawnDrawParms parms, ref bool __result)
        {
            if (node?.apparel?.def == null) { return; }
            string defName = node.apparel.def.defName;
            if ((defName == "CE_Apparel_TacVest" && !Controller.settings.ShowTacticalVests) ||
                (defName == "CE_Apparel_Backpack" && !Controller.settings.ShowBackpacks))
            {
                __result = false;
            }
        }
    }
}
