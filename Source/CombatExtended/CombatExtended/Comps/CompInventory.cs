using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;
using Verse.AI;

namespace CombatExtended
{
    public class CompInventory : ThingComp
    {
        #region Fields

        private int age = 0;
        private Pawn parentPawnInt = null;
        private const int CLEANUPTICKINTERVAL = 2100;        
        private float currentWeightCached;
        private float currentBulkCached;                        
        private List<Thing> ammoListCached = new List<Thing>();
        private List<ThingWithComps> meleeWeaponListCached = new List<ThingWithComps>();
        private List<ThingWithComps> rangedWeaponListCached = new List<ThingWithComps>();

        #endregion

        #region Properties

        public CompProperties_Inventory Props
        {
            get
            {
                return (CompProperties_Inventory)props;
            }
        }       
        public float currentWeight
        {
            get
            {
                return currentWeightCached;
            }
        }
        public float currentBulk
        {
            get
            {
                return currentBulkCached;
            }
        }
        private float availableWeight
        {
            get
            {
                return capacityWeight - currentWeight;
            }
        }
        private float availableBulk
        {
            get
            {
                return capacityBulk - currentBulk;
            }
        }
        public float capacityBulk
        {
            get
            {
                return parentPawn.GetStatValue(CE_StatDefOf.CarryBulk);
            }
        }
        public float capacityWeight
        {
            get
            {
                return parentPawn.GetStatValue(CE_StatDefOf.CarryWeight);
            }
        }
        private Pawn parentPawn
        {
            get
            {
                if (parentPawnInt == null)
                {
                    parentPawnInt = parent as Pawn;
                }
                return parentPawnInt;
            }
        }
        public float moveSpeedFactor
        {
            get
            {
                return MassBulkUtility.MoveSpeedFactor(currentWeight, capacityWeight);
            }
        }
        public float workSpeedFactor
        {
            get
            {
                return MassBulkUtility.WorkSpeedFactor(currentBulk, capacityBulk);
            }
        }
        public float encumberPenalty
        {
            get
            {
                return MassBulkUtility.EncumberPenalty(currentWeight, capacityWeight);
            }
        }
        public IEnumerable<ThingWithComps> weapons
        {
            get
            {
                if (meleeWeaponList != null)
                {
                    foreach (ThingWithComps weapon in meleeWeaponList)
                        yield return weapon;
                }
                if (rangedWeaponList != null)
                {
                    foreach (ThingWithComps weapon in rangedWeaponList)
                        yield return weapon;
                }
            }
        }       
        public ThingOwner container
        {
            get
            {
                if (parentPawn.inventory != null)
                {
                    return parentPawn.inventory.innerContainer;
                }
                return null;
            }
        }
        public List<Thing> ammoList => ammoListCached;
        public List<ThingWithComps> meleeWeaponList => meleeWeaponListCached;
        public List<ThingWithComps> rangedWeaponList => rangedWeaponListCached;

        #endregion Properties

        #region Methods
 
        /// <summary>
        /// WARNING this is very slow. Return the available weight.
        /// </summary>
        /// <param name="updateInventory">Wether to recalculate the cache. Setting this to false can result in invalid results.</param>
        /// <returns></returns>
        public float GetAvailableWeight(bool updateInventory = true)
        {
            if(updateInventory)
                UpdateInventory();
            return availableWeight;
        }

        /// <summary>
        /// WARNING this is very slow. Return the available bulk.
        /// </summary>
        /// <param name="updateInventory">Wether to recalculate the cache. Setting this to false can result in invalid results.</param>
        /// <returns></returns>
        public float GetAvailableBulk(bool updateInventory = true)
        {
            if (updateInventory)
                UpdateInventory();
            return availableBulk;
        }

        /// <summary>
        /// Similar to ThingContainer.TotalStackCountOfDef(), returns the count of all matching AmmoDefs in AmmoList cache.
        /// </summary>
        /// <param name="def">ThingDef to count.</param>
        /// <returns>int amount of AmmoDef found in AmmoList.</returns>
        public int AmmoCountOfDef(AmmoDef def)
        {
            return ammoListCached.Where(t => t.def == def).Sum(t => t.stackCount);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            UpdateInventory();
        }

