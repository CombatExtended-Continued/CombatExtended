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
            var originV3 = Caster.Position.ToVector3Shifted();
            var ticksToSkip = (int)verbProps.warmupTime;
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
            if (targetProjectile.def.projectile is ProjectilePropertiesCE targetProjectileProperties && Projectile.projectile is ProjectilePropertiesCE CIWS_ProjectileProperties)
            {
                var targetPos1 = new Vector2(targetProjectile.Position.x, targetProjectile.Position.z);
                foreach (var pos in targetProjectile.NextPositions.Skip(ticksToSkip))
                {
                    if (i > maximumPredectionTicks)
                    {
                        break;
                    }
                    var report = ShiftVecReportFor(pos.ToIntVec3());
                    ShiftTarget(report);

                    Vector2 originV2 = new Vector2(originV3.x, originV3.z), destinationV2 = new Vector2(pos.x, pos.z);
                    var positions = CIWS_ProjectileProperties.NextPositions(shotRotation, shotAngle, originV2, destinationV2, maximumPredectionTicks, ShotHeight, false, Vector3.zero, ShotSpeed, originV3, -1f, -1f, -1f, -1f, ShotSpeed, 0).Skip(i - 1).Take(2).ToList();
                    if (positions.Count < 2)
                    {
                        resultingLine = default(ShootLine);
                        return false;
                    }
                    Vector2 ciwsPos1 = new Vector2(positions.First().x, positions.First().z), ciwsPos2 = new Vector2(positions.Last().x, positions.Last().z), targetPos2 = new Vector2(pos.x, pos.z);

                    if (CE_Utility.TryFindIntersectionPoint(ciwsPos1, ciwsPos2, targetPos1, targetPos2, out var point))
                    {
                        resultingLine = new ShootLine(Shooter.Position, point.ToVector3().ToIntVec3());
                        return true;
                    }
                    targetPos1 = targetPos2;
                    i++;
                }
            }
            resultingLine = default;
            return false;
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
