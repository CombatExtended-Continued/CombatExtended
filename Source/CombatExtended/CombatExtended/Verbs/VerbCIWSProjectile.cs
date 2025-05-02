using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class VerbCIWSProjectile : VerbCIWS<ProjectileCE>
    {
        public new VerbProperties_CIWSProjectile Props => verbProps as VerbProperties_CIWSProjectile;

        public override IEnumerable<ProjectileCE> Targets => Caster.Map?.listerThings.ThingsInGroup(ThingRequestGroup.Projectile).OfType<ProjectileCE>() ?? Array.Empty<ProjectileCE>();

        protected override bool IsFriendlyTo(ProjectileCE thing) => base.IsFriendlyTo(thing) && !thing.launcher.HostileTo(Caster);
        protected override bool ShouldAimDrawPos(ProjectileCE target) => Props.forceUseDrawPos || projectilePropsCE.TrajectoryWorker.GetType() != target.TrajectoryWorker.GetType(); //Because the target may use different render methods
        protected override IEnumerable<Vector3> PredictPositions(ProjectileCE target, int maxTicks, bool drawPos)
        {
            return target.TrajectoryWorker.PredictPositions(target, maxTicks, drawPos);
        }
        public override BaseTrajectoryWorker TrajectoryWorker(LocalTargetInfo target)
        {
            var defaultTrajectoryWorker = base.TrajectoryWorker(target);
            if (target.Thing is ProjectileCE projectile && ShouldAimDrawPos(projectile) && defaultTrajectoryWorker.GetType() == typeof(LerpedTrajectoryWorker))
            {
                return lerpedTrajectoryWorker;
            }
            return defaultTrajectoryWorker;
        }
    }
    public class VerbProperties_CIWSProjectile : VerbProperties_CIWS
    {
        public VerbProperties_CIWSProjectile()
        {
            this.verbClass = typeof(VerbCIWSProjectile);
            this.holdFireIcon = "UI/Buttons/CE_CIWS_Projectile";
            this.holdFireLabel = "HoldCloseInProjectilesFire";
            this.holdFireDesc = "HoldCloseInProjectilesFireDesc";
        }
        public override bool Interceptable(ThingDef targetDef) => targetDef.projectile.speed < maximumSpeed && targetDef.projectile.flyOverhead && base.Interceptable(targetDef);
        public float maximumSpeed = 80;
        public bool shouldInterceptUnpredictable = true;
        protected override IEnumerable<ThingDef> InitAllTargets() => DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.projectile != null && x.projectile.flyOverhead && x.projectile.speed < maximumSpeed);
    }
}
