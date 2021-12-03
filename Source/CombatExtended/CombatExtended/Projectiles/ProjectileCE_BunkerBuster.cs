using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
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

                int x = this.Position.x;

                int z = this.Position.z;

                if (props.HP_penetration)
                {
                    tiles_penetration *= (int)(hitThing.HitPoints / props.HP_penetration_ratio);

                }

                tiles_penetration = Math.Min(5, tiles_penetration);

                Log.Message(this.shotRotation.ToString().Colorize(UnityEngine.Color.red));

                /*if (this.shotRotation > 0f && this.shotRotation < 90f)
                {
                    this.rotationInt = Rot4.North;
                }*/

                if ((this.shotRotation > 0f && this.shotRotation < 92) | (this.shotRotation > -275f && this.shotRotation < -265f))
                {
                    this.rotationInt = Rot4.West;
                }

                if (this.shotRotation > -90f && this.shotRotation < 45f)
                {
                    this.rotationInt = Rot4.East;
                }

                if (this.shotRotation > -50f && this.shotRotation < 0f)
                {
                    this.rotationInt = Rot4.North;
                }

                Log.Message(this.rotationInt.ToString().Colorize(UnityEngine.Color.red));

                if (this.rotationInt == Rot4.East)
                {
                    Log.Message("east");
                    x += tiles_penetration;
                }

                if (this.rotationInt == Rot4.West)
                {
                    Log.Message("west");
                    x -= tiles_penetration;
                }

                if (this.rotationInt == Rot4.North)
                {
                    Log.Message("north");
                    z -= tiles_penetration;
                }

                if (this.rotationInt == Rot4.South)
                {
                    Log.Message("south");
                    z += tiles_penetration;
                }

                this.Position = new IntVec3(x, 0, z);

                Log.Message(this.Position.ToString());

                GenExplosionCE.DoExplosion(this.Position,
                    this.Map,
                    props.explosionRadius, props.damageDef,
                    this,
                    props.GetDamageAmount(1),
                    (props.GetDamageAmount(1) / 5),
                    damageFalloff: props.explosionDamageFalloff,
                    applyDamageToExplosionCellsNeighbors: props.applyDamageToExplosionCellsNeighbors
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
