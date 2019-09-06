using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class WeatherTracker : MapComponent
    {
        private const float HumidityDecayPerTick = 0.1f;
        private const int MaxPrecipitation = GenDate.TicksPerDay * 2;

        private float _humidity = MaxPrecipitation * 0.5f;

        public float Humidity
        {
            get => _humidity;
            private set => _humidity = Mathf.Clamp(value, 0, MaxPrecipitation);
        }

        public float HumidityPercent => Humidity / MaxPrecipitation;

        public WeatherTracker(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (map.weatherManager.RainRate > 0)
            {
                Humidity += map.weatherManager.RainRate;
            }
            else
            {
                Humidity -= HumidityDecayPerTick;
            }
        }
    }
}