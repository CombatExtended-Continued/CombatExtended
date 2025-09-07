using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class CompCharges : ThingComp
    {
        private static float MaxRangeAngle = Mathf.Deg2Rad * 45;

        public CompProperties_Charges Props => (CompProperties_Charges)props;

        public bool GetChargeBracket(float range, float shotHeight, float gravityFactor, out Vector2 bracket)
        {
            bracket = new Vector2(0, 0);
            if (Props.chargeSpeeds.Count <= 0)
            {
                Log.Error("Tried getting charge bracket from empty list.");
                return false;
            }
            foreach (var speed in Props.chargeSpeeds)
            {
                var curRange = CE_Utility.MaxProjectileRange(shotHeight, speed, MaxRangeAngle, gravityFactor);
                if (range <= curRange)
                {
                    bracket = new Vector2(speed, curRange);
                    return true;
                }
            }
            return false;
        }
    }
}
