using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class WeatherTracker : MapComponent
    {
        private const float HumidityDecayPerTick = 0.1f;
        private const int MaxPrecipitation = GenDate.TicksPerDay * 2;
        private const float MaxWindStrength = 6;    // With 1.5 multiplier from weather we get a 9 on Beaufort scale
        private const float MaxWindStrengthDelta = 0.5f;
        private const float MaxDirectionDelta = 5f;

        private float _humidity = MaxPrecipitation * 0.5f;
        private float _windStrength;
        private float _windStrengthTarget;
        private float _windDirection;
        private float _windDirectionTarget;

        public float Humidity
        {
            get => _humidity;
            private set => _humidity = Mathf.Clamp(value, 0, MaxPrecipitation);
        }

        public float HumidityPercent => Humidity / MaxPrecipitation;
        public float WindStrength => _windStrength * map.weatherManager.CurWindSpeedFactor;
        public Vector3 WindDirection => Vector3Utility.FromAngleFlat(_windDirection);

        public WeatherTracker(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            // Rain
            if (map.weatherManager.RainRate > 0)
            {
                Humidity += map.weatherManager.RainRate;
            }
            else
            {
                Humidity -= HumidityDecayPerTick;
            }

            // Wind
            if (GenTicks.TicksGame % GenTicks.TickRareInterval == 0)
            {
                if (Math.Abs(_windStrengthTarget - _windStrength) < 0.1f)
                {
                    _windStrengthTarget = Rand.Range(0, MaxWindStrength);
                }

                _windStrength = Mathf.MoveTowards(_windStrength, _windStrengthTarget, Rand.Range(0, MaxWindStrengthDelta));
                Log.Message($"CE :: Wind strength set to {_windStrength}, trending towards {_windStrengthTarget}");

                if (Math.Abs(_windDirection - _windDirectionTarget) < 1)
                {
                    _windDirectionTarget = Rand.Range(0, 360);
                }

                _windDirection = Mathf.MoveTowardsAngle(_windDirection, _windDirectionTarget, Rand.Range(0, MaxDirectionDelta));
                Log.Message($"CE :: Wind angle set to {_windDirection}, trending towards {_windDirectionTarget}");
            }
        }
    }
}