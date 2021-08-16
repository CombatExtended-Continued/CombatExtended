using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;
using System.Runtime.Remoting.Messaging;

namespace CombatExtended
{
    public static class GenExplosionCE
    {
        public const float MinExplosionScale = 0.1f;    //as if 1/1000 of the shell exploded
        public const float MaxExplosionScale = 10f;     //as if 1000 shells exploded

        public static void DoExplosion(IntVec3 center, Map map, float radius, DamageDef damType, Thing instigator, int damAmount = -1, float armorPenetration = -1f, SoundDef explosionSound = null, ThingDef weapon = null, ThingDef projectile = null, Thing intendedTarget = null, ThingDef postExplosionSpawnThingDef = null, float postExplosionSpawnChance = 0f, int postExplosionSpawnThingCount = 1, bool applyDamageToExplosionCellsNeighbors = false, ThingDef preExplosionSpawnThingDef = null, float preExplosionSpawnChance = 0f, int preExplosionSpawnThingCount = 1, float chanceToStartFire = 0f, bool damageFalloff = false, float? direction = null, List<Thing> ignoredThings = null,
            float height = 0f, float scaleFactor = 1f, bool destroyAfterwards = false, ThingWithComps explosionParentToDestroy = null)
        {
            // Allows destroyed things to be exploded with appropriate scaleFactor
            if (scaleFactor <= 0f)
                scaleFactor = 1f;
            else
                scaleFactor = Mathf.Clamp(scaleFactor, MinExplosionScale, MaxExplosionScale);

            if (map == null)
            {
                Log.Warning("CombatExtended :: Tried to do explosionCE in a null map.");
                return;
            }
            if (damAmount < 0)
            {
                damAmount = damType.defaultDamage;
                armorPenetration = damType.defaultArmorPenetration;
                if (damAmount < 0)
                {
                    Log.ErrorOnce("CombatExtended :: Attempted to trigger an explosionCE without defined damage", 910948823);
                    damAmount = 1;
                }
            }

            explosionSound = explosionSound ?? damType.soundExplosion;

            if (explosionSound == null)
            {
                Log.Error("CombatExtended :: SoundDef was null for DamageDef " + damType.defName + " as well as instigator " + instigator.ThingID);
            }

            damAmount = Mathf.RoundToInt(damAmount * scaleFactor);
            radius *= scaleFactor;
            armorPenetration *= scaleFactor;

            ExplosionCE explosion = GenSpawn.Spawn(CE_ThingDefOf.ExplosionCE, center, map) as ExplosionCE;
            IntVec3? needLOSToCell = null;
            IntVec3? needLOSToCell2 = null;
            if (direction.HasValue)
            {
                CalculateNeededLOSToCells(center, map, direction.Value, out needLOSToCell, out needLOSToCell2);
            }
            explosion.height = height;
            explosion.radius = radius;
            explosion.damType = damType;
            explosion.instigator = instigator;
            explosion.damAmount = damAmount;
            explosion.armorPenetration = armorPenetration;
            explosion.weapon = weapon;
            explosion.projectile = projectile;
            explosion.intendedTarget = intendedTarget;
            explosion.preExplosionSpawnThingDef = preExplosionSpawnThingDef;
            explosion.preExplosionSpawnChance = preExplosionSpawnChance;
            explosion.preExplosionSpawnThingCount = preExplosionSpawnThingCount;
            explosion.postExplosionSpawnThingDef = postExplosionSpawnThingDef;
            explosion.postExplosionSpawnChance = postExplosionSpawnChance;
            explosion.postExplosionSpawnThingCount = postExplosionSpawnThingCount;
            explosion.applyDamageToExplosionCellsNeighbors = applyDamageToExplosionCellsNeighbors;
            explosion.chanceToStartFire = chanceToStartFire;
            explosion.damageFalloff = damageFalloff;
            explosion.needLOSToCell1 = needLOSToCell;
            explosion.needLOSToCell2 = needLOSToCell2;
            explosion.StartExplosionCE(explosionSound, ignoredThings);

            // Needed to allow CompExplosive to use stackCount
            if (destroyAfterwards && !explosionParentToDestroy.Destroyed)
                explosionParentToDestroy?.Kill();
        }

        //Exact copy (1.1)
        private static void CalculateNeededLOSToCells(IntVec3 position, Map map, float direction, out IntVec3? needLOSToCell1, out IntVec3? needLOSToCell2)
        {
            needLOSToCell1 = null;
            needLOSToCell2 = null;
            if (position.CanBeSeenOverFast(map))
            {
                return;
            }
            direction = GenMath.PositiveMod(direction, 360f);
            IntVec3 intVec = position;
            intVec.z++;
            IntVec3 intVec2 = position;
            intVec2.z--;
            IntVec3 intVec3 = position;
            intVec3.x--;
            IntVec3 intVec4 = position;
            intVec4.x++;
            if (direction < 90f)
            {
                if (intVec3.InBounds(map) && intVec3.CanBeSeenOverFast(map))
                {
                    needLOSToCell1 = new IntVec3?(intVec3);
                }
                if (intVec.InBounds(map) && intVec.CanBeSeenOverFast(map))
                {
                    needLOSToCell2 = new IntVec3?(intVec);
                    return;
                }
            }
            else if (direction < 180f)
            {
                if (intVec.InBounds(map) && intVec.CanBeSeenOverFast(map))
                {
                    needLOSToCell1 = new IntVec3?(intVec);
                }
                if (intVec4.InBounds(map) && intVec4.CanBeSeenOverFast(map))
                {
                    needLOSToCell2 = new IntVec3?(intVec4);
                    return;
                }
            }
            else if (direction < 270f)
            {
                if (intVec4.InBounds(map) && intVec4.CanBeSeenOverFast(map))
                {
                    needLOSToCell1 = new IntVec3?(intVec4);
                }
                if (intVec2.InBounds(map) && intVec2.CanBeSeenOverFast(map))
                {
                    needLOSToCell2 = new IntVec3?(intVec2);
                    return;
                }
            }
            else
            {
                if (intVec2.InBounds(map) && intVec2.CanBeSeenOverFast(map))
                {
                    needLOSToCell1 = new IntVec3?(intVec2);
                }
                if (intVec3.InBounds(map) && intVec3.CanBeSeenOverFast(map))
                {
                    needLOSToCell2 = new IntVec3?(intVec3);
                }
            }
        }
        public static float GetExplosionAP(ProjectileProperties props)
        {
            return props.GetDamageAmount(1) * 0.1f;
        }
    }
}
