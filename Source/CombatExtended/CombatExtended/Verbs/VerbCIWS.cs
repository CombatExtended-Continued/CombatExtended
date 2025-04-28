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
        protected ProjectileCE dummyCIWSProjectile;

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
        protected (Vector3 firstPos, Vector3 secondPos) PositionOfCIWSProjectile(int sinceTicks, Vector3 targetPos, bool drawPos = false)
        {
            AimDummyCIWSProjectileTo(targetPos);
            var firstPos = Caster.Position.ToVector3Shifted();
            var secondPos = firstPos;
            var enumeration = TrajectoryWorker.NextPositions(dummyCIWSProjectile).GetEnumerator();
            for (int i = 1; i <= sinceTicks; i++)
            {
                firstPos = secondPos;

                if (!enumeration.MoveNext())
                {
                    break;
                }
                secondPos = enumeration.Current;

            }
            if (drawPos)
            {
                firstPos = TrajectoryWorker.ExactPosToDrawPos(firstPos, sinceTicks - 1, projectilePropsCE.TickToTruePos, Projectile.Altitude);
                secondPos = TrajectoryWorker.ExactPosToDrawPos(secondPos, sinceTicks, projectilePropsCE.TickToTruePos, Projectile.Altitude);
            }
            return (firstPos, secondPos);
        }
        protected void AimDummyCIWSProjectileTo(Vector3 to)
        {
            if (dummyCIWSProjectile == null || dummyCIWSProjectile.def != Projectile)
            {
                dummyCIWSProjectile = SpawnProjectile(); // Not sure if we should call PostMake and etc for dummy
            }
            var originV3 = Caster.Position.ToVector3Shifted();
            var originV2 = new Vector2(originV3.x, originV3.z);
            dummyCIWSProjectile.Launch(Caster, originV2, ShotAngle(to), ShotRotation(to), ShotHeight, ShotSpeed, Caster);

        }
        public override ThingDef Projectile
        {
            get
            {
                var result = base.Projectile;
                var ciwsVersion = (result?.projectile as ProjectilePropertiesCE)?.CIWSVersion;
                if (ciwsVersion == null && !typeof(ProjectileCE_CIWS).IsAssignableFrom(result.thingClass))
                {
                    Log.WarningOnce($"{result} is not a CIWS projectile and the projectile does not have the CIWS version specified in its properties. Must be on-ground projectile used for CIWS", result.GetHashCode());
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
                thing.forcedTrajectoryWorker = TrajectoryWorker;
                thing.def = def;
                thing.PostMake();
                thing.PostPostMake();
                return thing;
            }
            return base.SpawnProjectile();
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
        protected abstract IEnumerable<Vector3> TargetNextPositions(TargetType target);
        protected virtual Bounds ConvertToBounds(TargetType target, Vector3 pos) => new Bounds(pos, new Vector3(1f, 1f, 1f));
        protected virtual bool IgnoreHeightCheck => true;



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
                float distToSqr = (intersectionPoint - Caster.Position).LengthHorizontalSquared;
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
            if (TrajectoryWorker.GuidedProjectile)
            {
                if ((originV3 - target.DrawPos).MagnitudeHorizontalSquared() > maxDistSqr)
                {
                    resultingLine = default;
                    targetPos = default;
                    return false;
                }
                var y = TargetNextPositions(target).FirstOrDefault().y;
                targetPos = target.DrawPos;
                resultingLine = new ShootLine(Shooter.Position, new IntVec3((int)targetPos.x, (int)y, (int)targetPos.z));
                return true;
            }
            var midBurst = MidBurst;
            var ticksToSkip = (Caster as Building_TurretGunCE)?.CurrentTarget.IsValid ?? CurrentTarget.IsValid ? this.BurstWarmupTicksLeft : VerbPropsCE.warmupTime.SecondsToTicks();
            var instant = projectilePropsCE.isInstant;
            if (instant)
            {
                var to = TargetNextPositions(target).Skip(ticksToSkip).FirstOrFallback(Vector3.negativeInfinity);
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
            foreach (var nextPos in TargetNextPositions(target).Skip(ticksToSkip))
            {
                var pos = nextPos;
                if ((pos - originV3).MagnitudeHorizontalSquared() > maxDistSqr)
                {
                    i++;
                    continue;
                }

                var positions = PositionOfCIWSProjectile(i, pos, true);
                //if (positions.firstPos == positions.secondPos) //Not sure why, but sometimes this code drops calculations on i = 1
                //{
                //    resultingLine = default(ShootLine);
                //    return false;
                //}
                Vector3 ciwsPos1 = positions.firstPos, ciwsPos2 = positions.secondPos;
                if (IgnoreHeightCheck)
                {
                    ciwsPos1.y = 0;
                    ciwsPos2.y = 0;
                    pos.y = 0f;
                }
                var targetBounds = ConvertToBounds(target, pos);
                var ray = new Ray(ciwsPos1, ciwsPos2 - ciwsPos1);
                if (targetBounds.IntersectRay(ray, out var dist) && dist * dist < (ciwsPos1 - ciwsPos2).sqrMagnitude)
                {
                    targetPos = nextPos;
                    resultingLine = new ShootLine(Shooter.Position, new IntVec3(Mathf.FloorToInt(nextPos.x), Mathf.FloorToInt(nextPos.y), Mathf.FloorToInt(nextPos.z)));
                    return true;
                }
                i++;
                if (i > maximumPredectionTicks)
                {
                    break;
                }
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
        protected override IEnumerable<Vector3> TargetNextPositions(Thing target)
        {
            return target.TryGetComp<CompCIWSTarget>().NextPositions;
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
