using Verse;

namespace ProjectileImpactFX
{
    // ProjectileImpactFX.EffectProjectileExtension
    public class EffectProjectileExtension : DefModExtension
    {
        public bool AutoAssign = true;
        public bool CreateTerrainEffects = true;

        public FleckDef explosionFleckDef;
        public float explosionFleckSize = 1f;
        public EffecterDef explosionEffecter;
        public FloatRange? explosionFleckSizeRange;

        public FleckDef ImpactFleckDef;
        public float ImpactFleckSize = 1f;
        public FloatRange? ImpactFleckSizeRange;
        public SoundDef ImpactSoundDef;
        public FleckDef ImpactGlowFleckDef;
        public float ImpactGlowFleckSize = 1f;
        public FloatRange? ImpactGlowFleckSizeRange;
        public FleckDef StuckProjectileFleckDef;
        public float StuckProjectileFleckSize = 1f;

        public bool muzzleFlare = false;
        public string muzzleFlareDef = string.Empty;
        public float muzzleFlareSize = 1f;
        public bool muzzleSmoke = false;
        public string muzzleSmokeDef = string.Empty;
        public float muzzleSmokeSize = 0.35f;
    }
}
