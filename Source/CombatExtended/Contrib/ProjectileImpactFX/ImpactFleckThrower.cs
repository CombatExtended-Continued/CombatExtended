using CombatExtended;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProjectileImpactFX;
public static class ImpactFleckThrower
{
    public static void ThrowFleck(Vector3 loc, IntVec3 position, Map map, ProjectilePropertiesCE projProps, ThingDef def, Thing hitThing = null, float direction = 0)
    {
        if (!loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
        {
            return;
        }
        EffectProjectileExtension ext = def.HasModExtension<EffectProjectileExtension>() ? def.GetModExtension<EffectProjectileExtension>() : new EffectProjectileExtension();
        FleckDef ExplosionFleck = ext.explosionFleckDef;
        FleckDef ImpactFleck = ext.ImpactFleckDef;
        FleckDef ImpactGlowFleck = ext.ImpactGlowFleckDef;
        FleckDef StuckProjectileFleck = ext.StuckProjectileFleckDef;

        SoundDef ImpactSound = ext.ImpactSoundDef;
        float ExplosionFleckSize = ext.explosionFleckSizeRange?.RandomInRange ?? ext.explosionFleckSize;
        float ImpactFleckSize = ext.ImpactFleckSizeRange?.RandomInRange ?? ext.ImpactFleckSize;
        float ImpactGlowFleckSize = ext.ImpactGlowFleckSizeRange?.RandomInRange ?? ext.ImpactGlowFleckSize;

        Rand.PushState();
        float rotationRate = Rand.Range(-30f, 30f);
        float VelocitySpeed = Rand.Range(0.5f, 1.0f);
        int ImpactFleckCount = 1;
        float ImpactFleckAirTimeLeft = 5f;

        //angle order for multiple smoke flecks going out: first one always up, 2nd and 3rd to the sides, rest random
        float[] velocityAngleArray = { 0f, -60f, 60f };

        DamageDef damageDef = projProps.damageDef;
        if (ext.AutoAssign)
        {
            if (!projProps.secondaryDamage.NullOrEmpty())
            {
                SecondaryDamage secondaryDamage = projProps.secondaryDamage.FirstOrDefault();
                if (secondaryDamage != null && (secondaryDamage.def == CE_DamageDefOf.Flame_Secondary || secondaryDamage.def == CE_DamageDefOf.PrometheumFlame))
                {//API ammo
                    ExplosionFleck = FleckDefOf.MicroSparksFast;
                    ExplosionFleckSize = ScaleToRange(0.2f, 2.0f, 1, 100, secondaryDamage.amount);
                    ImpactGlowFleck = CE_FleckDefOf.Fleck_HeatGlow_API;
                    ImpactGlowFleckSize = ExplosionFleckSize * 4f;

                }
                else if (secondaryDamage != null && (secondaryDamage.def == CE_DamageDefOf.Bomb_Secondary))
                {//AP-HE ammo
                    ExplosionFleck = CE_FleckDefOf.BlastFlame;
                    ExplosionFleckSize = ScaleToRange(0.2f, 1.8f, 1, 100, secondaryDamage.amount);
                    ImpactGlowFleck = FleckDefOf.ExplosionFlash;
                    ImpactGlowFleckSize = ExplosionFleckSize * 7;
                }
                else if (secondaryDamage.def == DamageDefOf.EMP)
                {//Ion ammo
                    ExplosionFleck = CE_FleckDefOf.ElectricalSpark;
                    ExplosionFleckSize = ScaleToRange(0.5f, 4f, 6, 30, secondaryDamage.amount);
                    ImpactGlowFleck = CE_FleckDefOf.Fleck_ElectricGlow_EMP;
                    ImpactGlowFleckSize = ImpactFleckSize * 3;
                }
            }
            else if (damageDef == DamageDefOf.EMP && projProps.explosionRadius == 0)
            {//shotgun EMP and similar
                ExplosionFleck = CE_FleckDefOf.ElectricalSpark;
                ExplosionFleckSize = ScaleToRange(0.5f, 4f, 6, 30, projProps.damageAmountBase);
                ImpactGlowFleck = CE_FleckDefOf.Fleck_ElectricGlow_EMP;
                ImpactGlowFleckSize = ImpactFleckSize * 3;
            }
        }
        if (ext.CreateTerrainEffects)
        {
            if (hitThing == null)
            {//if bullet landed on ground
                TerrainDef terrain = position.GetTerrain(map);
                ImpactFleckSize = ScaleToRange(1.0f, 3.0f, 10, 50, projProps.damageAmountBase);
                if (projProps.damageAmountBase > 30)
                {
                    ImpactFleckCount = 3;
                    VelocitySpeed = Rand.Range(1.5f, 2f);
                    ImpactFleckSize /= 2;
                }

                //Water effects temporarily disabled due to no effecter texture

                //if (terrain.takeSplashes)
                //{//water and swamp impact
                //    //ImpactFleck = CE_FleckDefOf.Fleck_BulletWaterSplash;
                //    TriggerWaterSplashes(loc, map, 5);
                //}
                if (terrain.takeFootprints)
                {//soil and sand impact
                    ImpactFleck = FleckDefOf.DustPuffThick;
                    VelocitySpeed /= 2;
                    ImpactFleckSize = ScaleToRange(2.0f, 4.0f, 10, 50, projProps.damageAmountBase);
                }
                else if (terrain.holdSnowOrSand && (damageDef == DamageDefOf.Bullet || damageDef == DamageDefOf.Cut))
                {//concrete and stone impact from shots. Only terrains that don't hold snow are non-solid ones, like water and vacuum from SOS2
                    ImpactFleck = FleckDefOf.AirPuff;
                    TriggerBulletHole(loc, map, projProps.damageAmountBase < 50 ? ScaleToRange(0.1f, 0.7f, 1, 50, projProps.damageAmountBase) : 0.7f);
                    TriggerScatteredSparks(loc, map, projProps.damageAmountBase >= 10 && Rand.Chance(0.5f) ? (int)ScaleToRange(3, 15, 10, 100, projProps.damageAmountBase) : 2, (-direction) % 360);

                }
            }
            if (hitThing is Building)
            {//bullets that hit a building spread evenly over the building's texture
                float offsetX = 0.3f * hitThing.DrawSize.x;
                float offsetY = 0.3f * hitThing.DrawSize.y;
                TriggerBulletHole(hitThing.DrawPos + new Vector3(Rand.Range(-offsetX, offsetX), 0f, Rand.Range(-offsetY, offsetY)), map, projProps.damageAmountBase < 50 ? ScaleToRange(0.1f, 0.7f, 1, 50, projProps.damageAmountBase) : 0.7f);
            }
        }

        if (ext.explosionEffecter != null)
        {
            TriggerEffect(ext.explosionEffecter, loc, map);
        }
        if (ImpactFleck != null)
        {
            FleckCreationData creationData = FleckMaker.GetDataStatic(loc, map, ImpactFleck);
            for (int i = 0; i < ImpactFleckCount; i++)
            {
                creationData.rotation = Rand.Range(0, 360);
                creationData.velocityAngle = i < 3 ? velocityAngleArray[i] : Rand.Range(-90, 90);
                creationData.velocitySpeed = VelocitySpeed;
                creationData.scale = ImpactFleckSize;
                creationData.spawnPosition = loc;
                creationData.spawnPosition.y += 3f;
                creationData.airTimeLeft = ImpactFleckAirTimeLeft;
                map.flecks.CreateFleck(creationData);
            }
        }
        if (ImpactGlowFleck != null)
        {
            FleckCreationData creationData = FleckMaker.GetDataStatic(loc, map, ImpactGlowFleck);
            creationData.scale = ImpactGlowFleckSize;
            map.flecks.CreateFleck(creationData);
        }

        if (ExplosionFleck != null)
        {
            FleckCreationData creationData = FleckMaker.GetDataStatic(loc, map, ExplosionFleck);
            creationData.scale = ExplosionFleckSize;
            creationData.rotationRate = rotationRate;
            creationData.spawnPosition = loc;
            creationData.instanceColor = projProps.damageDef.explosionColorCenter;
            map.flecks.CreateFleck(creationData);
        }

        if (Controller.settings.StuckArrowsAsFlecks && StuckProjectileFleck != null)
        {
            FleckCreationData creationData = FleckMaker.GetDataStatic(loc, map, StuckProjectileFleck);
            creationData.scale = ext.StuckProjectileFleckSize;
            creationData.rotation = (-direction) % 360;
            //tilt landed projectile slightly upwards so it looks stuck in ground
            creationData.rotation += creationData.rotation > 180 ? Rand.Range(-45, 0) : Rand.Range(0, 45);
            creationData.rotationRate = 0;
            creationData.spawnPosition = loc;
            map.flecks.CreateFleck(creationData);
        }

        if (ImpactSound != null)
        {
            ImpactSound.PlayOneShot(new TargetInfo(loc.ToIntVec3(), map));
        }
        Rand.PopState();
    }

    private static void TriggerBulletHole(Vector3 loc, Map map, float size)
    {

        FleckCreationData creationData = FleckMaker.GetDataStatic(loc, map, CE_FleckDefOf.Fleck_BulletHole);
        creationData.rotation = Rand.Range(0, 360);
        creationData.scale = size;
        creationData.spawnPosition = loc;
        map.flecks.CreateFleck(creationData);
    }

    private static void TriggerScatteredSparks(Vector3 loc, Map map, int count, float direction)
    {
        FleckCreationData creationData = FleckMaker.GetDataStatic(loc, map, CE_FleckDefOf.Fleck_SparkThrownFast);
        for (int i = 0; i < count; i++)
        {
            creationData.velocityAngle = direction + Rand.Range(-45, 45);
            creationData.scale = 0.2f;
            creationData.velocitySpeed = Rand.Range(5, 10);
            creationData.spawnPosition = loc;
            creationData.airTimeLeft = 0.2f;
            map.flecks.CreateFleck(creationData);
        }
    }

    private static float ScaleToRange(float minNew, float maxNew, float minOld, float maxOld, float value)
    {
        return minNew + (value - minOld) / (maxOld - minOld) * (maxNew - minNew);
    }

    public static void TriggerEffect(EffecterDef effect, Vector3 position, Map map, Thing hitThing = null)
    {
        TriggerEffect(effect, IntVec3.FromVector3(position), map);
    }

    public static void TriggerEffect(EffecterDef effect, IntVec3 dest, Map map)
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
