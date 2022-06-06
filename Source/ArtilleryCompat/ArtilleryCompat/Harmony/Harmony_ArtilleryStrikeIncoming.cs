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


namespace CombatExtended.Compatibility.Artillery
{
    [HarmonyPatch(typeof(ArtilleryStrikeIncoming), "SpawnSetup")]
    public class Harmony_ArtilleryStrikeIncoming_SpawnSetup {
        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator gen, IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> patch = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Utility), nameof(Utility.GetProjectileProperties))),
            };
	    foreach (var code in TranspilerCE.Transpile(gen, instructions, patch, new CodeInstruction(OpCodes.Callvirt), cutCount: 1))
	    {
		yield return code;
	    }
        }

    }

    [HarmonyPatch(typeof(ArtilleryStrikeIncoming), "Tick")]
    public class Harmony_ArtilleryStrikeIncoming_Tick {
        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator gen, IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> patch = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Utility), nameof(Utility.GetProjectileProperties))),
            };
	    var target = new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(VFESecurity.ArtilleryStrikeIncoming), nameof(VFESecurity.ArtilleryStrikeIncoming.artilleryShellDef)));
	    foreach (var code in TranspilerCE.Transpile(gen, instructions, patch, target, cutCount: 1, count: 2))
	    {
		yield return code;
	    }
        }
    }

    [HarmonyPatch(typeof(ArtilleryStrikeIncoming), "Impact")]
    public class Harmony_ArtilleryStrikeIncoming_Impact {
        private static ArtilleryStrikeIncoming Impact(ArtilleryStrikeIncoming asi) {
            if (asi.Position.InBounds(asi.Map)) {
                var projectile = (ProjectileCE)ThingMaker.MakeThing(asi.artilleryShellDef.GetProjectile());
		projectile.OffMapOrigin = true;
                GenSpawn.Spawn(projectile, new IntVec3(asi.Position.x, asi.Position.y, asi.Position.z), asi.Map);
		projectile.Launch(asi, new Vector2(asi.Position.x, asi.Position.z));
                projectile.ticksToImpact = 0;
            }
            return asi;
        }
        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator gen, IEnumerable<CodeInstruction> instructions)
        {
            var ilist = instructions.ToList();

            var c = ilist.Count();

            yield return ilist[c-5];
            yield return ilist[c-4];

            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_ArtilleryStrikeIncoming_Impact), nameof(Harmony_ArtilleryStrikeIncoming_Impact.Impact)));

            yield return ilist[c-3];
            yield return ilist[c-2];
            yield return ilist[c-1];

        }

    }

}