        /// <summary>
        /// Refreshes the cached bulk and weight. Call this whenever items are added/removed from inventory
        /// </summary>
        public void UpdateInventory()
        {
            if (parentPawn == null)
            {
                Log.Error("CompInventory on non-pawn " + parent.ToString());
                return;
            }
            float newBulk = 0f;
            float newWeight = 0f;

            // Add equipped weapon
            if (parentPawn.equipment != null && parentPawn.equipment.Primary != null)
            {
                GetEquipmentStats(parentPawn.equipment.Primary, out newWeight, out newBulk);
            }

            // Add apparel
            if (parentPawn.apparel != null && parentPawn.apparel.WornApparelCount > 0)
            {
                foreach (Thing apparel in parentPawn.apparel.WornApparel)
                {
                    float apparelBulk = apparel.GetStatValue(CE_StatDefOf.WornBulk);
                    float apparelWeight = apparel.GetStatValue(StatDefOf.Mass);
                    newBulk += apparelBulk;
                    newWeight += apparelWeight;
                    if (age > CLEANUPTICKINTERVAL && apparelBulk > 0 && (parentPawn?.Spawned ?? false) && (parentPawn.factionInt?.IsPlayer ?? false))
                        LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_WornBulk, OpportunityType.GoodToKnow);
                }
            }

            // Add inventory items
            if (parentPawn.inventory != null && parentPawn.inventory.innerContainer != null)
            {
                ammoListCached.Clear();
                meleeWeaponListCached.Clear();
                rangedWeaponListCached.Clear();

                List<HoldRecord> recs = LoadoutManager.GetHoldRecords(parentPawn);
                foreach (Thing thing in parentPawn.inventory.innerContainer)
                {
                    // Check for weapons
                    ThingWithComps eq = thing as ThingWithComps;
                    CompEquippable compEq = thing.TryGetComp<CompEquippable>();
                    if (eq != null && compEq != null)
                    {
                        if (eq.def.IsRangedWeapon)
                        {
                            rangedWeaponListCached.Add(eq);
                        }
                        else
                        {
                            meleeWeaponListCached.Add(eq);
                        }
                        // Calculate equipment weight
                        float eqWeight;
                        float eqBulk;
                        GetEquipmentStats(eq, out eqWeight, out eqBulk);
                        newWeight += eqWeight * thing.stackCount;
                        newBulk += eqBulk * thing.stackCount;
                    }
                    else
                    {
                        // Add item weight
                        newBulk += thing.GetStatValue(CE_StatDefOf.Bulk) * thing.stackCount;
                        newWeight += thing.GetStatValue(StatDefOf.Mass) * thing.stackCount;
                    }
                    // Update ammo list
                    if (thing.def is AmmoDef)
                    {
                        ammoListCached.Add(thing);
                    }
                    if (recs != null)
                    {
                        HoldRecord rec = recs.FirstOrDefault(hr => hr.thingDef == thing.def);
                        if (rec != null && !rec.pickedUp)
                            rec.pickedUp = true;
                    }
                }
            }
            currentBulkCached = newBulk;
            currentWeightCached = newWeight;
        }

        /// <summary>
        /// Determines if and how many of an item def currently fit into the inventory with regards to weight/bulk constraints.
        /// </summary>
        /// <param name="thingDef">ThingDef to check</param>
        /// <param name="count">Maximum amount of that item that can fit into the inventory</param>
        /// <param name="ignoreEquipment">Whether to include currently equipped weapons when calculating current weight/bulk</param>
        /// <param name="useApparelCalculations">Whether to use calculations for worn apparel. This will factor in equipped stat offsets boosting inventory space and use the worn bulk and weight.</param>
        /// <returns>True if one or more items fit into the inventory</returns>
        public bool CanFitInInventory(ThingDef thingDef, out int count, bool ignoreEquipment = false, bool useApparelCalculations = false)
        {
            float thingWeight;
            float thingBulk;

            if (useApparelCalculations)
            {
                thingWeight = thingDef.GetStatValueAbstract(StatDefOf.Mass);
                thingBulk = thingDef.GetStatValueAbstract(CE_StatDefOf.WornBulk);
                if (thingWeight <= 0 && thingBulk <= 0)
                {
                    count = 1;
                    return true;
                }
                // Subtract the stat offsets we get from wearing this
                thingWeight -= thingDef.equippedStatOffsets.GetStatOffsetFromList(CE_StatDefOf.CarryWeight);
                thingBulk -= thingDef.equippedStatOffsets.GetStatOffsetFromList(CE_StatDefOf.CarryBulk);
            }
            else
            {
                thingWeight = thingDef.GetStatValueAbstract(StatDefOf.Mass);
                //  thingWeight = thing.GetStatValue(CE_StatDefOf.Weight);
                thingBulk = thingDef.GetStatValueAbstract(CE_StatDefOf.Bulk);
            }
            // Subtract weight of currently equipped weapon
            float eqBulk = 0f;
            float eqWeight = 0f;
            if (ignoreEquipment && parentPawn.equipment != null && parentPawn.equipment.Primary != null)
            {
                ThingWithComps eq = parentPawn.equipment.Primary;
                GetEquipmentStats(eq, out eqWeight, out eqBulk);
            }
            // Calculate how many items we can fit into our inventory
            float amountByWeight = thingWeight <= 0 ? 1 : (availableWeight + eqWeight) / thingWeight;
            float amountByBulk = thingBulk <= 0 ? 1 : (availableBulk + eqBulk) / thingBulk;
            count = Mathf.FloorToInt(Mathf.Min(amountByBulk, amountByWeight, 1));
            return count > 0;
        }            

