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

        public override void ShowTrajectories()
        {
            base.ShowTrajectories();
            (currentTarget.Thing as ProjectileCE)?.DrawNextPositions();
        }

        protected override IEnumerable<Vector3> TargetNextPositions(ProjectileCE target)
        {
            int tickOffset = 1;
            foreach (var exactPos in target.NextPositions)
            {
                yield return target.TrajectoryWorker.ExactPosToDrawPos(exactPos, target.FlightTicks + tickOffset, target.ticksToTruePosition, target.def.Altitude).WithY(exactPos.y);
                tickOffset++;
            }
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
        protected override IEnumerable<ThingDef> InitAllTargets() => DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.projectile != null && x.projectile.flyOverhead && x.projectile.speed < maximumSpeed);
    }
}
