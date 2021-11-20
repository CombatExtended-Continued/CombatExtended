using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using CombatExtended.WorldObjects;
using System.Linq;
using System.Diagnostics;

namespace CombatExtended
{
    public static class AttritionUtility
    {
        private const float WORLD_SHELLDAMAGE = 0.015f;

        private const float MAP_SHELLMINDAMAGE = 180;
        private const float MAP_SHELLMAXDAMAGE = 800;
        private const float MAP_SHELLMINRADIUS = 6f;
        private const float MAP_SHELLMAXRADIUS = 20f;
        private const float MAP_BURNCHANCE = 0.3334f;        

        private const int MAP_MINSITECOUNT = 6;
        private const int MAP_GENLIMITER = 128;

        private const int SHADOW_CARRYLIMIT = 5;
        private const float SITE_MINDIST = 2;        

        private struct AttritionRequest
        {            
            public int damage;
            public int radius;

            public bool IsValid
            {
                get => damage > 0 && radius > MAP_SHELLMINRADIUS;
            }           

            public AttritionRequest(int damage, int radius)
            {
                this.damage = damage;
                this.radius = radius;                
            }

            public static AttritionRequest For(ProjectilePropertiesCE props)
            {
                var request = new AttritionRequest();                                
                var dmg = props.damageAmountBase;
                if (props.shellingProps != null)
                {
                    dmg = Math.Max(dmg, Mathf.CeilToInt(props.shellingProps.damage / WORLD_SHELLDAMAGE * MAP_SHELLMINDAMAGE));
                }                
                request.damage = dmg;
                request.radius = Math.Max(Mathf.CeilToInt(props.explosionRadius), (int)MAP_SHELLMINRADIUS);
                return request;
            }            
        }

        private struct AttritionResult
        {            
            public IntVec3 position;
            public AttritionRequest request;
            
            public int Damage => request.damage;
            public int Radius => request.radius;
        }
               
        private static Map map;        

        private static readonly HashSet<IntVec3> attritionedCells = new HashSet<IntVec3>();        
        private static readonly List<AttritionRequest> requests = new List<AttritionRequest>();
        private static readonly List<AttritionResult> results = new List<AttritionResult>();

        public static bool TryApplyAttrition(Map map)
        {            
            if (map.Parent == null)
            {
                return false;
            }
            HealthComp healthComp = map.Parent.GetComponent<HealthComp>();
            if (healthComp == null || healthComp.Health >= 0.911f)
            {
                return false;
            }
            int siteCount = GetSiteCount(1f - healthComp.Health);                      
            if (siteCount == 0)
            {
                return false;
            }
            requests.Clear();
            for (int i = 0;i < siteCount; i++)
            {
                AttritionRequest request = new AttritionRequest();
                request.damage = GetRandomDamage();
                request.radius = GetRandomRadius();
                requests.Add(request);
            }
            AttritionUtility.map = map;            
            Execute();            
            AttritionUtility.map = null;
            return results.Count != 0;
        }

        public static void ApplyAttrition(Map map, IEnumerable<ThingDef> shellsDef) => ApplyAttrition(map, shellsDef.Select(s => s.projectile as ProjectilePropertiesCE));
        public static void ApplyAttrition(Map map, IEnumerable<ProjectilePropertiesCE> shells)
        {            
            AttritionUtility.map = map;            
            requests.Clear();            
            if (!shells.EnumerableNullOrEmpty())
            {
                requests.AddRange(shells.Select(s => AttritionRequest.For(s)));
                Execute();                               
            }            
            AttritionUtility.map = null;
        }

        private static void Execute()
        {
            results.Clear();
            attritionedCells.Clear();
            try
            {                                                               
                int limiter;                
                AttritionRequest request;
                requests.Shuffle();
                for (int i = 0; i < requests.Count; i++)
                {
                    request = requests[i];
                    limiter = MAP_GENLIMITER;
                    while (limiter-- > 0)
                    {
                        IntVec3 cell = GetRandomSiteCell();
                        RoofDef roof = cell.GetRoof(map);
                        if (roof != RoofDefOf.RoofRockThick && !results.Any(d => d.position.DistanceToSquared(cell) < SITE_MINDIST) && TryExplode(cell, request, out AttritionResult result))
                        {
                            results.Add(result);
                            break;
                        }
                    }
                }               
            }
            catch (Exception er)
            {
                Log.Error($"CE: GenStep_Attrition Failed with error {er}");
            }
        }       

