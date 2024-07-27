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
    public class CompCIWSSkyfaller : CompCIWS<Skyfaller>
    {
        protected override string HoldDesc => "HoldSkyfallerFireDesc";
        protected override string HoldLabel => "HoldSkyfallerFire";
        public override IEnumerable<Skyfaller> Targets => parent.Map?.listerThings.ThingsInGroup(Verse.ThingRequestGroup.ActiveDropPod).OfType<Skyfaller>().Union(CompCIWSTarget.Targets(parent.Map).OfType<Skyfaller>());

        public override Verb Verb
        {
            get
            {
                return Turret.GunCompEq.AllVerbs.OfType<Verb_CIWS_Shoot>().FirstOrDefault() ?? base.Verb;
            }
        }
        protected override bool IsFriendlyTo(Skyfaller thing) => base.IsFriendlyTo(thing) && Utils.ContainedThings(thing).All(x => !x.HostileTo(parent));
        public override bool HasTarget => Turret.currentTargetInt.Thing is Skyfaller;

    }
    public class CompProperties_CIWSSkyfaller : CompProperties_CIWS
    {
        public CompProperties_CIWSSkyfaller()
        {
            this.compClass = typeof(CompCIWSSkyfaller);
        }
        protected override IEnumerable<ThingDef> InitAllTargets() => DefDatabase<ThingDef>.AllDefsListForReading.Where(x => (typeof(Skyfaller).IsAssignableFrom(x.thingClass) && x.HasComp<CompCIWSTarget>()) || typeof(IActiveDropPod).IsAssignableFrom(x.thingClass));
    }
}
