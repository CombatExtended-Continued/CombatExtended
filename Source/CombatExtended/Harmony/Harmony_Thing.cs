using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using CombatExtended.Utilities;
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

    [HarmonyPatch(typeof(Thing), nameof(Thing.Position), MethodType.Setter)]
    [HarmonyPriority(Priority.First)]
    public class Harmony_Thing_Position
    {
        private static FieldInfo fPosition = AccessTools.Field(typeof(Thing), "positionInt");

        // The goal is to notify the things tracker of a thing entering a new cell
        // 
        // ]-------------------------------------------------------[
        // The patch target Thing.Postion (setter)
        //
        //if (value == positionInt)
        //{
        //    return;
        //}
        //if (Spawned)
        //{
        //    if (def.AffectsRegions)
        //    {
        //        Log.Warning("Changed position of a spawned thing which affects regions. This is not supported.");
        //    }
        //    DirtyMapMesh(Map);
        //    RegionListersUpdater.DeregisterInRegions(this, Map);
        //    Map.thingGrid.Deregister(this);
        //}
        //positionInt = value;
        // <------------------- Patch insert code here
        //if (Spawned)
        //{
        //    Map.thingGrid.Register(this);
        //    RegionListersUpdater.RegisterInRegions(this, Map);
        //    DirtyMapMesh(Map);
        //    if (def.AffectsReachability)
        //    {
        //        Map.reachability.ClearCache();
        //    }
        //}
        // ]-------------------------------------------------------[
        // The injected code
        //
        // if(this.Spawned)
        // {
        //      ThingsTracker.GetTracker(Map)?.Notify_PositionChanged(this);
        // }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var finished = false;
            var l1 = generator.DefineLabel();
            var l2 = generator.DefineLabel();
            for (int i = 0; i < codes.Count; i++)
            {
                if (!finished)
                {
                    if (codes[i].opcode == OpCodes.Stfld && codes[i].OperandIs(fPosition))
                    {
                        finished = true;
                        yield return codes[i];
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.Spawned)));
                        yield return new CodeInstruction(OpCodes.Brfalse_S, l1);

                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.Map)));
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ThingsTracker), nameof(ThingsTracker.GetTracker)));
                        yield return new CodeInstruction(OpCodes.Dup);
                        yield return new CodeInstruction(OpCodes.Brfalse_S, l2);
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ThingsTracker), nameof(ThingsTracker.Notify_PositionChanged)));

                        yield return new CodeInstruction(OpCodes.Br_S, l1);
                        yield return new CodeInstruction(OpCodes.Pop) { labels = new List<Label>() { l2} };

                        codes[i + 1].labels.Add(l1);
                        continue;
                    }
                }
                yield return codes[i];
            }
        }
    }

    [HarmonyPatch]
    public class Harmony_Thing_DeSpawn
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Thing), nameof(Thing.DeSpawn));
            yield return AccessTools.Method(typeof(Thing), nameof(Thing.ForceSetStateToUnspawned));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [HarmonyPriority(Priority.First)]
        public static void Prefix(Thing __instance)
        {
            ThingsTracker.GetTracker(__instance.Map)?.Notify_DeSpawned(__instance);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.SpawnSetup))]
    public class Harmony_Thing_SpawnSetup
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [HarmonyPriority(Priority.First)]
        public static void Postfix(Thing __instance)
        {
            ThingsTracker.GetTracker(__instance.Map)?.Notify_Spawned(__instance);
        }
    }
}
