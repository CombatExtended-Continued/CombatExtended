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
        private static MethodBase DropThingsNearMethod
        {
            get
            {
                return AccessTools.Method(typeof(DropPodUtility), nameof(DropPodUtility.DropThingsNear));
            }
        }

        public static bool Prepare()
        {
            if (Crashlanded_Patch_HarmonyPatches == null)
            {
                return false;
            }
            if (DropThingsNearMethod == null)
            {
                Log.Error($"Combat Extended :: Failed to find target method while attempting to patch VEE Crashlanded.");
                return false;
            }
            return true;
        }

        public static MethodBase TargetMethod()
        {
            return DropThingsNearMethod;
        }

        public static void Prefix(IEnumerable<Thing> things)
        {
            if (things == null)
            {
                return;
            }
            foreach (Thing thing in things)
            {
                if (thing is Pawn pawn && pawn.Faction is { temporary: true } faction && faction.def == FactionDefOf.Beggars)
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
