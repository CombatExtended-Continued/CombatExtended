using ProjectRimFactory.AutoMachineTool;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using Verse;
using Verse.Sound;

namespace CombatExtended
{
    public abstract class VerbCIWS : Verb_ShootCE, ITargetSearcher, IVerbDisableable
    {
        protected bool debug;
        protected Texture2D icon;
        protected int maximumPredectionTicks = 40;

        public virtual bool HoldFire { get; set; }

        public VerbProperties_CIWS Props => (VerbProperties_CIWS)verbProps;
        public virtual string HoldFireLabel => Props.holdFireLabel;
        public virtual string HoldFireDesc => Props.holdFireDesc;
        public Building_CIWS_CE Turret => Caster as Building_CIWS_CE;

        public virtual Texture2D HoldFireIcon
        {
            get
            {
                if (icon == null)
                {
                    icon = ContentFinder<Texture2D>.Get(Props.holdFireIcon);
                }
                return icon;
            }
        }
        protected override bool ShouldAim => false;
        public virtual bool Active => Controller.settings.EnableCIWS && !HoldFire && Turret.Active;
        protected override bool LockRotationAndAngle => false;
        public abstract bool TryFindNewTarget(out LocalTargetInfo target);
        public virtual void ShowTrajectories()
        {
            if (lastShootLine != null)
            {
                Caster.Map.debugDrawer.FlashLine(lastShootLine.Value.source, lastShootLine.Value.Dest, 60, SimpleColor.Green);
            }
        }


        public override ThingDef Projectile
        {
            get
            {
                var result = base.Projectile;
                var ciwsVersion = (result?.projectile as ProjectilePropertiesCE)?.CIWSVersion;
                if (ciwsVersion == null && !typeof(ProjectileCE_CIWS).IsAssignableFrom(result.thingClass))
                {
                    if (debug)
                    {
                        Log.WarningOnce($"{result} is not a CIWS projectile and the projectile does not have the CIWS version specified in its properties. Must be on-ground projectile used for CIWS", result.GetHashCode());
                    }
                }
                return ciwsVersion ?? result;
            }
        }

        public override bool TryCastShot()
        {
            var result = base.TryCastShot();
            if (result && debug)
            {
                ShowTrajectories();
            }
            return result;
        }
        protected override bool KeepBurstOnNoShootLine(bool suppressing, out ShootLine shootLine)
        {
            shootLine = lastShootLine.HasValue ? lastShootLine.Value : default;
            return !currentTarget.ThingDestroyed;
        }
        public override bool Available()
        {
            return Active && base.Available();
        }
        protected override ProjectileCE SpawnProjectile()
        {
            if (!typeof(ProjectileCE_CIWS).IsAssignableFrom(Projectile.thingClass))
            {
                var def = Projectile;
                var thing = new ProjectileCE_CIWS();
                thing.def = def;
                thing.isCIWS = true;
                thing.PostMake();
                thing.PostPostMake();
                return thing;
            }
            var projectile = base.SpawnProjectile();
            projectile.isCIWS = true;
            return projectile;
        }
        static BaseTrajectoryWorker lerpedTrajectoryWorker = new LerpedTrajectoryWorker_ExactPosDrawing();
        protected BaseTrajectoryWorker TrajectoryWorker
        {
            get
            {
                if (!typeof(ProjectileCE_CIWS).IsAssignableFrom(Projectile.thingClass) && projectilePropsCE.TrajectoryWorker.GetType() == typeof(LerpedTrajectoryWorker))
                {
                    return lerpedTrajectoryWorker;
                }
                return projectilePropsCE.TrajectoryWorker;
            }
        }
    }
    public abstract class VerbCIWS<TargetType> : VerbCIWS where TargetType : Thing
    {
        public abstract IEnumerable<TargetType> Targets { get; }
        protected abstract IEnumerable<Vector3> PredictPositions(TargetType target, int maxTicks);




