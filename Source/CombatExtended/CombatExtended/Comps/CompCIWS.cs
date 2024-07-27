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
    public abstract class CompCIWS : ThingComp
    {
        #region Caching
        protected static Dictionary<Map, List<Building_TurretGun>> CIWS = new Dictionary<Map, List<Building_TurretGun>>();

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (parent.Map == null)
            {
                return;
            }
            if (!CIWS.TryGetValue(parent.Map, out var targetList))
            {
                CIWS[parent.Map] = targetList = new List<Building_TurretGun>();
            }
            targetList.Add(Turret);

            //IgnoredDefs ??= Props.Ignored.Union(Mod.Settings?.settings?.FirstOrDefault(x => x.ciws == parent.def)?.IgnoredDefs ?? Enumerable.Empty<ThingDef>()).ToList();
        }
        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (CIWS.TryGetValue(map, out var targetList))
            {
                targetList.Remove(Turret);
            }
        }
        #endregion
        protected bool holdFire;
        public IEnumerable<ThingDef> IgnoredDefs { get; private set; }
        public Building_TurretGun Turret => parent as Building_TurretGun;
        public CompProperties_CIWS Props => props as CompProperties_CIWS;
        public virtual Verb Verb => Turret.GunCompEq.PrimaryVerb;
        public abstract bool HasTarget { get; }
        protected abstract string HoldLabel { get; }
        protected abstract string HoldDesc { get; }
        protected virtual string HoldIcon => "UI/Commands/HoldFire";
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref holdFire, nameof(holdFire), false);
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (Turret.CanToggleHoldFire)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = HoldLabel.Translate(),
                    defaultDesc = HoldDesc.Translate(),
                    icon = ContentFinder<Texture2D>.Get(HoldIcon, true),
                    hotKey = KeyBindingDefOf.Misc6,
                    toggleAction = delegate ()
                    {
                        this.holdFire = !this.holdFire;
                        if (this.holdFire && HasTarget)
                        {
                            Turret.ResetForcedTarget();
                        }
                    },
                    isActive = (() => this.holdFire)
                };
            }
        }
        public virtual bool Active => !holdFire && Turret.Active;
    }
    public abstract class CompCIWS<TargetType> : CompCIWS where TargetType : Thing
    {

        public override void CompTick()
        {
            base.CompTick();

            if (parent.IsHashIntervalTick(10) && Active && Verb.state != VerbState.Bursting && Turret.burstCooldownTicksLeft <= 0 && TryFindNewTarget(out var target))
            {
                Turret.currentTargetInt = target;
                Turret.BeginBurst();
                Turret.Top.TurretTopTick();
            }
        }
        public virtual bool TryFindNewTarget(out LocalTargetInfo target)
        {
            float range = this.Verb.verbProps.range;
            var _target = Targets.Where(x => Props.Interceptable(x.def) && !IgnoredDefs.Contains(x.def)).Where(x => !IsFriendlyTo(x)).FirstOrDefault(t =>
            {
                if (CIWS[parent.Map].Any(turret => turret.currentTargetInt.Thing == t) || CIWSProjectile.ProjectilesAt(parent.Map).Any(x => x.intendedTarget.Thing == t))
                {
                    return false;
                }
                float num = this.Verb.verbProps.EffectiveMinRange(t, this.parent);
                var intersectionPoint = t.TryGetComp<CompCIWSTarget>()?.CalculatePointForPreemptiveFire(Verb.GetProjectile(), parent.DrawPos, (int)Verb.verbProps.warmupTime) ?? t.CalculatePointForPreemptiveFire(Verb.GetProjectile(), parent.DrawPos, (int)Verb.verbProps.warmupTime);
                if (!intersectionPoint.IsValid)
                {
                    return false;
                }
                float num2 = intersectionPoint.DistanceToSquared(this.parent.Position);
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
        protected virtual bool IsFriendlyTo(TargetType thing) => ((!thing.TryGetComp<CompCIWSTarget>()?.Props.alwaysIntercept) ?? false) && !thing.HostileTo(parent);
        public abstract IEnumerable<TargetType> Targets { get; }
    }

    public abstract class CompProperties_CIWS : CompProperties
    {
        public List<ThingDef> ignored = new List<ThingDef>();
        public IEnumerable<ThingDef> Ignored => ignored;
        public virtual bool Interceptable(ThingDef targetDef) => true;

        private IEnumerable<ThingDef> allTargets;

        public IEnumerable<ThingDef> AllTargets => allTargets ??= InitAllTargets();
        protected abstract IEnumerable<ThingDef> InitAllTargets();
    }
}
