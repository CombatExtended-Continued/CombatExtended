using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.Code;

namespace CombatExtended
{
    public class CompCIWSProjectile : CompCIWS<Projectile>
    {
        protected override string HoldDesc => "HoldCloseInProjectilesFireDesc";
        protected override string HoldLabel => "HoldCloseInProjectilesFire";
        public new CompProperties_CIWSProjectile Props => props as CompProperties_CIWSProjectile;

        public override IEnumerable<Projectile> Targets => parent.Map?.listerThings.ThingsInGroup(ThingRequestGroup.Projectile).OfType<Projectile>() ?? Array.Empty<Projectile>();

        public override Verb Verb
        {
            get
            {
                return Turret.GunCompEq.AllVerbs.OfType<Verb_CIWS_Shoot>().FirstOrDefault() ?? base.Verb;
            }
        }
        protected override bool IsFriendlyTo(Projectile thing) => base.IsFriendlyTo(thing) && !thing.Launcher.HostileTo(parent);
        public override bool HasTarget => Turret.currentTargetInt.Thing is Projectile;
    }
    public class CompProperties_CIWSProjectile : CompProperties_CIWS
    {
        public CompProperties_CIWSProjectile()
        {
            this.compClass = typeof(CompCIWSProjectile);
        }
        public override bool Interceptable(ThingDef targetDef) => targetDef.projectile.speed < maximumSpeed && targetDef.projectile.flyOverhead && base.Interceptable(targetDef);
        public float maximumSpeed = 80;
        protected override IEnumerable<ThingDef> InitAllTargets() => DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.projectile != null && x.projectile.flyOverhead && x.projectile.speed < maximumSpeed);
    }
}
