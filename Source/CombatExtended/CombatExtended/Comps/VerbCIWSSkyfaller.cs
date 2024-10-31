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
        public override IEnumerable<Skyfaller> Targets => Caster.Map?.listerThings.ThingsInGroup(Verse.ThingRequestGroup.ActiveDropPod).OfType<Skyfaller>().Union(CompCIWSTarget.Targets(Caster.Map).OfType<Skyfaller>());

        
        protected override bool IsFriendlyTo(Skyfaller thing) => base.IsFriendlyTo(thing) && thing.ContainedThings().All(x => !x.HostileTo(Caster));

    }
    public class VerbProperties_CIWSSkyfaller : VerbProperties_CIWS
    {
        public VerbProperties_CIWSSkyfaller()
        {
            this.verbClass = typeof(VerbCIWSSkyfaller);
        }
        protected override IEnumerable<ThingDef> InitAllTargets() => DefDatabase<ThingDef>.AllDefsListForReading.Where(x => (typeof(Skyfaller).IsAssignableFrom(x.thingClass) && x.HasComp<CompCIWSTarget>()) || typeof(IActiveDropPod).IsAssignableFrom(x.thingClass));
    }
}
