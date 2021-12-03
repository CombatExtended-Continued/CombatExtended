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

                int tiles_penetration = props.fuze_delay;

                tiles_penetration /= (hitThing.HitPoints / 300);

                Log.Message(tiles_penetration.ToString().Colorize(Color.red));

                Log.Message(this.origin.y.ToString());

                Log.Message(this.origin.x.ToString());

                Vector3 direction = hitThing.Position.ToVector3() - (new Vector3(this.origin.x, 0, this.origin.y));
                IntVec3 finalPos = ((new Vector3(hitThing.Position.x, 0, hitThing.Position.z) + (direction.normalized * tiles_penetration))).ToIntVec3();

                Log.Message(direction.ToString());

                Log.Message(finalPos.ToString());

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

                this.Destroy();

            }
            else
            {
                base.Impact(hitThing);
            }


        }
    }
}
