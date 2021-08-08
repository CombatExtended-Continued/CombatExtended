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
    public class CompUrgentWeaponPickup : ICompTactics
    {
        private const int BULLETIMPACT_COOLDOWN = 800;
        private const int PRIMARY_OPTIMIZE_COOLDOWN = 1500;

        private int lastBulletImpact = -1;

        public bool BulletImpactedRecently
        {
            get
            {
                return GenTicks.TicksGame - lastBulletImpact < BULLETIMPACT_COOLDOWN;
            }
        }

        private int lastPrimaryOptimization = -1;
        public bool PrimaryOptimizatedRecently
        {
            get
            {
                return GenTicks.TicksGame - lastPrimaryOptimization < PRIMARY_OPTIMIZE_COOLDOWN;
            }
        }

        public override int Priority => 20;

        public CompUrgentWeaponPickup()
        {
        }

        public override void Notify_BulletImpactNearBy()
        {
            base.Notify_BulletImpactNearBy();
            if (!BulletImpactedRecently && !PrimaryOptimizatedRecently)
            {
                lastBulletImpact = GenTicks.TicksGame;
                CheckPrimaryEquipment();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastBulletImpact, "lastBulletImpact", -1);
            Scribe_Values.Look(ref lastPrimaryOptimization, "lastPrimaryOptimization", -1);
        }

        private void CheckPrimaryEquipment()
        {
            if (SelPawn.Faction.IsPlayerSafe())
                return;
            if (SelPawn.RaceProps.IsMechanoid)
                return;
            if (!SelPawn.RaceProps.Humanlike)
                return;
            if (SelPawn.equipment == null)
                return;
            if (SelPawn.equipment.Primary != null)
                return;
            if (SelPawn.Downed || SelPawn.InMentalState)
                return;
            if (CompInventory?.SwitchToNextViableWeapon(false, true, false) ?? true)
                return;
            if (CompInventory.rangedWeaponList == null)
                return;
            if (SelPawn.story != null && SelPawn.WorkTagIsDisabled(WorkTags.Violent))
                return;
            foreach (ThingWithComps thing in CompInventory.rangedWeaponList)
            {
                CompAmmoUser compAmmo = thing.TryGetComp<CompAmmoUser>();
                if (compAmmo == null)
                    continue;
                if (compAmmo.TryPickupAmmo())
                {
                    lastPrimaryOptimization = GenTicks.TicksGame;

                    SelPawn.equipment.equipment.TryAddOrTransfer(thing);
                    return;
                }
            }
            IEnumerable<AmmoThing> ammos = SelPawn.Position.AmmoInRange(Map, 15).Where(t => t is AmmoThing) ?? new List<AmmoThing>();
            foreach (Thing thing in SelPawn.Position.WeaponsInRange(Map, 15).OrderBy(t => t.Position.DistanceTo(SelPawn.Position)))
            {
                // TODO need more tunning
                if (thing is ThingWithComps weapon)
                {
                    CompAmmoUser compAmmo = weapon.TryGetComp<CompAmmoUser>();
                    if (!SelPawn.CanReach(thing, PathEndMode.InteractionCell, Danger.Unspecified, false, false))
                        continue;
                    if (!SelPawn.CanReserve(weapon))
                        continue;
                    if (compAmmo == null)
                        continue;
                    IEnumerable<AmmoDef> supportedAmmo = compAmmo.Props?.ammoSet?.ammoTypes?.Select(a => a.ammo) ?? null;
                    if (supportedAmmo == null)
                        continue;

                    foreach (AmmoThing ammo in ammos)
                    {
                        if (!supportedAmmo.Contains(ammo.AmmoDef))
                            continue;
                        if (!SelPawn.CanReach(ammo, PathEndMode.InteractionCell, Danger.Unspecified, false, false))
                            continue;
                        if (!SelPawn.CanReserve(ammo))
                            continue;

                        if (CompInventory.CanFitInInventory(ammo, out int count))
                        {
                            lastPrimaryOptimization = GenTicks.TicksGame;

                            Job pickup = JobMaker.MakeJob(JobDefOf.TakeInventory, ammo);
                            pickup.count = count;
                            SelPawn.jobs.StartJob(pickup, JobCondition.InterruptForced, resumeCurJobAfterwards: false);

                            Job equip = JobMaker.MakeJob(JobDefOf.Equip, weapon);
                            SelPawn.jobs.jobQueue.EnqueueFirst(equip);
                            return;
                        }
                    }
                }
            }
            foreach (Thing thing in SelPawn.Position.WeaponsInRange(Map, 15))
            {
                if (!SelPawn.CanReach(thing, PathEndMode.InteractionCell, Danger.Unspecified, false, false))
                    continue;
                if (!SelPawn.CanReserve(thing))
                    continue;
                if (!thing.def.IsRangedWeapon)
                {
                    lastPrimaryOptimization = GenTicks.TicksGame;

                    Job job = JobMaker.MakeJob(JobDefOf.Equip, thing);
                    SelPawn.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: true);
                    return;
                }
            }
        }
    }
}
