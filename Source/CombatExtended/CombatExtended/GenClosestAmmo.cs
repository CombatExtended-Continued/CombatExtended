using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace CombatExtended
{
    public static class GenClosestAmmo
    {
        public static Thing ClosestAmmoReachable(IntVec3 root, Map map, CompAmmoUser user, TraverseParms traverseParams, PathEndMode peMode = PathEndMode.ClosestTouch, float maxDistance = 9999f, Predicate<Thing> validator = null, IEnumerable<Thing> customGlobalSearchSet = null, int searchRegionsMin = 0, int searchRegionsMax = -1, bool forceAllowGlobalSearch = false, RegionType traversableRegionTypes = RegionType.Set_Passable, bool ignoreEntirelyForbiddenRegions = false)
        {
            //selectedAmmo
            //currentAmmo
            //others
            
            if (user == null)
            {
                Log.ErrorOnce("ClosestAmmoReachable with null CompAmmoUser", 724492);
                return null;
            }

            if (user.Props.ammoSet?.ammoTypes.NullOrEmpty() ?? true)
                return null;
            
            bool flag = searchRegionsMax < 0 | forceAllowGlobalSearch;
            if (!flag && customGlobalSearchSet != null)
            {
                Log.ErrorOnce("searchRegionsMax >= 0 && customGlobalSearchSet != null && !forceAllowGlobalSearch. customGlobalSearchSet will never be used.", 6568764, false);
            }

            #region EarlyOutSearch-like
            var findAny = user.Props.ammoSet?.ammoTypes?.Any(x => map.listerThings.ThingsOfDef(x.ammo).Any()) ?? false;
            
            if (!findAny)
                return null;
            
            var mDsq = maxDistance * maxDistance;
            if (maxDistance != 9999f)
            {
                var mDsqMax = Math.Max(
                    Math.Max(root.DistanceToSquared(IntVec3.Zero), root.DistanceToSquared(map.Size)),
                    Math.Max(root.DistanceToSquared(new IntVec3(0, 0, map.Size.z)), root.DistanceToSquared(new IntVec3(map.Size.x, 0, 0))));
                
                //maxDistance smaller than the maximum distance possible within the map -- already exclude some of the ammo
                if (mDsq < mDsqMax)
                {
                    if (!user.Props.ammoSet.ammoTypes.Any(x => map.listerThings.ThingsOfDef(x.ammo).Any(y => y.Position.DistanceToSquared(root) <= mDsq)))
                        return null;
                }
                else
                    if (!user.Props.ammoSet.ammoTypes.Any(x => map.listerThings.ThingsOfDef(x.ammo).Any()))
                    return null;

            }
            #endregion
            
            #region Preparing list to search through
            List<Thing> thingList = new List<Thing>();

            foreach (var ammo in user.Props.ammoSet.ammoTypes.Select(x => x.ammo))
            {
                thingList.AddRange(map.listerThings.ThingsOfDef(ammo).Where(y => y.Position.DistanceToSquared(root) <= mDsq));
            }
            #endregion

            Func<Thing, float> priorityGetter = (x => { if (x.def == user.SelectedAmmo) return 3f; else if (x.def == user.CurrentAmmo) return 2f; else return 1f; });
            
            int num = (searchRegionsMax > 0) ? searchRegionsMax : 30;
            var thing = RegionwiseBFSWorker(root, map, thingList, peMode, traverseParams, validator, priorityGetter, searchRegionsMin, num, maxDistance, out int num2, traversableRegionTypes, ignoreEntirelyForbiddenRegions);
            var flag2 = (thing == null && num2 < num);
            
            if ((thing == null & flag) && !flag2)
            {
                if (traversableRegionTypes != RegionType.Set_Passable)
                {
                    Log.ErrorOnce("ClosestAmmoReachable had to do a global search, but traversableRegionTypes is not set to passable only. It's not supported, because Reachability is based on passable regions only.", 14345767, false);
                }

                Predicate<Thing> validator2 = (Thing t) => map.reachability.CanReach(root, t, peMode, traverseParams) && (validator == null || validator(t));
                thing = GenClosest.ClosestThing_Global(root, thingList, maxDistance, validator2, null);
               
            }
            
            return thing;
        }

        public static Thing RegionwiseBFSWorker(IntVec3 root, Map map, List<Thing> thingList, PathEndMode peMode, TraverseParms traverseParams, Predicate<Thing> validator, Func<Thing, float> priorityGetter, int minRegions, int maxRegions, float mDsq, out int regionsSeen, RegionType traversableRegionTypes = RegionType.Set_Passable, bool ignoreEntirelyForbiddenRegions = false)
        {
            regionsSeen = 0;
            if (traverseParams.mode == TraverseMode.PassAllDestroyableThings)
            {
                Log.Error("CombatExtended :: RegionwiseBFSWorker with traverseParams.mode PassAllDestroyableThings. Use ClosestThingGlobal.", false);
                return null;
            }
            if (traverseParams.mode == TraverseMode.PassAllDestroyableThingsNotWater)
            {
                Log.Error("CombatExtended :: RegionwiseBFSWorker with traverseParams.mode PassAllDestroyableThingsNotWater. Use ClosestThingGlobal.", false);
                return null;
            }
            Region region = root.GetRegion(map, traversableRegionTypes);
            if (region == null)
            {
                return null;
            }
            
            RegionEntryPredicate entryCondition = (Region from, Region to) => to.Allows(traverseParams, false) && (mDsq > 25000000f || to.extentsClose.ClosestDistSquaredTo(root) < mDsq);
            Thing closestThing = null;
            int regionsSeenScan2 = 0;
            float closestDistSquared = 9999999f;
            float bestPrio = -1;
            RegionProcessor regionProcessor = delegate (Region r)
            {
                int regionsSeenScan;
                if (RegionTraverser.ShouldCountRegion(r))
                {
                    regionsSeenScan = regionsSeenScan2;
                    regionsSeenScan2++;
                }
                if (!r.IsDoorway && !r.Allows(traverseParams, true))
                {
                    return false;
                }
                if (!ignoreEntirelyForbiddenRegions || !r.IsForbiddenEntirely(traverseParams.pawn))
                {
                    foreach (var item in thingList)
                    {
                        if (ReachabilityWithinRegion.ThingFromRegionListerReachable(item, r, peMode, traverseParams.pawn))
                        {
                            float num = (priorityGetter != null) ? priorityGetter(item) : 0f;
                            if (num >= bestPrio)
                            {
                                float num2 = (float)(item.Position - root).LengthHorizontalSquared;
                                if ((num > bestPrio || num2 < closestDistSquared) && num2 < mDsq && (validator == null || validator(item)))
                                {
                                    closestThing = item;
                                    closestDistSquared = num2;
                                    bestPrio = num;
                                }
                            }
                        }
                    }
                }
                return regionsSeenScan2 >= minRegions && closestThing != null;
            };
            
            RegionTraverser.BreadthFirstTraverse(region, entryCondition, regionProcessor, maxRegions, traversableRegionTypes);
            regionsSeen = regionsSeenScan2;
            return closestThing;
        }
    }
}
