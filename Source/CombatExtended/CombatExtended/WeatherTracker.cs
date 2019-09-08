using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class WeatherTracker : MapComponent
    {
        private static readonly string[] windDirections = new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        private const float HumidityDecayPerTick = 0.1f;
        private const int MaxPrecipitation = GenDate.TicksPerDay * 2;
        private const float MaxWindStrength = 6;    // With 1.5 multiplier from weather we get a 9 on Beaufort scale
        private const float BeaufortScaleMultiplier = 1.5f;
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

        private int BeaufortScale => Mathf.RoundToInt(_windStrength * BeaufortScaleMultiplier);

        private string WindStrengthText => ("CE_Wind_Beaufort" + BeaufortScale).Translate();

        private string WindDirectionText
        {
            get
            {
                if (BeaufortScale == 0)
                {
                    return "";
                }
                int windDirectionsPosition = Mathf.Clamp(Mathf.RoundToInt(_windDirection / (360f / windDirections.Length)), 0, windDirections.Length - 1);
                return ", " + ("CE_Wind_Direction_" + windDirections[windDirectionsPosition]).Translate();
            }
        }

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
                //Log.Message($"CE :: Wind strength set to {_windStrength}, trending towards {_windStrengthTarget}");

                if (Math.Abs(_windDirection - _windDirectionTarget) < 1)
                {
                    _windDirectionTarget = Rand.Range(0, 360);
                }

                _windDirection = Mathf.MoveTowardsAngle(_windDirection, _windDirectionTarget, Rand.Range(0, MaxDirectionDelta));
                //Log.Message($"CE :: Wind angle set to {_windDirection}, trending towards {_windDirectionTarget}");
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