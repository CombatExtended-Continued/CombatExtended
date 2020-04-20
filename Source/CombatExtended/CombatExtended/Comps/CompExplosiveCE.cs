using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    /// <summary>
    /// Empty class for backwards compatibility
    /// </summary>
    public class CompExplosiveCE : ThingComp
    {
        CompProperties_ExplosiveCE Props => props as CompProperties_ExplosiveCE;

        public virtual void Explode(Thing instigator, Vector3 pos, Map map, float scaleFactor = 1, float? direction = null, List<Thing> ignoredThings = null)
        {
            var posIV = pos.ToIntVec3();
            if (map == null)
            {
                Log.Warning("Tried to do explodeCE in a null map.");
                return;
            }
            if (!posIV.InBounds(map))
            {
                Log.Warning("Tried to explodeCE out of bounds");
                return;
            }
            
            //Try to throw fragments -- increase count by scaleFactor
            parent.TryGetComp<CompFragments>()?.Throw(pos, map, instigator);//scaleFactor);

            if (Props.explosiveRadius > 0 //&& Props.damageAmountBase > 0 Disabled to allow flame explosions etc
                && parent.def != null)
            {
                //Call GenExplosionCE for main explosion
                GenExplosionCE.DoExplosion(posIV, map, Props.explosiveRadius, Props.explosiveDamageType, instigator,
                    GenMath.RoundRandom(Props.damageAmountBase), Props.damageAmountBase * 0.1f,
                    Props.explosionSound, null, parent.def, null,
                    Props.postExplosionSpawnThingDef, Props.postExplosionSpawnChance, Props.postExplosionSpawnThingCount,
                    Props.applyDamageToExplosionCellsNeighbors, Props.preExplosionSpawnThingDef, Props.preExplosionSpawnChance,
                    Props.preExplosionSpawnThingCount, Props.chanceToStartFire, Props.damageFalloff, direction, ignoredThings,
                    pos.y, scaleFactor);
            }
        }
    }
}