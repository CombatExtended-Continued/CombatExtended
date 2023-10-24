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

        public Vector2 CasingOffset = Vector2.zero;
        public float CasingAngleOffset = 0;

    }
}
