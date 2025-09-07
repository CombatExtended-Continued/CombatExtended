using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class FireSpreadExtension : DefModExtension
    {
        public int baseSpreadTicks;
        public int minSpreadTicks;
        public float baseGrowthPerTick;
        public float spreadFarBaseChance;
        public float fireSizeMultiplier;
        public float windSpeedMultiplier;
        public float maxHumidity;
        public float humidityDecayPerTick;
        public float humidityIncreaseMultiplier;
        public float flammabilityHumidityMin;
        public float flammabilityHumidityHalf;
        public float flammabilityHumidityMax;
    }

    public static class FireSpread
    {
        public static readonly FireSpreadExtension values = ThingDefOf.Fire.GetModExtension<FireSpreadExtension>();
    }
}
