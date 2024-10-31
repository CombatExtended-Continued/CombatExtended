using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using Verse;
using Verse.Sound;

namespace CombatExtended
{
    public abstract class VerbCIWS : Verb_ShootCE_CIWS, ITargetSearcher
    {
        
        protected bool holdFire;
        
        public VerbProperties_CIWS Props => verbProps as VerbProperties_CIWS;
        protected abstract string HoldLabel { get; }
        protected abstract string HoldDesc { get; }
        protected virtual string HoldIcon => "UI/Commands/HoldFire";

        //public override IEnumerable<Gizmo> CompGetGizmosExtra()
        //{
        //    foreach (var gizmo in base.CompGetGizmosExtra())
        //    {
        //        yield return gizmo;
        //    }
        //    if (Turret.CanToggleHoldFire)
        //    {
        //        yield return new Command_Toggle
        //        {
        //            defaultLabel = HoldLabel.Translate(),
        //            defaultDesc = HoldDesc.Translate(),
        //            icon = ContentFinder<Texture2D>.Get(HoldIcon, true),
        //            hotKey = KeyBindingDefOf.Misc6,
        //            toggleAction = delegate ()
        //            {
        //                this.holdFire = !this.holdFire;
        //                if (this.holdFire && HasTarget)
        //                {
        //                    Turret.ResetForcedTarget();
        //                }
        //            },
        //            isActive = (() => this.holdFire)
        //        };
        //    }
        //}
        public virtual bool Active => !holdFire && Turret.Active;
        public abstract bool TryFindNewTarget(out LocalTargetInfo target);
    }
    public abstract class VerbCIWS<TargetType> : VerbCIWS where TargetType : Thing
    {

        public override bool TryFindNewTarget(out LocalTargetInfo target)
        {
            float range = this.verbProps.range;
            var _target = Targets.Where(x => Props.Interceptable(x.def) && !Turret.IgnoredDefs.Contains(x.def)).Where(x => !IsFriendlyTo(x)).FirstOrDefault(t =>
            {
                var verb = this;
                if (Caster.Map.GetComponent<TurretTracker>().CIWS.Any(turret => turret.currentTargetInt.Thing == t) || ProjectileCE_CIWS.ProjectilesAt(Caster.Map).Any(x => x.intendedTarget.Thing == t))
                {
                    return false;
                }
                float num = verb.verbProps.EffectiveMinRange(t, this.Caster);
                if (!verb.TryFindCEShootLineFromTo(Caster.Position, t, out var shootLine))
                {
                    return false;
                }
                var intersectionPoint = shootLine.Dest;
                float num2 = intersectionPoint.DistanceToSquared(Caster.Position);
                return num2 > num * num && num2 < range * range;
            });
            if (_target != null)
            {
                target = _target;
                return true;
            }
            target = null;
            return false;
        }
        protected virtual bool IsFriendlyTo(TargetType thing) => ((!thing.TryGetComp<CompCIWSTarget>()?.Props.alwaysIntercept) ?? false) && !thing.HostileTo(Caster);
        public abstract IEnumerable<TargetType> Targets { get; }
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true) => target.Thing is TargetType && base.ValidateTarget(target, showMessages);
    }

    public abstract class VerbProperties_CIWS : VerbPropertiesCE
    {
        public List<ThingDef> ignored = new List<ThingDef>();
        public IEnumerable<ThingDef> Ignored => ignored;
        public virtual bool Interceptable(ThingDef targetDef) => true;

        private IEnumerable<ThingDef> allTargets;

        public IEnumerable<ThingDef> AllTargets => allTargets ??= InitAllTargets();
        protected abstract IEnumerable<ThingDef> InitAllTargets();
    }
}
