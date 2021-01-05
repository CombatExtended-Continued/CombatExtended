using System;
using Verse;

namespace ProjectileImpactFX
{
    // ProjectileImpactFX.BarrelOffsetExtension
    public class BarrelOffsetExtension : DefModExtension
    {
        public float barrellength = 1f;
        public bool muzzleFlare = true;
        public string muzzleFlareDef = string.Empty;
        public float muzzleFlareSize = 1f;
        public bool muzzleSmoke = true;
        public string muzzleSmokeDef = string.Empty;
        public float muzzleSmokeSize = 0.35f;
    }
}
