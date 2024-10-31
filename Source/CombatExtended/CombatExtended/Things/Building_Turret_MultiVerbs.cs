using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended
{
    public class Building_Turret_MultiVerbs : Building_TurretGunCE
    {
        public override Verb AttackVerb
        {
            get
            {
                if (CurrentTarget.IsValid)
                {
                    foreach (var verb in GunCompEq.AllVerbs)
                    {
                        if (verb.ValidateTarget(CurrentTarget, false))
                        {
                            return verb;
                        }
                    }
                }

                return base.AttackVerb;
            }
        }
        IEnumerable<ITargetSearcher> cachedVerbsWithTargetSearcher;
        protected IEnumerable<ITargetSearcher> VerbsWithTargetSearcher => cachedVerbsWithTargetSearcher ??= GunCompEq.AllVerbs.OfType<ITargetSearcher>();
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            foreach (var verb in GunCompEq.AllVerbs.Except(AttackVerb))
            {
                float range = verb.verbProps.range;
                if (range < 90f)
                {
                    GenDraw.DrawRadiusRing(base.Position, range);
                }
                float num = verb.verbProps.EffectiveMinRange(true);
                if (num < 90f && num > 0.1f)
                {
                    GenDraw.DrawRadiusRing(base.Position, num);
                }
            }
        }
        public override LocalTargetInfo TryFindNewTarget()
        {
            foreach (var verb in VerbsWithTargetSearcher)
            {
                if (verb.TryFindNewTarget(out var target))
                {
                    return target;
                }
            }
            return base.TryFindNewTarget();
        }
    }
}