        public override bool TryFindNewTarget(out LocalTargetInfo target)
        {
            if (!Active)
            {
                target = LocalTargetInfo.Invalid;
                return false;
            }
            float range = this.verbProps.range;
            var _target = Targets.Where(x => Props.Interceptable(x.def) && !Props.Ignored.Contains(x.def) && !Turret.IgnoredDefsSettings.Contains(x.def)).Where(x => !IsFriendlyTo(x)).FirstOrDefault(t =>
            {
                var verb = this;
                if (Caster.Map.GetComponent<TurretTracker>().CIWS.Any(turret => turret.currentTargetInt.Thing == t) || ProjectileCE_CIWS.ProjectilesAt(Caster.Map).Any(x => x.intendedTarget.Thing == t))
                {
                    return false;
                }
                float minRange = verb.verbProps.EffectiveMinRange(t, this.Caster);
                if (!verb.TryFindCEShootLineFromTo(Caster.Position, t, out var shootLine, out var targetPos))
                {
                    return false;
                }
                var intersectionPoint = shootLine.Dest;
                float distToSqr = intersectionPoint.DistanceToSquared(Caster.Position);
                return distToSqr > minRange * minRange && distToSqr < range * range;
            });
            if (_target != null)
            {
                target = _target;
                return true;
            }
            target = null;
            return false;
        }
        protected virtual bool IsFriendlyTo(TargetType thing) => !thing.HostileTo(Caster);
        //public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true) => target.Thing is TargetType && TryFindCEShootLineFromTo(Caster.Position, target, out _, out _) && base.ValidateTarget(target, showMessages);
        public override bool TryFindCEShootLineFromTo(IntVec3 root, LocalTargetInfo targetInfo, out ShootLine resultingLine, out Vector3 targetPos)
        {

            if (!(targetInfo.Thing is TargetType target))
            {
                resultingLine = default;
                targetPos = default;
                return false;
            }
            var maxDistSqr = Props.range * Props.range;
            var originV3 = Caster.Position.ToVector3Shifted();
            int maxTicks = (int)(this.verbProps.range / ShotSpeed) + 5;
            var tworker = TrajectoryWorker(originalTarget);
            if (tworker.GuidedProjectile)
            {
                if ((originV3 - target.DrawPos).MagnitudeHorizontalSquared() > maxDistSqr)
                {
                    resultingLine = default;
                    targetPos = default;
                    return false;
                }
                var y = PredictPositions(target, 1).FirstOrDefault().y;
                targetPos = target.DrawPos;
                resultingLine = new ShootLine(Shooter.Position, new IntVec3((int)targetPos.x, (int)y, (int)targetPos.z));
                return true;
            }
            var midBurst = MidBurst;
            var ticksToSkip = (Caster as Building_TurretGunCE)?.CurrentTarget.IsValid ?? CurrentTarget.IsValid ? this.BurstWarmupTicksLeft : VerbPropsCE.warmupTime.SecondsToTicks();
            var instant = projectilePropsCE.isInstant;
            if (instant)
            {
                var to = PredictPositions(target, ticksToSkip + 1).Skip(ticksToSkip).FirstOrFallback(Vector3.negativeInfinity);
                if (to == Vector3.negativeInfinity)
                {
                    resultingLine = default;
                    targetPos = default;
                    return false;
                }
                targetPos = to;
                resultingLine = new ShootLine(originV3.ToIntVec3(), to.ToIntVec3());
                return true;
            }
            int i = 1;
            var speed = ShotSpeed;
            var targetPos1 = new Vector2(target.DrawPos.x, target.DrawPos.z);
            var source = new Vector3(originV3.x, ShotHeight, originV3.z);
            foreach (var pos in PredictPositions(target, ticksToSkip + maxTicks).Skip(ticksToSkip))
            {
                var targetPos2 = new Vector2(pos.x, pos.z);
                var dhs = (pos - originV3).MagnitudeHorizontalSquared();
                // Check if the projected location is outside our maximum targeting range.
                if (dhs > maxDistSqr)
                {
                    targetPos1 = targetPos2;
                    i++;
                    continue;
                }
                /* Target will be in range, check if we can intercept it.
                 * For each potential target position, we need to see if the number of ticks for us to shoot that position is
                 * equal to the number of ticks before the target is there. So calculate the shot angle to hit the cell,
                 * then calculate how many ticks to reach it.
                 */
                var distance = Mathf.Sqrt(dhs);
                var heightOffset = pos.y - ShotHeight;
                var gravity = projectilePropsCE.Gravity;
                var shotAngle = tworker.ShotAngle(Projectile.projectile as ProjectilePropertiesCE, source, pos, speed);
                var v_xz = speed * Mathf.Sin(shotAngle);
                var d = v_xz * v_xz - 2 * gravity * heightOffset;
                if (d < 0) // cannot actually reach the given location, probably too high up
                {
                    targetPos1 = targetPos2;
                    i++;
                    continue;
                }
                var t = (v_xz + Mathf.Sqrt(d)) / gravity;
                if (Mathf.Abs(t * speed * Mathf.Cos(shotAngle) - distance) > 0.01f) // Didn't reach there on the way up, must be after the zenith
                {
                    t = (v_xz - Mathf.Sqrt(d)) / gravity;
                    if (Mathf.Abs(t * speed * Mathf.Cos(shotAngle) - distance) > 0.01f) // Didn't reach there on the way down either, it's probably landed, or otherwise invalid
                    {
                        i++;
                        continue;
                    }
                }
                int ticksToIntercept = Mathf.CeilToInt(t);
                if (ticksToIntercept > i)
                {
                    if (debug)
                    {
                        Log.Message($"Can hit target, but not at the right time, checking next position");
                    }
                    i++;
                    continue;
                }
                if (ticksToIntercept < i)
                {
                    if (debug)
                    {
                        Log.Message($"Can hit target, but not yet. Need to delay {i - ticksToIntercept} ticks;");
                    }
                    break;
                }
                if (debug)
                {
                    Log.Message("Found shot line at the right delay");
                }
                resultingLine = new ShootLine(Shooter.Position, new IntVec3((int)pos.x, (int)pos.y, (int)pos.z));
                targetPos = pos;
                return true;
            }
            resultingLine = default;
            targetPos = default;
            return false;
        }
        public override float GetTargetHeight(LocalTargetInfo target, Thing cover, bool roofed, Vector3 targetLoc)
        {
            if (target.Thing is TargetType targ)
            {
                return targetLoc.y;
            }
            return base.GetTargetHeight(target, cover, roofed, targetLoc);
        }
    }
    public abstract class VerbCIWS_Comp<TargetType> : VerbCIWS<Thing> where TargetType : CompCIWSTarget
    {
        public override IEnumerable<Thing> Targets => CompCIWSTarget.Targets<TargetType>(Caster.Map);
        protected override bool IsFriendlyTo(Thing thing) => thing.TryGetComp<TargetType>()?.IsFriendlyTo(thing) ?? base.IsFriendlyTo(thing);
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true) => target.HasThing && target.Thing.HasComp<TargetType>() && base.ValidateTarget(target, showMessages);
        protected override IEnumerable<Vector3> PredictPositions(Thing target, int maxTicks)
        {
            return target.TryGetComp<CompCIWSTarget>().PredictedPositions;
        }
    }

    public abstract class VerbProperties_CIWS : VerbPropertiesCE
    {
        public string holdFireIcon = "UI/Commands/HoldFire";
        public string holdFireLabel = "HoldFire";
        public string holdFireDesc;
        public List<ThingDef> ignored = new List<ThingDef>();
        public IEnumerable<ThingDef> Ignored => ignored;
        public virtual bool Interceptable(ThingDef targetDef) => true;

        private IEnumerable<ThingDef> allTargets;

        public IEnumerable<ThingDef> AllTargets => allTargets ??= InitAllTargets();
        protected abstract IEnumerable<ThingDef> InitAllTargets();
    }
}
