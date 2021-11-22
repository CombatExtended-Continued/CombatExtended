using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using CombatExtended.WorldObjects;

namespace CombatExtended
{
    public static class ShellingUtility
    {
        private static Map map;
        private static ThingDef shellDef;
        private static ProjectilePropertiesCE props;
        private static DamageDef projectileDamageDef;        

        public static IntVec3 FindRandomImpactCell(Map map, ThingDef shellDef = null)
        {
            ShellingUtility.map = map;
            ShellingUtility.shellDef = shellDef;
            IntVec3 result = IntVec3.Invalid;
            if (map.IsPlayerHome)
            {
                props = (ProjectilePropertiesCE)shellDef.projectile;
                projectileDamageDef = props.damageDef;
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                int i = 5;
                while (i-- >= 0 && stopwatch.ElapsedTicks / Stopwatch.Frequency < 0.01f)
                {
                    result = GetRandomPlayerHomeImpactCell();
                    if (result.IsValid)
                    {
                        return result;
                    }                    
                }
                stopwatch.Stop();
            }
            if(!result.IsValid)
            {
                result = GetRandomImpactCell();
            }            
            map = null;            
            return result;
        }

        private static IntVec3 GetRandomPlayerHomeImpactCell()
        {                     
            if(projectileDamageDef == CE_DamageDefOf.Bomb)
            {
                if(Rand.Chance(props.damageAmountBase / 5000f) && map.areaManager.Home.innerGrid.trueCountInt > 0) // make more powerful shells land near the player base
                {
                    return GetRandomImpectCell(map.areaManager.Home.innerGrid);                    
                }
                else if (Rand.Chance(0.05f) && map.zoneManager.zoneGrid.Length > 0)
                {
                    IEnumerable<Zone> zones = map.zoneManager.zoneGrid.Where(z => z != null && z.cells?.Count > 0);
                    if (!zones.EnumerableNullOrEmpty())
                    {
                        return GetRandomImpectCell(zones.RandomElement().cells);
                    }                    
                }
            }
            else if(projectileDamageDef == CE_DamageDefOf.PrometheumFlame)
            {
                if (Rand.Chance(0.05f))
                {
                    foreach(var zone in map.zoneManager.zoneGrid.InRandomOrder())
                    {
                        if (zone is Zone_Growing growing && !growing.cells.NullOrEmpty())
                        {
                            return GetRandomImpectCell(zone.cells);
                        }
                    }
                }               
            }
            return IntVec3.Invalid;
        }

        private static readonly List<IntVec3> potentialCells = new List<IntVec3>(10000);
        private static IntVec3 GetRandomImpectCell(BoolGrid grid)
        {
            potentialCells.Clear();
            int processedCount = 0;            
            int startIndex = ((!grid.minPossibleTrueIndexDirty) ? grid.minPossibleTrueIndexCached : 0);            
            for (int i = startIndex; i < grid.arr.Length && processedCount < grid.trueCountInt / 2; i += (int) Rand.Range(1, 32))
            {                
                if (grid.arr[i])
                {                    
                    IntVec3 cell = CellIndicesUtility.IndexToCell(i, grid.mapSizeX);
                    if (Valid(cell))
                    {
                        processedCount++;
                        potentialCells.Add(cell);
                    }                       
                }
            }            
            return potentialCells.RandomElementWithFallback(IntVec3.Invalid);
        }

        private static IntVec3 GetRandomImpectCell(List<IntVec3> cells)
        {           
            int i = 0;
            while(i++ < cells.Count / 2)
            {
                IntVec3 cell = cells[Rand.Range(0, (int) 1e5) % cells.Count];
                if(Valid(cell))
                {
                    return cell;
                }
            }
            return IntVec3.Invalid;
        }

        private static IntVec3 GetRandomImpactCell()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.ElapsedTicks / Stopwatch.Frequency < 0.005f)
            {
                IntVec3 cell = new IntVec3(Rand.Range(5, map.cellIndices.mapSizeX - 5), 0, Rand.Range(5, map.cellIndices.mapSizeZ - 5));
                if (Valid(cell))
                {
                    return cell;
                }
            }
            stopwatch.Stop();
            return IntVec3.Invalid;
        }

        private static bool Valid(IntVec3 cell)
        {
            RoofDef roof = map.roofGrid.RoofAt(cell);
            if (roof == null || roof == RoofDefOf.RoofConstructed)
            {
                return true;
            }
            return false;
        }
    }
}