        /// <summary>
        /// Determines if and how many of an item currently fit into the inventory with regards to weight/bulk constraints.
        /// </summary>
        /// <param name="thing">Thing to check</param>
        /// <param name="count">Maximum amount of that item that can fit into the inventory</param>
        /// <param name="ignoreEquipment">Whether to include currently equipped weapons when calculating current weight/bulk</param>
        /// <param name="useApparelCalculations">Whether to use calculations for worn apparel. This will factor in equipped stat offsets boosting inventory space and use the worn bulk and weight.</param>
        /// <returns>True if one or more items fit into the inventory</returns>
        public bool CanFitInInventory(Thing thing, out int count, bool ignoreEquipment = false, bool useApparelCalculations = false)
        {
            float thingWeight;
            float thingBulk;

            if (useApparelCalculations)
            {
                thingWeight = thing.GetStatValue(StatDefOf.Mass);
                thingBulk = thing.GetStatValue(CE_StatDefOf.WornBulk);
                if (thingWeight <= 0 && thingBulk <= 0)
                {
                    count = 1;
                    return true;
                }
                // Subtract the stat offsets we get from wearing this
                thingWeight -= thing.def.equippedStatOffsets.GetStatOffsetFromList(CE_StatDefOf.CarryWeight);
                thingBulk -= thing.def.equippedStatOffsets.GetStatOffsetFromList(CE_StatDefOf.CarryBulk);
            }
            else
            {
                thingWeight = thing.GetStatValue(StatDefOf.Mass);
                //  thingWeight = thing.GetStatValue(CE_StatDefOf.Weight);
                thingBulk = thing.GetStatValue(CE_StatDefOf.Bulk);
            }
            // Subtract weight of currently equipped weapon
            float eqBulk = 0f;
            float eqWeight = 0f;
            if (ignoreEquipment && parentPawn.equipment != null && parentPawn.equipment.Primary != null)
            {
                ThingWithComps eq = parentPawn.equipment.Primary;
                GetEquipmentStats(eq, out eqWeight, out eqBulk);
            }
            // Calculate how many items we can fit into our inventory
            float amountByWeight = thingWeight <= 0 ? thing.stackCount : (availableWeight + eqWeight) / thingWeight;
            float amountByBulk = thingBulk <= 0 ? thing.stackCount : (availableBulk + eqBulk) / thingBulk;
            count = Mathf.FloorToInt(Mathf.Min(amountByBulk, amountByWeight, thing.stackCount));
            return count > 0;
        }

        public static void GetEquipmentStats(ThingWithComps eq, out float weight, out float bulk)
        {
            weight = eq.GetStatValue(StatDefOf.Mass);
            //old     weight = eq.GetStatValue(CE_StatDefOf.Weight);
            bulk = eq.GetStatValue(CE_StatDefOf.Bulk);

            /*
            CompAmmoUser comp = eq.TryGetComp<CompAmmoUser>();
            if (comp != null && comp.CurrentAmmo != null)
            {
                weight += comp.CurrentAmmo.GetStatValueAbstract(StatDefOf.Mass) * comp.CurMagCount;
                //old     weight += comp.currentAmmo.GetStatValueAbstract(CE_StatDefOf.Weight) * comp.curMagCount;
                bulk += comp.CurrentAmmo.GetStatValueAbstract(CE_StatDefOf.Bulk) * comp.CurMagCount;
            }
            */
        }

