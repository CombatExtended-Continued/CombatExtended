using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class CompCIWS_Projectile : CompCIWS
    {
        public new CompProperties_CIWS_Projectile Props => props as CompProperties_CIWS_Projectile;

        public override bool TryFindTarget(IAttackTargetSearcher targetSearcher, out LocalTargetInfo result)
        {
            Thing p;
            if (parent is Building_TurretGunCE turret)
            {
                float range = turret.AttackVerb.verbProps.range;
                Faction faction = targetSearcher.Thing.Faction;
                if (turret.Map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile).Where(x => IsHostile(targetSearcher.Thing, x)).Where(delegate (Thing x)
                  {
                      float num = turret.AttackVerb.verbProps.EffectiveMinRange(x, turret);
                      float num2 = (float)x.Position.DistanceToSquared(turret.Position);
                      return num2 > num * num && num2 < range * range;
                  }).TryRandomElement(out p))
                {
                    result = p;
                    return true;
                }
            }
            result = LocalTargetInfo.Invalid;
            return false;
        }

        public override bool CheckImpact(ProjectileCE projectile)
        {
            var intendedTarget = projectile.intendedTarget;
            if ((intendedTarget.Thing is Projectile || intendedTarget.Thing is ProjectileCE) && !intendedTarget.Thing.Destroyed)
            {
                var exactPosition = projectile.ExactPosition.Yto0();
                var targetPos = intendedTarget.Thing is ProjectileCE projectileCE ? projectileCE.ExactPosition.Yto0() : intendedTarget.Thing.Position.ToVector3Shifted();
                var minCollisionDistance = projectile.minCollisionDistance;
                var vect = targetPos - exactPosition;
                if (vect.sqrMagnitude < minCollisionDistance && Rand.Chance(Props.hitChance))
                {
                    projectile.Impact(null);
                    intendedTarget.Thing.Destroy();
                    return true;
                }
            }
            return false;
        }
        static bool IsHostile(Thing thing, Thing thing1)
        {
            return thing.HostileTo(thing1) || (thing1 is ProjectileCE projectile && projectile.launcher.HostileTo(thing));
        }
    }
}
