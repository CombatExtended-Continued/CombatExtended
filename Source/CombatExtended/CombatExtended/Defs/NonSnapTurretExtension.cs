using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended
{
    public class NonSnapTurretExtension : DefModExtension
    {
        public float speed = 1f;

        public float preferedAngleRange = -1;

        public float angleWeightMultiplier = 1;

        public float minAngleWeight = 0.1f;

        public float TweakWeight(float weight, float angle)
        {
            if (angle < minAngleWeight || angle < preferedAngleRange) { angle = minAngleWeight; }
            angle *= angleWeightMultiplier;
            return weight / angle;
        }
    }
}
