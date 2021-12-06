using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;
using RimWorld;

namespace CombatExtended
{
    public class ProjectileCE_BunkerBuster : ProjectileCE_Explosive
    {
        public override void Impact(Thing hitThing)
        {
            

            if (hitThing is Building)
            {
                var props = (ProjectilePropertiesCE)this.def.projectile;

                float tiles_penetration = props.fuze_delay;

                tiles_penetration /= (hitThing.HitPoints / 300f);

                Vector3 direction = hitThing.Position.ToVector3() - (new Vector3(this.origin.x, 0, this.origin.y));
                IntVec3 finalPos = ((new Vector3(hitThing.Position.x, 0, hitThing.Position.z) + (direction.normalized * Math.Max(tiles_penetration, 1)))).ToIntVec3();

                GenExplosionCE.DoExplosion(finalPos,
                    this.Map,
                    props.explosionRadius, props.damageDef,
                    this,
                    props.GetDamageAmount(1),
                    (props.GetDamageAmount(1) / 5),
                    damageFalloff: props.explosionDamageFalloff,
                    applyDamageToExplosionCellsNeighbors: props.applyDamageToExplosionCellsNeighbors,
                    preExplosionSpawnThingDef: props.preExplosionSpawnThingDef,
                    preExplosionSpawnChance: props.preExplosionSpawnChance,
                    preExplosionSpawnThingCount: props.preExplosionSpawnThingCount
                    );

                if (this.TryGetComp<CompFragments>() != null)
                {
                    this.TryGetComp<CompFragments>().Throw(finalPos.ToVector3(), this.Map, this);
                }

                this.Destroy();

            }
            else
            {
                base.Impact(hitThing);
            }


        }
    }
}
