using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Multiplayer.API;
using Verse;

namespace CombatExtended.Compatibility
{
    internal class Multiplayer
    {
        private static bool isMultiplayerActive = false;

        public static bool CanInstall()
        {
            return ModLister.HasActiveModWithName("Multiplayer");
        }

        public static void Install()
        {
            isMultiplayerActive = true;

            var methods = Controller.content.assemblies.loadedAssemblies
                .SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                .Where(m => !m.IsAbstract)
                .Where(m => m.HasAttribute<SyncMethodAttribute>());

            //MP.RegisterSyncMethod(typeof(Building_TurretGunCE), nameof(Building_TurretGunCE.OrderAttack));
            foreach (var method in methods)
            {
                if (!method.TryGetAttribute<SyncMethodAttribute>(out var attribute)) continue;

                var syncMethod = MP.RegisterSyncMethod(method);
                
                if (attribute.syncContext != -1)
                    syncMethod.SetContext((SyncContext)attribute.syncContext);

                if (attribute.exposeParameters == null) continue;

                foreach (var parameter in attribute.exposeParameters)
                    syncMethod.ExposeParameter(parameter);
            }

            MP.RegisterAll();
        }

        public static bool InMultiplayer
        {
            get
            {
                if (isMultiplayerActive)
                    return _inMultiplayer;
                return false;
            }
        }

        public static bool IsExecutingCommandsIssuedBySelf
        {
            get
            {
                if (isMultiplayerActive)
                    return _isExecutingCommandsIssuedBySelf;
                return false;
            }
        }

        private static bool _inMultiplayer => MP.IsInMultiplayer;

        private static bool _isExecutingCommandsIssuedBySelf => MP.IsExecutingSyncCommandIssuedBySelf;

        [SyncWorker]
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
                    comp = sync.Read<Building_TurretGunCE>().CompAmmo;
                else
                    comp = sync.Read<ThingComp>() as CompAmmoUser;
            }
        }

        [SyncWorker]
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
                    comp = sync.Read<Building_TurretGunCE>().CompFireModes;
                else
                    comp = sync.Read<ThingComp>() as CompFireModes;
            }
        }

        [SyncWorker]
        private static void SyncLoadout(SyncWorker sync, ref Loadout loadout)
        {
            if (sync.isWriting)
                sync.Write(LoadoutManager.Loadouts.IndexOf(loadout));
            else
            {
                var index = sync.Read<int>();
                if (index >= 0)
                    loadout = LoadoutManager.Loadouts[index];
            }
        }

        [SyncWorker]
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
                        break;
                }

                if (slotIndex >= 0)
                {
                    sync.Write(loadoutIndex);
                    sync.Write(slotIndex);
                }
                else
                    sync.Write(-1);
            }
            else
            {
                var loadoutIndex = sync.Read<int>();
                if (loadoutIndex < 0)
                    return;

                loadoutSlot = LoadoutManager.Loadouts[loadoutIndex].Slots[sync.Read<int>()];
            }
        }

        // Don't sync anything, we just want a blank instance for method calling purposes
        // We only care about shouldConstruct being true
        [SyncWorker(shouldConstruct = true)]
        private static void SyncITab_Inventory(SyncWorker sync, ref ITab_Inventory inventory)
        { }

        [AttributeUsage(AttributeTargets.Method)]
        public class SyncMethodAttribute : Attribute
        {
            public int syncContext = -1;
            public int[] exposeParameters = null;
        }
    }
}