        /// <summary>
        /// Attempts to equip a weapon from the inventory, puts currently equipped weapon into inventory if it exists
        /// </summary>
        /// <param name="useFists">Whether to put the currently equipped weapon away even if no replacement is found</param>
        /// <param name="useAOE">Whether to use AOE weapons (grenades, explosive, RPGs, etc)</param>
        public bool SwitchToNextViableWeapon(bool useFists = false, bool useAOE = false, bool stopJob = true, Func<ThingWithComps, CompAmmoUser, bool> predicate = null)
        {
            if (parentPawn.equipment?.Primary?.def.weaponTags?.Contains("NoSwitch") ?? false)
            {
                return false;
            }

            ThingWithComps newEq = null;

            // Stop current job
            if (parentPawn.jobs != null && stopJob)
                parentPawn.jobs.StopAll();

            // Cycle through available ranged weapons
            foreach (ThingWithComps gun in rangedWeaponListCached)
            {
                if (parentPawn.equipment != null && parentPawn.equipment.Primary != gun)
                {
                    CompAmmoUser compAmmo = gun.TryGetComp<CompAmmoUser>();
                    if ((!useAOE && gun.def.IsAOEWeapon()) || gun.def.IsIlluminationDevice())
                        continue;
                    if ((predicate?.Invoke(gun, compAmmo) ?? true) && compAmmo == null || compAmmo.HasAndUsesAmmoOrMagazine)
                    {
                        newEq = gun;
                        break;
                    }
                }
            }
            // If no ranged weapon was found, use first available melee weapons
            if (newEq == null)
                newEq = (predicate == null ? meleeWeaponListCached : meleeWeaponListCached.Where(w => predicate.Invoke(w, null))).FirstOrDefault();

            // Equip the weapon
            if (newEq != null)
            {
                if (!stopJob)
                    parentPawn.jobs.StartJob(JobMaker.MakeJob(CE_JobDefOf.EquipFromInventory, newEq), JobCondition.InterruptForced, resumeCurJobAfterwards: true);
                else
                    TrySwitchToWeapon(newEq, stopJob);
                return true;
            }
            else if (useFists)
            {
                // Put away current weapon
                ThingWithComps eq = parentPawn.equipment?.Primary;
                if (eq != null && !parentPawn.equipment.TryTransferEquipmentToContainer(eq, container))
                {
                    // If we can't put it into our inventory, drop it
                    if (parentPawn.Position.InBounds(parentPawn.Map))
                    {
                        ThingWithComps unused;
                        parentPawn.equipment.TryDropEquipment(eq, out unused, parentPawn.Position);
                    }
                    else
                    {
#if DEBUG
                        Log.Warning("Combat Extended :: CompInventory :: SwitchToNextViableWeapon :: destroying out of bounds equipment" + eq.ToString());
#endif
                        if (!eq.Destroyed)
                        {
                            eq.Destroy();
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public bool TryFindRandomAOEWeapon(out ThingWithComps weapon, Func<ThingWithComps, bool> predicate = null, bool checkAmmo = false)
        {
            weapon = null;
            foreach (ThingWithComps gun in rangedWeaponListCached.InRandomOrder())
            {
                if (checkAmmo)
                {
                    CompAmmoUser ammoUser = gun.TryGetComp<CompAmmoUser>();
                    if (ammoUser != null && !ammoUser.HasAmmoOrMagazine)
                        continue;
                }
                if (parentPawn.equipment != null && parentPawn.equipment.Primary != gun)
                {
                    if (gun.def.IsAOEWeapon() && (predicate == null || predicate.Invoke(gun)))
                    {
                        weapon = gun;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool TryFindSmokeWeapon(out ThingWithComps grenade)
        {
            grenade = (ThingWithComps)container.FirstOrFallback(t => t.def.weaponTags?.Contains("GrenadeSmoke") ?? false, null);
            if (grenade == null)
                return false;
            CompAmmoUser ammoUser = grenade.TryGetComp<CompAmmoUser>();
            if (ammoUser != null)
            {
                if (ammoUser.CurAmmoProjectile?.projectile?.damageDef != DamageDefOf.Smoke)
                    return false;
                if (ammoUser.CurAmmoProjectile?.projectile?.postExplosionSpawnThingDef != ThingDefOf.Gas_Smoke)
                    return false;
            }
            return true;
        }

        public bool TryFindViableWeapon(out ThingWithComps weapon, bool useAOE = false, Func<ThingWithComps, CompAmmoUser, bool> predicate = null)
        {
            weapon = null;
            // Cycle through available ranged weapons
            foreach (ThingWithComps gun in rangedWeaponListCached)
            {
                if (parentPawn.equipment != null && parentPawn.equipment.Primary != gun)
                {
                    CompAmmoUser compAmmo = gun.TryGetComp<CompAmmoUser>();
                    if ((!useAOE && gun.def.IsAOEWeapon()) || gun.def.IsIlluminationDevice())
                        continue;
                    if ((predicate?.Invoke(gun, compAmmo) ?? true) && compAmmo == null || compAmmo.HasAndUsesAmmoOrMagazine)
                    {
                        weapon = gun;
                        break;
                    }
                }
            }
            // If no ranged weapon was found, use first available melee weapons
            if (weapon == null)
                weapon = (predicate == null ? meleeWeaponListCached : meleeWeaponListCached.Where(w => predicate.Invoke(w, null))).FirstOrDefault();
            return weapon != null;
        }

        public bool TryFindFlare(out ThingWithComps flareGun, bool checkAmmo = false)
        {
            foreach (ThingWithComps gun in rangedWeaponList)
            {
                if (checkAmmo)
                {
                    CompAmmoUser ammoUser = gun.TryGetComp<CompAmmoUser>();
                    if (ammoUser != null && !ammoUser.HasAmmoOrMagazine)
                        continue;
                }
                if (gun.def.IsIlluminationDevice())
                {
                    CompAmmoUser compAmmo = gun.TryGetComp<CompAmmoUser>();
                    if (compAmmo == null || compAmmo.HasAmmoOrMagazine)
                    {
                        flareGun = gun;
                        return true;
                    }
                }
            }
            flareGun = null;
            return false;
        }

        public void TrySwitchToWeapon(ThingWithComps newEq, bool stopJob = true)
        {
            if (newEq == null || parentPawn.equipment == null || !container.Contains(newEq))
            {
                return;
            }
            // Stop current job
            if (parentPawn.jobs != null && stopJob)
                parentPawn.jobs.StopAll();

            if (parentPawn.equipment.Primary != null)
            {
                int count;
                if (CanFitInInventory(parentPawn.equipment.Primary, out count, true))
                {
                    parentPawn.equipment.TryTransferEquipmentToContainer(parentPawn.equipment.Primary, container);
                }
                else
                {
#if DEBUG
                    Log.Warning("Combat Extended :: CompInventory :: TrySwitchToWeapon :: failed to add current equipment to inventory");
#endif
                    parentPawn.equipment.MakeRoomFor(newEq);
                }
            }
            parentPawn.equipment.AddEquipment((ThingWithComps)container.Take(newEq, 1));
            if (newEq.def.soundInteract != null)
                newEq.def.soundInteract.PlayOneShot(new TargetInfo(parent.Position, parent.MapHeld, false));
        }

        public override void CompTick()
        {
            /*
             * Need to update this to avoid calling IsColonist too soon after spawning
             */
            age++;
            /*
             * Routin cleanup
             */
            if ((parentPawn.thingIDNumber + GenTicks.TicksGame) % CLEANUPTICKINTERVAL == 0)
            {
                // Ask HoldTracker to clean itself up...
                parentPawn.HoldTrackerCleanUp();
            }
            base.CompTick();
            // Remove items from inventory if we're over the bulk limit
            /*
            while (availableBulk < 0 && container.Count > 0)
            {
                Log.Message("CE-RD :: Too much bulk for " + parentPawn.ToString() + ", " + (parentPawn.IsColonist ? "is colonist" : "is not colonist"));
                if (parentPawn.IsColonist)
                {
                    if (this.parent.Position.InBounds())
                    {
                        Thing droppedThing;
                        container.TryDrop(container.Last(), this.parent.Position, ThingPlaceMode.Near, 1, out droppedThing);
                    }
                    else
                    {
                        container.Remove(container.Last());
                    }
                }
            }
            */

            if (Controller.settings.DebugEnableInventoryValidation) ValidateCache();
        }

        /// <summary>
        /// Debug method to catch cases where inventory cache isn't being updated properly on pawn inventory change. ONLY FOR DEBUGGING, DON'T CALL THIS IN ANY KIND OF RELEASE BUILD.
        /// </summary>
        private void ValidateCache()
        {
            float oldWeight = currentWeight;
            float oldBulk = currentBulk;
            UpdateInventory();
            if (oldWeight != currentWeight || oldBulk != currentBulk)
            {
                Log.Error("Combat Extended :: CompInventory :: " + parent.ToString() + " failed inventory validation");
            }
        }

        #endregion Methods
    }
}
