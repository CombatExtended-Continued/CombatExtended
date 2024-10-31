using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended
{
    public class VerbCIWSProjectile : VerbCIWS<Projectile>
    {
        protected override string HoldDesc => "HoldCloseInProjectilesFireDesc";
        protected override string HoldLabel => "HoldCloseInProjectilesFire";
        public new CompProperties_CIWSProjectile Props => verbProps as CompProperties_CIWSProjectile;

        public override IEnumerable<Projectile> Targets => Caster.Map?.listerThings.ThingsInGroup(ThingRequestGroup.Projectile).OfType<Projectile>() ?? Array.Empty<Projectile>();

        protected override bool IsFriendlyTo(Projectile thing) => base.IsFriendlyTo(thing) && !thing.Launcher.HostileTo(Caster);
        public override bool ValidateTarget(LocalTargetInfo targetInfo, bool showMessages) => Turret.currentTargetInt.Thing is Projectile;
    }
    public class CompProperties_CIWSProjectile : VerbProperties_CIWS
    {
        public CompProperties_CIWSProjectile()
        {
            this.verbClass = typeof(VerbCIWSProjectile);
        }
        public override bool Interceptable(ThingDef targetDef) => targetDef.projectile.speed < maximumSpeed && targetDef.projectile.flyOverhead && base.Interceptable(targetDef);
        public float maximumSpeed = 80;
        protected override IEnumerable<ThingDef> InitAllTargets() => DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.projectile != null && x.projectile.flyOverhead && x.projectile.speed < maximumSpeed);
    }
}
