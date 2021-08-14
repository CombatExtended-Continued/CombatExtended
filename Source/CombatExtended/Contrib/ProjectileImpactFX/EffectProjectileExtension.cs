using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProjectileImpactFX
{
    // ProjectileImpactFX.EffectProjectileExtension
    public class EffectProjectileExtension : DefModExtension
    {
        public bool explosionMote = false;
        public string explosionMoteDef = string.Empty;
        public float explosionMoteSize = 1f;
        public EffecterDef explosionEffecter;
        public FloatRange? explosionMoteSizeRange;
        public string ImpactMoteDef = string.Empty;
        public float ImpactMoteSize = 1f;
        public FloatRange? ImpactMoteSizeRange;
        public string ImpactGlowMoteDef = string.Empty;
        public float ImpactGlowMoteSize = 1f;
        public FloatRange? ImpactGlowMoteSizeRange;
        public bool muzzleFlare = false;
        public string muzzleFlareDef = string.Empty;
        public float muzzleFlareSize = 1f;
        public bool muzzleSmoke = false;
        public string muzzleSmokeDef = string.Empty;
        public float muzzleSmokeSize = 0.35f;

        public void ThrowMote(Vector3 loc, Map map, ThingDef explosionMoteDef, Color color, SoundDef sound, Thing hitThing = null)
        {
            ThingDef explosionmoteDef = explosionMoteDef;
            ThingDef ImpactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(this.ImpactMoteDef) ?? null;
            ThingDef ImpactGlowMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(this.ImpactGlowMoteDef) ?? null;
            float explosionSize = this.explosionMoteSize;
            float ImpactMoteSize = this.ImpactMoteSizeRange?.RandomInRange ?? this.ImpactMoteSize;
            float ImpactGlowMoteSize = this.ImpactGlowMoteSizeRange?.RandomInRange ?? this.ImpactGlowMoteSize;
            if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            Rand.PushState();
            float rotationRate = Rand.Range(-30f, 30f);
            float VelocityAngel = (float)Rand.Range(0, 360);
            float VelocitySpeed = Rand.Range(0.48f, 0.72f);
            Rand.PopState();
            if (ImpactGlowMoteDef != null)
            {
                MoteMaker.MakeStaticMote(loc, map, ImpactGlowMoteDef, ImpactGlowMoteSize);
            }
            if (explosionMote)
            {
                if (!this.explosionMoteDef.NullOrEmpty())
                {
                    ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(this.explosionMoteDef);
                    if (def != null)
                    {
                        explosionmoteDef = def;
                    }
                }
                if (explosionmoteDef != null)
                {
                    MoteThrown moteThrown;
                    moteThrown = (MoteThrown)ThingMaker.MakeThing(explosionmoteDef, null);
                    moteThrown.Scale = explosionSize;
                    Rand.PushState();
                    moteThrown.rotationRate = Rand.Range(-30f, 30f);
                    Rand.PopState();
                    moteThrown.exactPosition = loc;
                    moteThrown.instanceColor = color;
                    moteThrown.SetVelocity(VelocityAngel, VelocitySpeed);
                    GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map, WipeMode.Vanish);
                }

            }
            if (ImpactMoteDef != null)
            {
                if (hitThing != null && hitThing is Pawn pawn)
                {
                    ImpactMoteDef = ThingDef.Named("Mote_BloodPuff");
                    if (sound != null)
                    {
                        sound.PlayOneShot(new TargetInfo(loc.ToIntVec3(), map, false));
                    }
                    MoteThrown moteThrown;
                    moteThrown = (MoteThrown)ThingMaker.MakeThing(ImpactMoteDef, null);
                    moteThrown.Scale = ImpactMoteSize;
                    Rand.PushState();
                    moteThrown.rotationRate = Rand.Range(-30f, 30f);
                    Rand.PopState();
                    moteThrown.exactPosition = loc;
                    moteThrown.instanceColor = pawn.RaceProps.BloodDef.graphic.color;
                    moteThrown.SetVelocity(VelocityAngel, VelocitySpeed);
                    GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map, WipeMode.Vanish);
                    if (explosionEffecter != null)
                    {
                        TriggerEffect(explosionEffecter, loc, map, hitThing);
                    }
                }
                else
                {
                    if (explosionEffecter != null)
                    {
                        TriggerEffect(explosionEffecter, loc, map);
                    }
                    MoteMaker.MakeStaticMote(loc, map, ImpactMoteDef, ImpactMoteSize);
                }
            }
        }

        void TriggerEffect(EffecterDef effect, Vector3 position, Map map, Thing hitThing = null)
        {
            TriggerEffect(effect, IntVec3.FromVector3(position), map);
        }

        void TriggerEffect(EffecterDef effect, IntVec3 dest, Map map)
        {
            if (effect == null) return;

            var targetInfo = new TargetInfo(dest, map, false);

            Effecter effecter = effect.Spawn();
            effecter.Trigger(targetInfo, targetInfo);
            effecter.Cleanup();
        }

    }
}
