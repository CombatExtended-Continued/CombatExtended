using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static UnityEngine.UI.Image;

namespace CombatExtended
{
    public class VerbCIWSProjectile : VerbCIWS<ProjectileCE>
    {
        protected override string HoldDesc => "HoldCloseInProjectilesFireDesc";
        protected override string HoldLabel => "HoldCloseInProjectilesFire";
        public new VerbProperties_CIWSProjectile Props => verbProps as VerbProperties_CIWSProjectile;

        public override IEnumerable<ProjectileCE> Targets => Caster.Map?.listerThings.ThingsInGroup(ThingRequestGroup.Projectile).OfType<ProjectileCE>() ?? Array.Empty<ProjectileCE>();

        protected override bool IsFriendlyTo(ProjectileCE thing) => base.IsFriendlyTo(thing) && !thing.launcher.HostileTo(Caster);
        public override bool TryFindCEShootLineFromTo(IntVec3 root, LocalTargetInfo target, out ShootLine resultingLine)
        {
            if (base.TryFindCEShootLineFromTo(root, target, out resultingLine))
            {
                return true;
            }
            if (!(target.Thing is ProjectileCE targetProjectile))
            {
                return false;
            }
            var midBurst = numShotsFired > 0;
            var originV3 = Caster.Position.ToVector3Shifted();
            var ticksToSkip = this.BurstWarmupTicksLeft;
            var instant = Projectile.projectile is ProjectilePropertiesCE CIWSProjectilePropertiesCE && CIWSProjectilePropertiesCE.isInstant;
            if (instant)
            {
                var to = targetProjectile.NextPositions.Skip(ticksToSkip).FirstOrFallback(Vector3.negativeInfinity);
                if (to == Vector3.negativeInfinity)
                {
                    resultingLine = default;
                    return false;
                }
                resultingLine = new ShootLine(originV3.ToIntVec3(), to.ToIntVec3());
                return true;
            }
            int i = 1;
            var report = ShiftVecReportFor((LocalTargetInfo)targetProjectile);
            if (targetProjectile.def.projectile is ProjectilePropertiesCE targetProjectileProperties && Projectile.projectile is ProjectilePropertiesCE CIWS_ProjectileProperties)
            {
                var targetPos1 = new Vector2(targetProjectile.Position.x, targetProjectile.Position.z);
                foreach (var exactPos in targetProjectile.NextPositions.Skip(ticksToSkip))
                {
                    var pos = targetProjectile.TrajectoryWorker.ExactPosToDrawPos(exactPos, targetProjectile.FlightTicks + ticksToSkip + i - 1, (targetProjectile.def.projectile as ProjectilePropertiesCE).TickToTruePos, targetProjectile.def.Altitude);
                    if (i > maximumPredectionTicks)
                    {
                        break;
                    }
                    ShiftTarget(report, false, instant, midBurst, i);

                    Vector2 originV2 = new Vector2(originV3.x, originV3.z), destinationV2 = new Vector2(pos.x, pos.z);
                    var positions = CIWS_ProjectileProperties.TrajectoryWorker.NextPositions(targetProjectile, shotRotation, shotAngle, CIWS_ProjectileProperties.Gravity, originV2, Shooter.Position.ToVector3(), destinationV2, maximumPredectionTicks, maximumPredectionTicks, ShotHeight, false, Vector3.zero, ShotSpeed, originV3, -1f, -1f, -1f, -1f, ShotSpeed, 0).Skip(i + 1).Take(2).ToList();
                    if (positions.Count < 2)
                    {
                        resultingLine = default(ShootLine);
                        return false;
                    }
                    Vector2 ciwsPos1 = new Vector2(positions.First().x, positions.First().z), ciwsPos2 = new Vector2(positions.Last().x, positions.Last().z), targetPos2 = new Vector2(pos.x, pos.z);

                    if (CE_Utility.TryFindIntersectionPoint(ciwsPos1, ciwsPos2, targetPos1, targetPos2, out var point))
                    {
                        resultingLine = new ShootLine(Shooter.Position, point.ToVector3().ToIntVec3());

                        this.sinceTicks = i;
                        return true;
                    }
                    targetPos1 = targetPos2;
                    i++;
                }
            }
            resultingLine = default;
            return false;
        }
        public override void ShowTrajectories()
        {
            base.ShowTrajectories();
            (currentTarget.Thing as ProjectileCE)?.DrawNextPositions();
        }
    }
    public class VerbProperties_CIWSProjectile : VerbProperties_CIWS
    {
        public VerbProperties_CIWSProjectile()
        {
            this.verbClass = typeof(VerbCIWSProjectile);
        }
        public override bool Interceptable(ThingDef targetDef) => targetDef.projectile.speed < maximumSpeed && targetDef.projectile.flyOverhead && base.Interceptable(targetDef);
        public float maximumSpeed = 80;
        protected override IEnumerable<ThingDef> InitAllTargets() => DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.projectile != null && x.projectile.flyOverhead && x.projectile.speed < maximumSpeed);
    }
}
