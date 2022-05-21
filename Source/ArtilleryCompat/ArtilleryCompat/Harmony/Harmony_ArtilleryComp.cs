using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;
using VFESecurity;
using CombatExtended;
using RimWorld;
using Verse.Sound;
using CombatExtended.Utilities;
using RimWorld.Planet;

namespace CombatExtended.Compatibility.Artillery
{
    // TODO: Is this needed?
    /*    [HarmonyPatch(typeof(ArtilleryComp), "TryStartBombardment")]
    public class Harmony_ArtilleryComp_TryStartBombardment {
        public static bool Prefix(ArtilleryComp __instance) {
            if (__instance.CanAttack)
            {
                var parent = __instance.parent;
                var comp = Find.World.GetComponent<WorldArtilleryTracker>();
                comp.RegisterBombardment(parent);
                comp.bombardingWorldObjects.Add(parent);
                __instance.artilleryCooldownTicks = ArtilleryComp.BombardmentStartDelay;
                __instance.bombardmentDurationTicks = __instance.Props.bombardmentDurationRange.RandomInRange;
                __instance.recentRetaliationTicks = __instance.Props.bombardmentCooldownRange.RandomInRange;
                Find.LetterStack.ReceiveLetter("VFESecurity.ArtilleryStrikeSettlement_Letter".Translate(), "VFESecurity.ArtilleryStrikeSettlement_LetterText".Translate(parent.Faction.def.pawnsPlural, parent.Label), LetterDefOf.ThreatBig, parent);
            }
            return false;
        }
    }*/

    [HarmonyPatch(typeof(ArtilleryComp), "BombardmentTick")]
    public class Harmony_ArtilleryComp_BombardmentTick {
        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator gen, IEnumerable<CodeInstruction> instructions)
	{
	    var target = new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BuildingProperties), nameof(BuildingProperties.turretBurstCooldownTime)));
	    List<CodeInstruction> patch = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
		new CodeInstruction(OpCodes.Ldloc, 7),
		new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Utility), nameof(Utility.ArtilleryTick)))//,
            };

            foreach (var code in TranspilerCE.Transpile(gen, instructions, patch, target, offsetCount: 2))
	    {
		yield return code;
	    }
        }
    }
}
