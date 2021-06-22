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
        public HashSet<Building_TurretGunCE> nearbyTurrets = new HashSet<Building_TurretGunCE>();
        const int turretsWithinDistanceSqr = 100;
        const int ticksBetweenChecks = 600;    //Divide by 60 for seconds

        public CompProperties_AmmoResupplyOnWakeup Props => (CompProperties_AmmoResupplyOnWakeup)props;
        
        //Only do something if not dormant
        //Only do something when ammo system is enabled
        public bool IsActive => Controller.settings.EnableAmmoSystem && (parent.TryGetComp<CompCanBeDormant>()?.Awake ?? true);

        //FieldInfo thingsField = typeof(LordJob_MechanoidDefendBase).GetField("things");
        FieldInfo mechClusterDefeatedField = typeof(LordJob_MechanoidDefendBase).GetField("mechClusterDefeated");
        LordJob_MechanoidDefendBase parentLordJob => ((parent as Building)?.GetLord()?.LordJob as LordJob_MechanoidDefendBase);
        public bool ClusterAlive
        {
            get
            {
                var lordJob = parentLordJob;

                if (lordJob == null)
                    return false;

                return !(bool)mechClusterDefeatedField.GetValue(lordJob);
            }
        }

        public bool EnoughAmmoAround(Building_TurretGunCE turret)
        {
            //Prevent ammo being dropped as a result of the turret being reloaded at the time
            if (turret.GetReloading() || turret.ShouldReload(1.0f))
                return true;

            var ammoComp = turret.CompAmmo;
            
            int num = GenRadial.NumCellsInRadius(20f);
            int num2 = ammoComp.CurMagCount;
            int num3 = Mathf.CeilToInt((float)ammoComp.Props.magazineSize / 6f);
            
            for (int i = 0; i < num; i++)
            {
                IntVec3 c = parent.Position + GenRadial.RadialPattern[i];
                if (c.InBounds(parent.Map))
                {
                    List<Thing> thingList = c.GetThingList(parent.Map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        if ((ammoComp != null && ammoComp.CurrentAmmo == thingList[j].def))
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
                return;
            
            foreach (var turret in parent.Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
                    .Where(x => (x.def?.building?.IsTurret ?? false) && !x.def.building.IsMortar
                        //ONLY supply nearby turrets which are Building_TurretGunCE
                        && ((x as Building_TurretGunCE)?.CompAmmo?.UseAmmo ?? false)
                        && x.Faction?.def == FactionDefOf.Mechanoid
                        && x.Position.DistanceToSquared(parent.Position) < turretsWithinDistanceSqr).Select(x => x as Building_TurretGunCE))
            {
                if (EnoughAmmoAround(turret)) continue;
                if (turret.CompAmmo != null && turret.CompAmmo.CurrentAmmo != null)
                    DropSupplies(turret.CompAmmo.CurrentAmmo, Mathf.CeilToInt(0.5f * (float)turret.CompAmmo.Props.magazineSize), turret.Position);
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
