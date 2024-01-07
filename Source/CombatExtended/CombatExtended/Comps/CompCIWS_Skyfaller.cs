using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class CompCIWS_Skyfaller : CompCIWS
    {
        public new CompProperties_CIWS_Skyfaller Props => props as CompProperties_CIWS_Skyfaller;
        public IEnumerable<Thing> Skyfallers => Comp_InterceptableSkyfaller.InterceptableSkyfallersPerMap(parent.Map);
        public override bool TryFindTarget(IAttackTargetSearcher targetSearcher, out LocalTargetInfo result)
        {
            Thing p;
            if (parent is Building_TurretGunCE turret)
            {
                float range = turret.AttackVerb.verbProps.range;
                Faction faction = targetSearcher.Thing.Faction;
                if (Skyfallers.Where(x => IsHostile(targetSearcher.Thing, x)).Where(delegate (Thing x)
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
            if (intendedTarget.Thing is Skyfaller skyfaller && !intendedTarget.Thing.Destroyed)
            {
                //Log.Message($"{skyfaller.DrawPos}, {ExactPosition}, {vect.sqrMagnitude}, {minCollisionDistance}");
                var y0bounds = CE_Utility.GetBoundsFor(skyfaller);
                y0bounds.center = y0bounds.center.Yto0();
                var y0Ray = new Ray(projectile.LastPos.Yto0(), projectile.ExactPosition.Yto0());
                if (y0bounds.IntersectRay(y0Ray, out var dist) && projectile.ExactMinusLastPos.sqrMagnitude < dist * dist && Rand.Chance(Props.hitChance))
                {
                    skyfaller.GetComp<Comp_InterceptableSkyfaller>().Impact(projectile);
                    return true;
                }
            }
            return false;
        }
        static bool IsHostile(Thing thing, Thing thing2)
        {
            if (thing.HostileTo(thing2))
            {
                return true;
            }
            if (thing2 is IThingHolder thingHolder && thingHolder.GetDirectlyHeldThings().OfType<ActiveDropPod>().SelectMany(x => x.contents.innerContainer).Any(x => x.HostileTo(thing)))
            {
                return true;
            }
            return false;
        }
    }
}
