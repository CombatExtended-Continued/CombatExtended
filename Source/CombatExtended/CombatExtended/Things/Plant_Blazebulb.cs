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
        private const int ignitionTemp = 28;                    // Temperature (in Celsius) above which the plant will start catching fire

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_ObtainingPrometheum, KnowledgeAmount.Total);
        }

        public override void TickLong()
        {
            base.TickLong();
            float temperature = Position.GetTemperature(base.Map);
            if (temperature > ignitionTemp)
            {
                float ignitionChance = 0.005f * Mathf.Pow((temperature - ignitionTemp), 2);
                float rand = UnityEngine.Random.value;
                if(UnityEngine.Random.value < ignitionChance)
                {
                    FireUtility.TryStartFireIn(Position, base.Map, 0.1f);
                }
            }
        }

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);
            if (dinfo.Def != DamageDefOf.Rotting)
            {
                // Find existing fuel puddle or spawn one if needed
                Thing fuel = Position.GetThingList(this.Map).FirstOrDefault(x => x.def == CE_ThingDefOf.FilthPrometheum);
                int fuelHPFromDamage = Mathf.CeilToInt(CE_ThingDefOf.FilthPrometheum.BaseMaxHitPoints * Mathf.Clamp01(totalDamageDealt / MaxHitPoints));
                if (fuel != null)
                {
                    fuel.HitPoints = Mathf.Min(fuel.MaxHitPoints, fuel.HitPoints + fuelHPFromDamage);
                }
                else
                {
                    fuel = ThingMaker.MakeThing(CE_ThingDefOf.FilthPrometheum);
                    GenSpawn.Spawn(fuel, Position, this.Map);
                    fuel.HitPoints = fuelHPFromDamage;
                }
            }
        }
    }
}
