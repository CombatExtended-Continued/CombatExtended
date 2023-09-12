using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProjectileImpactFX
{
    // ProjectileImpactFX.EffectProjectileExtension
    public class EffectProjectileExtension : DefModExtension
    {
        public bool explosionFleck = false;
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

        public void ThrowFleck(Vector3 loc, Map map, Color color, SoundDef sound, Thing hitThing = null)
        {
            FleckDef ExplosionFleck = explosionFleckDef != string.Empty ? DefDatabase<FleckDef>.GetNamed(this.explosionFleckDef) : null;
            FleckDef ImpactFleck = ImpactFleckDef != string.Empty ? DefDatabase<FleckDef>.GetNamed(this.ImpactFleckDef) : null;
            FleckDef ImpactGlowFleck = ImpactGlowFleckDef != string.Empty ? DefDatabase<FleckDef>.GetNamed(this.ImpactGlowFleckDef) : null;
            SoundDef ImpactSound = ImpactSoundDef != string.Empty ? DefDatabase<SoundDef>.GetNamed(this.ImpactSoundDef) : null;
            float explosionSize = this.explosionFleckSizeRange?.RandomInRange ?? this.explosionFleckSize;
            float ImpactFleckSize = this.ImpactFleckSizeRange?.RandomInRange ?? this.ImpactFleckSize;
            float ImpactGlowFleckSize = this.ImpactGlowFleckSizeRange?.RandomInRange ?? this.ImpactGlowFleckSize;
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            Rand.PushState();
            float rotationRate = Rand.Range(-30f, 30f);
            float initialRotation = Rand.Range(0, 360);
            float VelocityAngle = (float)Rand.Range(-30, 30);
            float VelocitySpeed = Rand.Range(0.5f, 1f);
            Rand.PopState();
            if (ImpactFleck != null)
            {
                if (explosionEffecter != null)
                {
                    TriggerEffect(explosionEffecter, loc, map);
                }

                FleckCreationData creationData = FleckMaker.GetDataStatic(loc, map, ImpactFleck);
                creationData.rotation = initialRotation;
                creationData.velocityAngle = VelocityAngle;
                creationData.velocitySpeed = VelocitySpeed;
                creationData.scale = ImpactFleckSize;
                creationData.spawnPosition = loc;
                map.flecks.CreateFleck(creationData);
                //}
            }
            if (ImpactGlowFleck != null)
            {
                FleckCreationData creationData = FleckMaker.GetDataStatic(loc, map, ImpactGlowFleck);
                creationData.scale = ImpactGlowFleckSize;
                map.flecks.CreateFleck(creationData);
            }
            if (explosionFleck)
            {
                if (ExplosionFleck != null)
                {
                    FleckCreationData creationData = FleckMaker.GetDataStatic(loc, map, ExplosionFleck);
                    creationData.scale = explosionSize;
                    creationData.rotationRate = rotationRate;
                    creationData.spawnPosition = loc;
                    creationData.instanceColor = color;
                    creationData.velocityAngle = VelocityAngle;
                    creationData.velocitySpeed = VelocitySpeed;
                    map.flecks.CreateFleck(creationData);
                }
            }
            if (ImpactSound != null)
            {
                ImpactSound.PlayOneShot(new TargetInfo(loc.ToIntVec3(), map));
            }
        }

        void TriggerEffect(EffecterDef effect, Vector3 position, Map map, Thing hitThing = null)
        {
            TriggerEffect(effect, IntVec3.FromVector3(position), map);
        }

        void TriggerEffect(EffecterDef effect, IntVec3 dest, Map map)
        {
            if (effect == null)
            {
                return;
            }

            var targetInfo = new TargetInfo(dest, map, false);

            Effecter effecter = effect.Spawn();
            effecter.Trigger(targetInfo, targetInfo);
            effecter.Cleanup();
        }

    }
}