        private static bool TryExplode(IntVec3 center, AttritionRequest request, out AttritionResult attritionResult)
        {
            attritionResult = new AttritionResult();
            attritionResult.request = request;
            attritionResult.position = center;
            
            float radiusSquared = request.radius * request.radius;
            float damageBase = request.damage;

            Action<IntVec3, int> processor = (cell, carry) =>
            {
                var distSquared = -1f;
                if (!cell.InBounds(map) || (distSquared = cell.DistanceToSquared(center)) > radiusSquared)
                {
                    return;
                }                
                
                attritionedCells.Add(cell);
                
                int filthMadeState = 0;                
                int damage = Mathf.CeilToInt(Mathf.Lerp(damageBase / 2, damageBase, 0.5f * (1f - distSquared / radiusSquared) + 0.5f * (SHADOW_CARRYLIMIT - carry) / SHADOW_CARRYLIMIT));
                
                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (!thing.def.useHitPoints)
                    {
                        continue;
                    }
                    thing.hitPointsInt -= damage * (thing.IsPlant() ? 3 : 1);
                    if (thing.hitPointsInt > 0)
                    {
                        if (filthMadeState != 2 && Rand.Chance(0.5f))
                        {
                            ScatterDebrisUtility.ScatterFilthAroundThing(thing, map, ThingDefOf.Filth_RubbleBuilding);
                            filthMadeState = 2;
                        }                        
                    }
                    else
                    {                        
                        thing.DeSpawn(DestroyMode.Vanish);
                        thing.Destroy(DestroyMode.Vanish);
                        if (thing.def.MakeFog)
                        {
                            map.fogGrid.Notify_FogBlockerRemoved(cell);
                        }
                        if (thing.def.holdsRoof)
                        {
                            RoofCollapseCellsFinder.ProcessRoofHolderDespawned(thing.OccupiedRect(), cell, map, true);
                        }                        
                        if (thing.def.category == ThingCategory.Plant && (thing.def.plant?.IsTree ?? false))
                        {
                            Thing burntTree = ThingMaker.MakeThing(ThingDefOf.BurnedTree);
                            burntTree.positionInt = cell;
                            burntTree.SpawnSetup(map, false);
                            if (filthMadeState != 2 && Rand.Chance(0.5f))
                            {
                                ScatterDebrisUtility.ScatterFilthAroundThing(burntTree, map, ThingDefOf.Filth_Ash);
                                filthMadeState = 2;
                            }
                        }
                        if (thing.def.MakeFog)
                        {
                            map.fogGrid.Notify_FogBlockerRemoved(cell);
                        }
                        ThingDef filth = thing.def.filthLeaving ?? (Rand.Chance(0.5f) ? ThingDefOf.Filth_Ash : ThingDefOf.Filth_RubbleBuilding);
                        if (filthMadeState == 0 && FilthMaker.TryMakeFilth(cell, map, filth, Rand.Range(1, 3), FilthSourceFlags.Any))
                        {
                            filthMadeState = 1;
                        }
                    }
                }
                map.snowGrid.SetDepth(cell, 0);
                map.roofGrid.SetRoof(cell, null);
                if (Rand.Chance(0.33f) && map.terrainGrid.CanRemoveTopLayerAt(cell))
                {
                    map.terrainGrid.RemoveTopLayer(cell, false);
                }
                if (Rand.Chance(0.05f))
                {
                    FireUtility.TryStartFireIn(cell, map, Rand.Range(0.5f, 1.5f));
                }
            };
            processor(center, 0);
            ShadowCastingUtility.CastWeighted(map, center, processor, request.radius, SHADOW_CARRYLIMIT, out int count);            
            return count > 0;
        }       

        private static IntVec3 GetRandomSiteCell() => new IntVec3((int)CE_Utility.RandomGaussian(5, map.Size.x - 5), 0, (int)CE_Utility.RandomGaussian(5, map.Size.z - 5));

        private static int GetSiteCount(float missingHealth) => Mathf.CeilToInt(missingHealth / WORLD_SHELLDAMAGE);

        private static int GetRandomDamage() => Mathf.CeilToInt(CE_Utility.RandomGaussian(MAP_SHELLMINDAMAGE, MAP_SHELLMAXDAMAGE));
        private static int GetRandomRadius() => Mathf.CeilToInt(CE_Utility.RandomGaussian(MAP_SHELLMINRADIUS, MAP_SHELLMAXRADIUS));
    }
}

