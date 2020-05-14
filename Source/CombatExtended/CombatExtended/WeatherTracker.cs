using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class WeatherTracker : MapComponent
    {
        private static readonly string[] windDirections = new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        private const float BaseWindStrength = 3;    // With 2 max noise (see vanilla WindManager class), 1.5 multiplier from weather we get a 9 on Beaufort scale
        private const float MaxWindStrength = 9;
        private const float MaxDirectionDelta = 5f;

        private float _humidity = FireSpread.values.maxHumidity * 0.5f;
        private float _windDirection;
        private float _windDirectionTarget;

        public float Humidity
        {
            get => _humidity;
            private set => _humidity = Mathf.Clamp(value, 0, FireSpread.values.maxHumidity);
        }

        public float HumidityPercent => Humidity / FireSpread.values.maxHumidity;
        public Vector3 WindDirection => Vector3Utility.FromAngleFlat(_windDirection - 90);

        private float WindStrength => Mathf.Min(BaseWindStrength * map.windManager.WindSpeed, MaxWindStrength);
        private int BeaufortScale => Mathf.FloorToInt(WindStrength);

        private string WindStrengthText => ("CE_Wind_Beaufort" + BeaufortScale).Translate();

        private string WindDirectionText
        {
            get
            {
                if (BeaufortScale == 0)
                {
                    return "";
                }

                var directionAngle = 360f / windDirections.Length;
                int windDirectionsPosition = Mathf.Clamp(Mathf.RoundToInt((_windDirection - directionAngle * 0.5f) / directionAngle), 0, windDirections.Length - 1);
                return ", " + ("CE_Wind_Direction_" + windDirections[windDirectionsPosition]).Translate();
            }
        }

        public WeatherTracker(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _humidity, "humidity", FireSpread.values.maxHumidity * 0.5f);
            Scribe_Values.Look(ref _windDirection, "windDirection");
            Scribe_Values.Look(ref _windDirectionTarget, "windDirectionTarget");
        }

        public float GetWindStrengthAt(IntVec3 cell)
        {
            if (!cell.UsesOutdoorTemperature(map))
                return 0;

            return WindStrength;
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            // Rain
            if (map.weatherManager.RainRate > 0)
            {
                Humidity += map.weatherManager.RainRate * FireSpread.values.humidityIncreaseMultiplier;
            }
            else
            {
                Humidity -= FireSpread.values.humidityDecayPerTick;
            }

            // Wind
            if (GenTicks.TicksGame % GenTicks.TickRareInterval == 0)
            {
                if (Math.Abs(_windDirection - _windDirectionTarget) < 1)
                {
                    _windDirectionTarget = Rand.Range(0, 360);
                }

                _windDirection = Mathf.MoveTowardsAngle(_windDirection, _windDirectionTarget, Rand.Range(0, MaxDirectionDelta));
            }
        }

        public void DoWindGUI(float xPos, ref float yPos)
        {
            float widgetHorizontalOffset = 100f;
            float widgetWidth = 200f + widgetHorizontalOffset;
            float widgetHeight = 26f;
            Rect rect = new Rect(xPos - widgetHorizontalOffset, yPos - widgetHeight, widgetWidth, widgetHeight);
            Text.Anchor = TextAnchor.MiddleRight;
            rect.width -= 15f;
            Text.Font = GameFont.Small;
            Widgets.Label(rect, WindStrengthText + WindDirectionText);
            TooltipHandler.TipRegion(rect, "CE_Wind_Tooltip".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            yPos -= widgetHeight;   //value updated by ref, so the transpiled method has the correct vertical position for the next widget
        }

    }
}