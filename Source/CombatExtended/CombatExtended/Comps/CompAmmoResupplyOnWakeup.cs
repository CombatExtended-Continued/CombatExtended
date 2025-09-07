using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI.Group;
using System.Reflection;
using CombatExtended.Compatibility;

namespace CombatExtended
{
    public class CompAmmoResupplyOnWakeup : ThingComp
    {
        /// <summary>
        /// The radius in which to scan for turrets to resupply with ammo.
        /// </summary>
        private const float MaxResupplyRadius = 40;

        /// <summary>
        /// The radius around a given turret in which to search for corresponding ammo.
        /// </summary>
        private const float AmmoSearchRadius = 40;

        const int ticksBetweenChecks = 600;    //Divide by 60 for seconds

        public CompProperties_AmmoResupplyOnWakeup Props => (CompProperties_AmmoResupplyOnWakeup)props;

        /// <summary>
        /// Cached reference to the <see cref="Lord"/> of the mech cluster that the owner of this comp is part of.
        /// </summary>
        private Lord lord;

        /// <summary>
        /// Determine whether this beacon should be supplying ammo.
        /// </summary>
        public bool IsActive
        {
            get
            {
                // No point in supplying ammo if it's not enabled
                if (!Controller.settings.EnableAmmoSystem)
                {
                    return false;
                }

                if (!(parent.TryGetComp<CompCanBeDormant>()?.Awake ?? true))
                {
                    return false;
                }

                // Ensure this beacon does not provide an infinite source of ammo
                // if there are no remaining mechs that could use it to reload turrets.
                return lord?.AnyActivePawn ?? false;
            }
        }

        /// <summary>
        /// Find the owning <see cref="Lord"/> (i.e. mech cluster) for this beacon after spawning.
        /// </summary>
        /// <remarks>
        /// Ammo beacons are not considered to be threats like turrets or assemblers and so are not directly owned
        /// by the <see cref="Lord"/> of the parent cluster; it is therefore necessary to find their owner manually.
        /// </remarks>
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            var faction = parent.Faction;
            foreach (var lord in parent.Map.lordManager.lords)
            {
                if (lord.faction == faction && lord.LordJob is LordJob_MechanoidDefendBase mechDefendBaseJob)
                {
                    if (mechDefendBaseJob.isMechCluster && mechDefendBaseJob.things.Contains(parent))
                    {
                        this.lord = lord;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Determine if the amount of ammo in this turret's magazine and on the ground around it
        /// is sufficient to not require dropping in further ammo supplies.
        /// </summary>
        /// <remarks>
        /// This checks if the cumulative ammo count is at least half the magazine capacity,
        /// which is the same threshold that triggers the turret reload job.
        /// </remarks>
        public bool EnoughAmmoAround(Building_TurretGunCE turret)
        {
            // Prevent ammo being dropped if the turret is being reloaded at the time
            // or has enough ammo in its magazine
            if (turret.GetReloading() || !turret.ShouldReload(JobGiver_DefenderReloadTurret.AmmoReloadThreshold))
            {
                return true;
            }

            var ammoComp = turret.CompAmmo;

            int numCells = GenRadial.NumCellsInRadius(AmmoSearchRadius);
            int availableAmmo = ammoComp.CurMagCount;
            int minRequiredAmmoCount = Mathf.CeilToInt(ammoComp.MagSize * JobGiver_DefenderReloadTurret.AmmoReloadThreshold);

            var map = parent.Map;

            for (int i = 0; i < numCells; i++)
            {
                IntVec3 c = parent.Position + GenRadial.RadialPattern[i];
                if (c.InBounds(map))
                {
                    List<Thing> thingList = map.thingGrid.ThingsListAtFast(c);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        if (ammoComp.CurrentAmmo == thingList[j].def)
                        {
                            availableAmmo += thingList[j].stackCount;

                            if (availableAmmo > minRequiredAmmoCount)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public override void CompTick()
        {
            if (parent.IsHashIntervalTick(ticksBetweenChecks))
            {
                TickRareWorker();
            }
        }

        public void TickRareWorker()
        {
            if (!IsActive)
            {
                return;
            }

            var turretTracker = parent.Map.GetComponent<TurretTracker>();
            var beaconFaction = parent.Faction;
            var beaconPos = parent.Position;

            foreach (var building in turretTracker.Turrets)
            {
                if (building is Building_TurretGunCE turret &&
                        turret.Faction == beaconFaction &&
                        !turret.def.building.IsMortar &&
                        (turret.CompAmmo?.UseAmmo ?? false) &&
                        turret.Position.InHorDistOf(beaconPos, MaxResupplyRadius))
                {
                    if (EnoughAmmoAround(turret))
                    {
                        continue;
                    }
                    if (turret.CompAmmo.CurrentAmmo != null)
                    {
                        DropSupplies(turret.CompAmmo.CurrentAmmo, Mathf.CeilToInt(0.5f * (float)turret.CompAmmo.MagSize), turret.Position);
                    }
                }
            }
        }

        private void DropSupplies(ThingDef thingDef, int count, IntVec3 cell)
        {
            List<Thing> list = new List<Thing>();
            Thing thing = ThingMaker.MakeThing(thingDef, null);
            thing.stackCount = count;
            list.Add(thing);

            //If turrets are near-empty or empty, call in ammo droppod
            DropPodUtility.DropThingsNear(
                cell,
                parent.Map,
                list,
                openDelay: 110,
                canInstaDropDuringInit: false,
                leaveSlag: false,
                canRoofPunch: true,
                forbid: true,
                // Use the mechanoid faction as the faction for the drop pods, to match style
                faction: parent.Faction
            );
        }
    }
}
