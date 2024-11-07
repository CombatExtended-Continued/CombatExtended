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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref activeVerb, nameof(activeVerb));
        }

        Verb activeVerb;
        public override Verb AttackVerb
        {
            get
            {
                return activeVerb ?? GunCompEq.AllVerbs.FirstOrDefault(x=>x.state == VerbState.Bursting) ?? base.AttackVerb;
            }
        }
        IEnumerable<ITargetSearcher> cachedVerbsWithTargetSearcher;
        protected IEnumerable<ITargetSearcher> VerbsWithTargetSearcher => cachedVerbsWithTargetSearcher ??= GunCompEq.AllVerbs.OfType<ITargetSearcher>().ToList();
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            foreach (var verb in GunCompEq.AllVerbs.Except(AttackVerb))
            {
                float range = verb.verbProps.range;
                if (range < 120f)
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
            activeVerb = null;
            foreach (var verb in VerbsWithTargetSearcher)
            {
                if (verb.TryFindNewTarget(out var target))
                {
                    activeVerb = (Verb)verb;
                    return target;
                }
            }
            return base.TryFindNewTarget();
        }

        public override void ResetCurrentTarget()
        {
            base.ResetCurrentTarget();
            activeVerb = null;
        }
        public override void ResetForcedTarget()
        {
            base.ResetForcedTarget();
            activeVerb = null;
        }
    }
}
