using CombatExtended;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;

namespace ProjectileImpactFX
{
    // ProjectileImpactFX.EffectProjectileExtension
    public class EffectProjectileExtension : DefModExtension
    {
        public bool AutoAssign = true;
        public bool CreateTerrainEffects = true;

        public string explosionFleckDef = string.Empty;
        public float explosionFleckSize = 1f;
        public EffecterDef explosionEffecter;
        public FloatRange? explosionFleckSizeRange;

        public string ImpactFleckDef = string.Empty;
        public float ImpactFleckSize = 1f;
        public FloatRange? ImpactFleckSizeRange;
        public string ImpactSoundDef = string.Empty;
        public string ImpactGlowFleckDef = string.Empty;
        public float ImpactGlowFleckSize = 1f;
        public FloatRange? ImpactGlowFleckSizeRange;

        public bool muzzleFlare = false;
        public string muzzleFlareDef = string.Empty;
        public float muzzleFlareSize = 1f;
        public bool muzzleSmoke = false;
        public string muzzleSmokeDef = string.Empty;
        public float muzzleSmokeSize = 0.35f;
    }
}
