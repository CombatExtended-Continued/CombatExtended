using RimWorld;
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

        Verb activeVerb;
        IEnumerable<ITargetSearcher> cachedVerbsWithTargetSearcher;

        public override Verb AttackVerb
        {
            get
            {
                return activeVerb ?? GunCompEq.AllVerbs.FirstOrDefault(x => x.state == VerbState.Bursting) ?? base.AttackVerb;
            }
        }
        protected IEnumerable<ITargetSearcher> VerbsWithTargetSearcher => cachedVerbsWithTargetSearcher ??= GunCompEq.AllVerbs.OfType<ITargetSearcher>().ToList();
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            IEnumerable<Verb> verbs = (Controller.settings.EnableCIWS ? GunCompEq.AllVerbs : GunCompEq.AllVerbs.Where(x => !(x is VerbCIWS))).Except(AttackVerb);
            foreach (var verb in verbs)
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
            foreach (var targetSearcher in VerbsWithTargetSearcher)
            {
                var verb = (Verb)targetSearcher;
                if (verb.Available() && targetSearcher.TryFindNewTarget(out var target))
                {
                    activeVerb = (Verb)targetSearcher;
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
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (PlayerControlled)
            {
                var disablerComp = Gun.TryGetComp<CompVerbDisabler>();
                if (disablerComp != null)
                {
                    foreach (var gizmo in disablerComp.CompGetGizmosExtra())
                    {
                        yield return gizmo;
                    }
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref activeVerb, nameof(activeVerb));
        }
    }
}
