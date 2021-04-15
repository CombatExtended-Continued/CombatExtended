using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    class Plant_Blazebulb : Plant
    {
        private const int IgnitionTemp = 28;                    // Temperature (in Celsius) above which the plant will start catching fire

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_ObtainingPrometheum, KnowledgeAmount.Total);
        }

        public override void TickLong()
        {
            base.TickLong();
            if (Destroyed)
            {
              return;
            }
                float temperature = Position.GetTemperature(base.Map);
                if (temperature > IgnitionTemp)
                {
                    float ignitionChance = 0.005f * Mathf.Pow((temperature - IgnitionTemp), 2);
                    float rand = UnityEngine.Random.value;
                    if (UnityEngine.Random.value < ignitionChance)
                    {
                        FireUtility.TryStartFireIn(Position, base.Map, 0.1f);
                    }
                }
            }
        }

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            if (dinfo.Def != DamageDefOf.Rotting && SpawnedOrAnyParentSpawned)
            {
                // Find existing fuel puddle or spawn one if needed
                Thing fuel = PositionHeld.GetThingList(MapHeld).FirstOrDefault(x => x.def == CE_ThingDefOf.FilthPrometheum);
                int fuelHpFromDamage = Mathf.CeilToInt(CE_ThingDefOf.FilthPrometheum.BaseMaxHitPoints * Mathf.Clamp01(totalDamageDealt / MaxHitPoints));
                if (fuel != null)
                {
                    fuel.HitPoints = Mathf.Min(fuel.MaxHitPoints, fuel.HitPoints + fuelHpFromDamage);
                }
                else
                {
                    fuel = ThingMaker.MakeThing(CE_ThingDefOf.FilthPrometheum);
                    GenSpawn.Spawn(fuel, PositionHeld, MapHeld);
                    fuel.HitPoints = fuelHpFromDamage;
                }
            }
            base.PostApplyDamage(dinfo, totalDamageDealt);
        }
    }
}
