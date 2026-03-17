using CombatExtended.Loader;
using HarmonyLib;
using Multiplayer.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using SyncMethodAttribute = global::CombatExtended.Compatibility.Multiplayer.SyncMethodAttribute;

namespace CombatExtended.Compatibility.MultiplayerAPI;
public class MultiplayerCompat : IModPart
{
    public MultiplayerCompat() { }

    public static ISyncField SyncGizmoAmmoStatusTryReloadOn;
    //public static ISyncField SyncGizmoAmmoStatusGizmoSliderPct;

    private static Harmony harmony = null;

    public Type GetSettingsType()
    {
        return null;

    }

    public IEnumerable<string> GetCompatList()
    {
        yield break;
    }

    public void PostLoad(ModContentPack content, ISettingsCE _)
    {
        harmony = new Harmony("MultiplayerCompat.HarmonyCE");
        LongEventHandler.QueueLongEvent(() => this.SlowInit(content), "CE_LongEvent_CompatibilityPatches", false, null);
    }
    public void SlowInit(ModContentPack content)
    {
        var methods = content.assemblies.loadedAssemblies
                      .SelectMany(a => a.GetTypes())
                      .SelectMany(t => t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                      .Where(m => !m.IsAbstract)
                      .Where(m => m.HasAttribute<SyncMethodAttribute>());

        //MP.RegisterSyncMethod(typeof(Building_TurretGunCE), nameof(Building_TurretGunCE.OrderAttack));
        foreach (var method in methods)
        {
            if (!method.TryGetAttribute<SyncMethodAttribute>(out var attribute))
            {
                continue;
            }

            var syncMethod = MP.RegisterSyncMethod(method);

            if (attribute.syncContext != -1)
            {
                syncMethod.SetContext((SyncContext)attribute.syncContext);
            }

            if (attribute.exposeParameters == null)
            {
                continue;
            }

            foreach (var parameter in attribute.exposeParameters)
            {
                syncMethod.ExposeParameter(parameter);
            }
        }
        SyncGizmoAmmoStatusTryReloadOn = MP.RegisterSyncField(AccessTools.Field(typeof(CompAmmoUser), "tryReloadOn")).SetBufferChanges();
        //SyncGizmoAmmoStatusGizmoSliderPct = MP.RegisterSyncField(AccessTools.Field(typeof(GizmoAmmoStatus), "targetValuePct")).SetBufferChanges();

        var prefix = new HarmonyMethod(typeof(Patch_GizmoAmmoStatusGizmoOnGUI), nameof(Patch_GizmoAmmoStatusGizmoOnGUI.Prefix));
        var postfix = new HarmonyMethod(typeof(Patch_GizmoAmmoStatusGizmoOnGUI), nameof(Patch_GizmoAmmoStatusGizmoOnGUI.Postfix));
        harmony.Patch(AccessTools.Method(typeof(GizmoAmmoStatus), nameof(GizmoAmmoStatus.GizmoOnGUI)), prefix: prefix, postfix: postfix);


        Type type = typeof(GizmoAmmoStatus);
        MP.RegisterSyncWorker<GizmoAmmoStatus>(SyncGizmoAmmoStatus, type);

        type = typeof(CompAmmoUser);
        MP.RegisterSyncWorker<CompAmmoUser>(SyncCompAmmoUser, type);

        type = typeof(CompFireModes);
        MP.RegisterSyncWorker<CompFireModes>(SyncCompFireMode, type);

        type = typeof(Loadout);
        MP.RegisterSyncWorker<Loadout>(SyncLoadout, type);

        type = typeof(LoadoutSlot);
        MP.RegisterSyncWorker<LoadoutSlot>(SyncLoadoutSlot, type);

        type = typeof(ITab_Inventory);
        MP.RegisterSyncWorker<ITab_Inventory>(SyncITab_Inventory, type, shouldConstruct: true);


        MP.RegisterAll();



        global::CombatExtended.Compatibility.Multiplayer.registerCallbacks((() => MP.IsInMultiplayer), (() => MP.IsExecutingSyncCommand), (() => MP.IsExecutingSyncCommandIssuedBySelf));
    }

    public static class Patch_GizmoAmmoStatusGizmoOnGUI
    {
        public static void Prefix(GizmoAmmoStatus __instance)
        {
            MP.WatchBegin();

            // SyncGizmoAmmoStatusGizmoSliderPct.Watch(__instance);
            SyncGizmoAmmoStatusTryReloadOn.Watch(__instance.compAmmo);
        }
        public static void Postfix(GizmoAmmoStatus __instance)
        {
            MP.WatchEnd();
        }
    }

    //[SyncWorker]
    private static void SyncGizmoAmmoStatus(SyncWorker sync, ref GizmoAmmoStatus gizmo)
    {
        if (sync.isWriting)
        {
            sync.Write<CompAmmoUser>(gizmo.compAmmo);
        }
        else
        {
            var compAmmo = sync.Read<CompAmmoUser>();
            // Need some help fix pct sync
            gizmo = new GizmoAmmoStatus { compAmmo = compAmmo };
        }
    }

    //[SyncWorker]
    private static void SyncCompAmmoUser(SyncWorker sync, ref CompAmmoUser comp)
    {
        if (sync.isWriting)
        {
            var caster = comp.parent.GetComp<CompEquippable>().PrimaryVerb.Caster;

            // Sync the turret because in that case syncing fails, due to comp.parent.Map being null,
            // which causes it to be inaccessible in MP for general syncing
            if (caster is Building_TurretGunCE turret)
            {
                sync.Write(true);
                sync.Write(turret);
            }
            // Sync the comp itself, as the parent is accessible in MP
            else
            {
                sync.Write(false);
                // Sync using ThingComp worker, not this one
                // Prevents infinite iteration
                sync.Write(comp as ThingComp);
            }
        }
        else
        {
            if (sync.Read<bool>())
            {
                comp = sync.Read<Building_TurretGunCE>().CompAmmo;
            }
            else
            {
                comp = sync.Read<ThingComp>() as CompAmmoUser;
            }
        }
    }

    //[SyncWorker]
    private static void SyncCompFireMode(SyncWorker sync, ref CompFireModes comp)
    {
        if (sync.isWriting)
        {
            var caster = comp.Caster;

            // Sync the turret because in that case syncing fails, due to comp.parent.Map being null,
            // which causes it to be inaccessible in MP for general syncing
            if (caster is Building_TurretGunCE turret)
            {
                sync.Write(true);
                sync.Write(turret);
            }
            // Sync the comp itself, as the parent is accessible in MP
            else
            {
                sync.Write(false);
                // Sync using ThingComp worker, not this one
                // Prevents infinite iteration
                sync.Write(comp as ThingComp);
            }
        }
        else
        {
            if (sync.Read<bool>())
            {
                comp = sync.Read<Building_TurretGunCE>().CompFireModes;
            }
            else
            {
                comp = sync.Read<ThingComp>() as CompFireModes;
            }
        }
    }

    //[SyncWorker]
    private static void SyncLoadout(SyncWorker sync, ref Loadout loadout)
    {
        if (sync.isWriting)
        {
            sync.Write(loadout.UniqueID);
        }
        else
        {
            var id = sync.Read<int>();
            loadout = LoadoutManager.GetLoadoutById(id);
        }
    }

    //[SyncWorker(shouldConstruct = true)]
    private static void SyncLoadoutSlot(SyncWorker sync, ref LoadoutSlot loadoutSlot)
    {
        if (sync.isWriting)
        {
            var loadoutIndex = 0;
            var slotIndex = -1;

            var list = LoadoutManager.Loadouts;
            for (; loadoutIndex < list.Count; loadoutIndex++)
            {
                var value = list[loadoutIndex];
                slotIndex = value.Slots.IndexOf(loadoutSlot);
                if (slotIndex >= 0)
                {
                    break;
                }
            }

            if (slotIndex >= 0)
            {
                sync.Write(loadoutIndex);
                sync.Write(slotIndex);
            }
            else
            {
                sync.Write(-1);
            }
        }
        else
        {
            var loadoutIndex = sync.Read<int>();
            if (loadoutIndex < 0)
            {
                return;
            }

            loadoutSlot = LoadoutManager.Loadouts[loadoutIndex].Slots[sync.Read<int>()];
        }
    }

    // Don't sync anything, we just want a blank instance for method calling purposes
    // We only care about shouldConstruct being true
    //[SyncWorker(shouldConstruct = true)]
    private static void SyncITab_Inventory(SyncWorker sync, ref ITab_Inventory inventory)
    { }
}
