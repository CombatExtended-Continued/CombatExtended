using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class GunDrawExtension : DefModExtension
    {
        public Vector2 DrawSize = Vector2.one;
        public Vector2 DrawOffset = Vector2.zero;

        public float recoilModifier = 1;
        public float recoilScale = -1;
        public int recoilTick = -1;
        public float muzzleJumpModifier = -1;

        //Angle: eject direction
        //Rotation: casing's orientation

        public Vector2 CasingOffset = Vector2.zero;
        public float CasingAngleOffset = 0;

        public bool AdvancedCasingVariables = false;

        public FloatRange CasingAngleOffsetRange = new FloatRange(0, 0);
        public float CasingLifeTimeMultiplier = -1;
        //having a min value below 0 disables lifetime override range. Override disables multiplier
        public FloatRange CasingLifeTimeOverrideRange = new FloatRange(-1, 1);

        public float CasingSpeedMultiplier = -1;
        public FloatRange CasingSpeedOverrideRange = new FloatRange(-1, 1);

        public float CasingSizeOffset = 1;

        //for edge cases (read: Phalanx or Type-1130, where casings fell from a deflector chute)
        public Vector2 CasingOffsetRandomRange = Vector2.zero;
        public float CasingRotationRandomRange = 0;
    }
}
