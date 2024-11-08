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
    public class VerbCIWSSkyfaller : VerbCIWS<Skyfaller>
    {
        protected override string HoldDesc => "HoldSkyfallerFireDesc";
        protected override string HoldLabel => "HoldSkyfallerFire";
        public override IEnumerable<Skyfaller> Targets => Caster.Map?.listerThings.ThingsInGroup(Verse.ThingRequestGroup.ActiveDropPod).OfType<Skyfaller>();

        
        protected override bool IsFriendlyTo(Skyfaller thing) => base.IsFriendlyTo(thing) && thing.ContainedThings().All(x => !x.HostileTo(Caster));

        protected override IEnumerable<Vector3> TargetNextPositions(Skyfaller target)
        {
            return target.DrawPositions();
        }
        public override Vector3 GetTargetLoc(LocalTargetInfo target, int sinceTicks)
        {
            if (target.Thing is Skyfaller skyfaller)
            {
                return skyfaller.DrawPosSinceTicks(sinceTicks);
            }
            return base.GetTargetLoc(target, sinceTicks);
        }
        public override float GetTargetHeight(LocalTargetInfo target, Thing cover, bool roofed, Vector3 targetLoc, int sinceTicks)
        {
            if (target.Thing is Skyfaller skyfaller)
            {
                return 0.5f * (skyfaller.ticksToImpact - sinceTicks);
            }
            return base.GetTargetHeight(target, cover, roofed, targetLoc, sinceTicks);
        }

    }
    public class VerbProperties_CIWSSkyfaller : VerbProperties_CIWS
    {
        public VerbProperties_CIWSSkyfaller()
        {
            this.verbClass = typeof(VerbCIWSSkyfaller);
        }
        protected override IEnumerable<ThingDef> InitAllTargets() => DefDatabase<ThingDef>.AllDefsListForReading.Where(x => (typeof(Skyfaller).IsAssignableFrom(x.thingClass) && typeof(IActiveDropPod).IsAssignableFrom(x.thingClass)));
    }
}
