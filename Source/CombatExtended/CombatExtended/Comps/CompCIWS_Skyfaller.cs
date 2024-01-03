using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class CompCIWS_Skyfaller : ThingComp
    {
        public CompProperties_CIWS_Skyfaller Props => props as CompProperties_CIWS_Skyfaller;
        public IEnumerable<Thing> Skyfallers => Comp_InterceptableSkyfaller.InterceptableSkyfallersPerMap(parent.Map);
        public virtual bool TryFindTarget(IAttackTargetSearcher targetSearcher, out LocalTargetInfo result)
        {
            Thing p;
            if (parent is Building_TurretGunCE turret)
            {
                float range = turret.AttackVerb.verbProps.range;
                Faction faction = targetSearcher.Thing.Faction;
                if (Skyfallers.Where(delegate (Thing x)
                {
                    float num = turret.AttackVerb.verbProps.EffectiveMinRange(x, turret);
                    float num2 = (float)x.Position.DistanceToSquared(turret.Position);
                    return num2 > num * num && num2 < range * range;
                }).TryRandomElement(out p))
                {
                    Log.Message("Found skyfaller");
                    result = p;
                    return true;
                }
            }
            result = LocalTargetInfo.Invalid;
            return false;
        }
    }
}
