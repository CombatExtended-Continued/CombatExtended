using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace CombatExtended
{
    public class CompInventory : ThingComp
    {
        #region Fields

        private Pawn parentPawnInt = null;
        private bool initializedLoadouts = false;
        private int ticksToInitLoadout = 5;         // Generate loadouts this many ticks after spawning
        private const int CLEANUPTICKINTERVAL = GenTicks.TickLongInterval;
        private int ticksToNextCleanUp = GenTicks.TicksAbs;
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
        public ThingContainer container
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
        public List<Thing> ammoList
        {
            get
            {
                return ammoListCached;
            }
        }
        public List<ThingWithComps> meleeWeaponList => meleeWeaponListCached;
        public List<ThingWithComps> rangedWeaponList => rangedWeaponListCached;

        #endregion

        #region Methods

        public override void PostSpawnSetup()
        {
            base.PostSpawnSetup();
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
                    if (apparelBulk > 0 && (parentPawn?.IsColonist ?? false)) TutorUtility.DoModalDialogIfNotKnown(CE_ConceptDefOf.CE_WornBulk);
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
                        if (compEq.PrimaryVerb != null)
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
                thingWeight = thing.GetStatValue(CE_StatDefOf.WornWeight);
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
            CompAmmoUser comp = eq.TryGetComp<CompAmmoUser>();
            if (comp != null && comp.currentAmmo != null)
            {
                weight += comp.currentAmmo.GetStatValueAbstract(StatDefOf.Mass) * comp.curMagCount;
                //old     weight += comp.currentAmmo.GetStatValueAbstract(CE_StatDefOf.Weight) * comp.curMagCount;
                bulk += comp.currentAmmo.GetStatValueAbstract(CE_StatDefOf.Bulk) * comp.curMagCount;
            }
        }

        /// <summary>
        /// Attempts to equip a weapon from the inventory, puts currently equipped weapon into inventory if it exists
        /// </summary>
        /// <param name="useFists">Whether to put the currently equipped weapon away even if no replacement is found</param>
        public void SwitchToNextViableWeapon(bool useFists = true)
        {
            ThingWithComps newEq = null;

            // Stop current job
            if (parentPawn.jobs != null)
                parentPawn.jobs.StopAll();

            // Cycle through available ranged weapons
            foreach (ThingWithComps gun in rangedWeaponListCached)
            {
                if (parentPawn.equipment != null && parentPawn.equipment.Primary != gun)
                {
                    CompAmmoUser compAmmo = gun.TryGetComp<CompAmmoUser>();
                    if (compAmmo == null || compAmmo.hasAndUsesAmmoOrMagazine)
                    {
                        newEq = gun;
                        break;
                    }
                }
            }
            // If no ranged weapon was found, use first available melee weapons
            if (newEq == null)
                newEq = meleeWeaponListCached.FirstOrDefault();

            // Equip the weapon
            if (newEq != null)
            {
                TrySwitchToWeapon(newEq);
            }
            else if (useFists && parentPawn.equipment?.Primary != null)
            {
                ThingWithComps oldEq;
                if (!parentPawn.equipment.TryTransferEquipmentToContainer(parentPawn.equipment.Primary, container, out oldEq))
                {
                    if (parentPawn.Position.InBounds(parentPawn.Map))
                    {
                        ThingWithComps unused;
                        parentPawn.equipment.TryDropEquipment(oldEq, out unused, parentPawn.Position);
                    }
                    else
                    {
#if DEBUG
                        Log.Message("CE :: CompInventory :: SwitchToNextViableWeapon :: destroying out of bounds equipment" + oldEq.ToString());
#endif
                        if (!oldEq.Destroyed)
                        {
                            oldEq.Destroy();
                        }
                    }
                }
            }
        }

        public void TrySwitchToWeapon(ThingWithComps newEq)
        {
            if (newEq == null || parentPawn.equipment == null || !container.Contains(newEq))
            {
                return;
            }
            if (parentPawn.equipment.Primary != null)
            {

                ThingWithComps oldEq;
                int count;
                if (CanFitInInventory(parentPawn.equipment.Primary, out count, true))
                {
                    parentPawn.equipment.TryTransferEquipmentToContainer(parentPawn.equipment.Primary, container, out oldEq);
                }
                else
                {
#if DEBUG
                    Log.Warning("CE :: CompInventory :: TrySwitchToWeapon :: failed to add current equipment to inventory");
#endif
                    parentPawn.equipment.MakeRoomFor(newEq);
                }
            }
            parentPawn.equipment.AddEquipment((ThingWithComps)container.Get(newEq, 1));
            if (newEq.def.soundInteract != null)
                newEq.def.soundInteract.PlayOneShot(new TargetInfo(parent.Position, parent.MapHeld, false));
        }

        public override void CompTick()
        {
            // Initialize loadouts on first tick
            if (ticksToInitLoadout > 0)
            {
                ticksToInitLoadout--;
            }
            else if (!initializedLoadouts)
            {
                // Find all loadout generators
                List<LoadoutGeneratorThing> genList = new List<LoadoutGeneratorThing>();
                foreach (Thing thing in container)
                {
                    LoadoutGeneratorThing lGenThing = thing as LoadoutGeneratorThing;
                    if (lGenThing != null && lGenThing.loadoutGenerator != null)
                        genList.Add(lGenThing);
                }

                // Sort list by execution priority
                genList.Sort(delegate (LoadoutGeneratorThing x, LoadoutGeneratorThing y)
                {
                    return x.priority.CompareTo(y.priority);
                });

                // Generate loadouts
                foreach (LoadoutGeneratorThing thing in genList)
                {
                    thing.loadoutGenerator.GenerateLoadout(this);
                    container.Remove(thing);
                }
                initializedLoadouts = true;
            }
            if (GenTicks.TicksAbs >= ticksToNextCleanUp)
            {
	            // Ask HoldTracker to clean itself up...
	            parentPawn.HoldTrackerCleanUp();
	            ticksToNextCleanUp = GenTicks.TicksAbs + CLEANUPTICKINTERVAL;
            }
            
            //Log.Message("CE-RD :: pre-base.comptick");
            base.CompTick();
            //Log.Message("CE-RD :: post-base.comptick");
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
        }

        #endregion
    }
}
