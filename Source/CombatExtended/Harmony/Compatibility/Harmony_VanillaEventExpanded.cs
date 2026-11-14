using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE.Compatibility;

public class Harmony_Compat_VanillaEventExpanded
{
    private static Type WeaponPod_Patch_HarmonyPatches
    {
        get
        {
            return AccessTools.TypeByName("VEE.RegularEvents.WeaponPod");
        }
    }
    [HarmonyPatch]
    public static class Harmony_WeaponPod_Patch
    {
        public static bool Prepare()
        {
            return WeaponPod_Patch_HarmonyPatches != null;
        }

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("VEE.RegularEvents.WeaponPod:TryExecuteWorker");
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var list = instructions.ToList();
            MethodInfo overrideMethod = typeof(Harmony_WeaponPod_Patch).GetMethod("InsertMethod", BindingFlags.Static | BindingFlags.Public);
            MethodInfo targetMethod = AccessTools.Method(typeof(List<Thing>), nameof(List<Thing>.Add));

            bool foundInjection = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].Calls(targetMethod))
                {
                    continue;
                }
                list.InsertRange(i + 1, [
                    new CodeInstruction(OpCodes.Ldloc_2),
                    new CodeInstruction(OpCodes.Call, overrideMethod)
                ]);
                foundInjection = true;
                break;
            }
            if (!foundInjection)
            {
                Log.Error($"Combat Extended :: Failed to find injection point when applying Patch: {HarmonyBase.GetClassName(MethodBase.GetCurrentMethod()?.DeclaringType)}");
            }
            return list;
        }

        public static void InsertMethod(List<Thing> outThings)
        {
            CE_ThingSetMakerUtility.GenerateAmmoForWeapon(outThings, true, true, new IntRange(1, 3));
        }
    }
    private static Type Crashlanded_Patch_HarmonyPatches
    {
        get
        {
            return AccessTools.TypeByName("VEE.RegularEvents.Crashlanded");
        }
    }
    [HarmonyPatch]
    public static class Harmony_Crashlanded_Patch
    {
        public static bool Prepare()
        {
            return Crashlanded_Patch_HarmonyPatches != null;
        }

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("VEE.RegularEvents.Crashlanded:TryExecuteWorker");
        }

        // VEE spins up one fresh temporary beggar faction per incident, so snapshot what already exists.
        public static void Prefix(out HashSet<int> __state)
        {
            __state = new HashSet<int>(Find.FactionManager.AllFactionsListForReading.Select(f => f.loadID));
        }

        // Postfix means the pods have already landed, pawns are spawned, and VEE has moved them to temp faction.
        public static void Postfix(bool __result, IncidentParms parms, HashSet<int> __state)
        {
            if (!__result || parms.target is not Map map)
            {
                return;
            }
            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                if (faction.def != FactionDefOf.Beggars || !faction.temporary || __state.Contains(faction.loadID))
                {
                    continue;
                }
                foreach (Pawn pawn in map.mapPawns.SpawnedPawnsInFaction(faction))
                {
                    GiveSpareAmmo(pawn);
                }
            }
        }

        public static void GiveSpareAmmo(Pawn pawn)
        {
            var gun = pawn?.equipment?.Primary;
            if (gun == null)
            {
                return;
            }

            CompAmmoUser ammoUser = gun.TryGetComp<CompAmmoUser>();
            if (ammoUser is not { UseAmmo: true })
            {
                return;
            }

            // Skip if already have ammo
            if (pawn.inventory.innerContainer.Any(thing => ammoUser.Props.ammoSet.ammoTypes.Any(link => link.ammo == thing.def)))
            {
                return;
            }

            // Magazines, not rounds. Bolt-action mag is 10 -> 100 rounds, autopistol mag is 7 -> 56.
            int mags = gun.def.defName == "Gun_BoltActionRifle" ? 10 : 8;

            var things = new List<Thing> { gun };
            CE_ThingSetMakerUtility.GenerateAmmoForWeapon(things, false, false, new IntRange(mags, mags));

            foreach (Thing thing in things)
            {
                if (thing == gun)
                {
                    continue;
                }
                pawn.inventory.TryAddAndUnforbid(thing);
            }
        }
    }

}
