using Multiplayer.API;
using RimWorld;
using Verse;

namespace CombatExtended.Compatibility
{
    internal class Multiplayer
    {
        public static bool CanInstall()
        {
            return MP.enabled;
        }

        public static void Install()
        {
            MP.RegisterAll();
        }

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
                    sync.Write(comp);
                }
            }
            else
            {
                if (sync.Read<bool>())
                    comp = sync.Read<Building_TurretGunCE>().CompAmmo;
                else
                    comp = sync.Read<CompAmmoUser>();
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
                    sync.Write(comp);
                }
            }
            else
            {
                if (sync.Read<bool>())
                    comp = sync.Read<Building_TurretGunCE>().CompFireModes;
                else
                    comp = sync.Read<CompFireModes>();
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
    }
}
