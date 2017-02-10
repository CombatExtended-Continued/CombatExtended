using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.Detours
{
    internal static class Detours_ThingContainer
    {
        private static readonly FieldInfo innerListFieldInfo = typeof(ThingContainer).GetField("innerList", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo maxStacksFieldInfo = typeof(ThingContainer).GetField("maxStacks", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static bool TryAdd(this ThingContainer _this, Thing item, bool canMergeWithExistingStacks = true)
        {
            if (item == null)
            {
                Log.Warning("Tried to add null item to ThingContainer.");
                return false;
            }
            if (_this.Contains(item))
            {
                Log.Warning("Tried to add " + item + " to ThingContainer but this item is already here.");
                return false;
            }

            if (item.stackCount > _this.AvailableStackSpace)
            {
                return _this.TryAdd(item, _this.AvailableStackSpace) > 0;
            }

            List<Thing> innerList = (List<Thing>)innerListFieldInfo.GetValue(_this);    // Fetch innerList through reflection

            SlotGroupUtility.Notify_TakingThing(item);
			if (canMergeWithExistingStacks && item.def.stackLimit > 1)
			{
				for (int i = 0; i < innerList.Count; i++)
				{
					if (innerList[i].def == item.def)
					{
						int num = item.stackCount;
						if (num > _this.AvailableStackSpace)
						{
							num = _this.AvailableStackSpace;
						}
						Thing other = item.SplitOff(num);
						if (!innerList[i].TryAbsorbStack(other, false))
						{
							Log.Error("ThingContainer did TryAbsorbStack " + item + " but could not absorb stack.");
						}
					}
					if (item.Destroyed)
					{
                        //CE PART!!!
                        CE_Utility.TryUpdateInventory(_this.owner as Pawn_InventoryTracker);   // Item has been added, notify CompInventory
                        return true;
					}
				}
			}

            int maxStacks = (int)maxStacksFieldInfo.GetValue(_this);    // Fetch maxStacks through reflection

            if (innerList.Count >= maxStacks)
            {
                return false;
            }
            if (item.Spawned)
            {
                item.DeSpawn();
            }
            if (item.HasAttachment(ThingDefOf.Fire))
            {
                item.GetAttachment(ThingDefOf.Fire).Destroy(DestroyMode.Vanish);
            }
            item.holdingContainer = _this;
            innerList.Add(item);

            CE_Utility.TryUpdateInventory(_this.owner as Pawn_InventoryTracker);   // Item has been added, notify CompInventory

            return true;
        }

        internal static bool TryDrop(this ThingContainer _this, Thing thing, IntVec3 dropLoc, Map map, ThingPlaceMode mode, int count, out Thing resultingThing, Action<Thing, int> placedAction = null)
        {
            if (thing.stackCount < count)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Tried to drop ",
                    count,
                    " of ",
                    thing,
                    " while only having ",
                    thing.stackCount
                }));
                count = thing.stackCount;
            }
            if (count == thing.stackCount)
            {
                if (GenDrop.TryDropSpawn(thing, dropLoc, map , mode, out resultingThing, placedAction))
                {
                    _this.Remove(thing);
                    CE_Utility.TryUpdateInventory(_this.owner as Pawn_InventoryTracker);   // Thing dropped, update inventory
                    return true;
                }
                return false;
            }
            else
            {
                Thing thing2 = thing.SplitOff(count);
                if (GenDrop.TryDropSpawn(thing2, dropLoc, map, mode, out resultingThing, placedAction))
                {
                    CE_Utility.TryUpdateInventory(_this.owner as Pawn_InventoryTracker);   // Thing dropped, update inventory
                    return true;
                }
                thing.stackCount += thing2.stackCount;
                return false;
            }
        }

        internal static Thing Get(this ThingContainer _this, Thing thing, int count)
        {
            if (count > thing.stackCount)
            {
                Log.Error(string.Concat(new object[]
                {
            "Tried to get ",
            count,
            " of ",
            thing,
            " while only having ",
            thing.stackCount
                }));
                count = thing.stackCount;
            }
            if (count == thing.stackCount)
            {
                _this.Remove(thing);
                return thing;
            }
            Thing thing2 = thing.SplitOff(count);
            thing2.holdingContainer = null;
            CE_Utility.TryUpdateInventory(_this.owner as Pawn_InventoryTracker);   // Item was taken from inventory, update
            return thing2;
        }

        internal static void Remove(this ThingContainer _this, Thing item)
        {
            if (!_this.Contains(item))
            {
                return;
            }
            if (item.holdingContainer == _this)
            {
                item.holdingContainer = null;
            }
            List<Thing> innerList = (List<Thing>)innerListFieldInfo.GetValue(_this);    // Fetch innerList through reflection
            innerList.Remove(item);
            Pawn_InventoryTracker pawn_InventoryTracker = _this.owner as Pawn_InventoryTracker;
            if (pawn_InventoryTracker != null)
            {
                pawn_InventoryTracker.Notify_ItemRemoved(item);
            }
            CE_Utility.TryUpdateInventory(_this.owner as Pawn_InventoryTracker);           // Item was removed, update inventory
        }
    }
}