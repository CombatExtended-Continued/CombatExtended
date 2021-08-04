using System;
using System.Collections.Generic;
using System.Linq;
using CombatExtended.Utilities;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public class CompWeaponPickup : ICompTactics
    {
        private const int BULLETIMPACT_COOLDOWN = 800;

        private int lastBulletImpact = -1;

        public bool BulletImpactedRecently
        {
            get
            {
                return GenTicks.TicksGame - lastBulletImpact < BULLETIMPACT_COOLDOWN;
            }
        }

        public override int Priority => 20;

        public CompWeaponPickup()
        {
        }

        public override void Notify_BulletImpactNearBy()
        {
            base.Notify_BulletImpactNearBy();
            if (!BulletImpactedRecently)
            {
                lastBulletImpact = GenTicks.TicksGame;
                CheckPrimaryEquipment();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
        }

        private void CheckPrimaryEquipment()
        {
            if (SelPawn.Faction.IsPlayerSafe())
                return;
            if (SelPawn.equipment == null)
                return;
            if (SelPawn.equipment.Primary != null)
                return;
            if (SelPawn.Downed || SelPawn.InMentalState)
                return;
            if (CompInventory?.SwitchToNextViableWeapon(false, true, false) ?? true)
                return;
            foreach (ThingWithComps thing in CompInventory.rangedWeaponList)
            {
                CompAmmoUser compAmmo = thing.TryGetComp<CompAmmoUser>();
                if (compAmmo == null)
                    continue;
                if (compAmmo.TryPickupAmmo())
                {
                    SelPawn.equipment.equipment.TryAddOrTransfer(thing);
                    return;
                }
            }
            IEnumerable<AmmoThing> ammos = SelPawn.Position.AmmoInRange(Map, 15) ?? new List<AmmoThing>();
            foreach (Thing thing in SelPawn.Position.WeaponsInRange(Map, 15))
            {
                if (!thing.def.IsRangedWeapon)
                {
                    Job job = JobMaker.MakeJob(JobDefOf.Equip, thing);
                    SelPawn.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: true);
                    return;
                }
                if (thing is ThingWithComps weapon)
                {
                    CompAmmoUser compAmmo = weapon.TryGetComp<CompAmmoUser>();
                    if (compAmmo == null)
                        continue;
                    IEnumerable<AmmoDef> supportedAmmo = compAmmo.Props?.ammoSet?.ammoTypes?.Select(a => a.ammo) ?? null;
                    if (supportedAmmo == null)
                        continue;
                    foreach (AmmoThing ammo in ammos)
                    {
                        if (!supportedAmmo.Contains(ammo.AmmoDef))
                            continue;
                        if (!Map.reachability.CanReach(SelPawn.positionInt, ammo, PathEndMode.InteractionCell, TraverseMode.NoPassClosedDoors))
                            continue;
                        Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, ammo);
                        job.count = Mathf.Min(ammo.stackCount, compAmmo.Props.magazineSize);
                        SelPawn.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: false);
                        job = JobMaker.MakeJob(JobDefOf.TakeInventory, weapon);
                        SelPawn.jobs.jobQueue.EnqueueFirst(job);
                        return;
                    }
                }
            }
        }
    }
}
