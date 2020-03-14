using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI.Group;
using System.Reflection;

namespace CombatExtended
{
    public class CompAmmoResupplyOnWakeup : ThingComp
    {
        public HashSet<Thing> nearbyTurrets = new HashSet<Thing>();
        const int turretsWithinDistanceSqr = 100;

        public CompProperties_AmmoResupplyOnWakeup Props => (CompProperties_AmmoResupplyOnWakeup)props;
        
        //Only do something if not dormant
        //Only do something when ammo system is enabled
        public bool IsActive => Controller.settings.EnableAmmoSystem
            && (parent.TryGetComp<CompCanBeDormant>()?.Awake ?? false);

        FieldInfo thingsField = typeof(LordJob_MechanoidDefendBase).GetField("things");
        FieldInfo mechClusterDefeatedField = typeof(LordJob_MechanoidDefendBase).GetField("mechClusterDefeated");
        LordJob_MechanoidDefendBase parentLordJob => ((parent as Building)?.GetLord()?.LordJob as LordJob_MechanoidDefendBase);
        public bool OwnerOnly
        {
            get
            {
                var lordJob = parentLordJob;

                if (lordJob == null)
                    return false;

                return !(bool)mechClusterDefeatedField.GetValue(lordJob);
            }
        }

        public bool EnoughAmmoAround(Thing thing)
        {
            var ammoComp = (thing as Building_TurretGunCE).CompAmmo;

            int num = GenRadial.NumCellsInRadius(20f);
            int num2 = ammoComp.CurMagCount;
            int num3 = Mathf.CeilToInt((float)ammoComp.Props.magazineSize / 6f);

            if (num2 > num3)
                return true;

            for (int i = 0; i < num; i++)
            {
                IntVec3 c = parent.Position + GenRadial.RadialPattern[i];
                if (c.InBounds(parent.Map))
                {
                    List<Thing> thingList = c.GetThingList(parent.Map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        if ((ammoComp != null && ammoComp.CurrentAmmo == thingList[j].def)
                            || thingList[j].def.IsShell)
                        {
                            num2 += thingList[j].stackCount;

                            if (num2 > num3)
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        void UpdateNearbyTurrets()
        {
            nearbyTurrets.Clear();

            if (OwnerOnly)
            {
                nearbyTurrets.AddRange(((List<Thing>)thingsField.GetValue(parentLordJob))
                    .Where(x => x.def.building.IsTurret && !x.def.building.IsMortar
                        //ONLY supply nearby turrets which are Building_TurretGunCE
                        && ((x as Building_TurretGunCE)?.CompAmmo?.UseAmmo ?? false)));
            }
            else
            {
                nearbyTurrets.AddRange(parent.Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
                    .Where(x => x.def.building.IsTurret && !x.def.building.IsMortar
                        //ONLY supply nearby turrets which are Building_TurretGunCE
                        && ((x as Building_TurretGunCE)?.CompAmmo?.UseAmmo ?? false)
                        && x.Position.DistanceToSquared(parent.Position) < turretsWithinDistanceSqr));
            }
        }

        public override void CompTickRare()
        {
            if (!IsActive)
                return;

            UpdateNearbyTurrets();

            foreach (var turret in nearbyTurrets)
            {
                if (EnoughAmmoAround(turret))
                    continue;
                
                ThingDef thingDef = (turret as Building_TurretGunCE).CompAmmo.CurrentAmmo;

                if (thingDef != null)
                {
                    DropSupplies(thingDef, 10, turret.Position);
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
            DropPodUtility.DropThingsNear(cell, parent.Map, list, 110, false, false, true, true);
        }
    }
}
