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
        public override IEnumerable<Skyfaller> Targets => Caster.Map?.listerThings.ThingsInGroup(Verse.ThingRequestGroup.ActiveDropPod).OfType<Skyfaller>();


        protected override bool IsFriendlyTo(Skyfaller thing) => base.IsFriendlyTo(thing) && thing.ContainedThings().All(x => !x.HostileTo(Caster));
        protected override IEnumerable<Vector3> PredictPositions(Skyfaller target, int maxTicks, bool drawPos)
        {
            return target.PredictPositions(maxTicks);
        }
        protected override bool ShouldAimDrawPos(Skyfaller target) => true;
        public override BaseTrajectoryWorker TrajectoryWorker(LocalTargetInfo target)
        {
            var defaultTrajectoryWorker = base.TrajectoryWorker(target);
            if (target.Thing is Skyfaller skyfaller && ShouldAimDrawPos(skyfaller) && defaultTrajectoryWorker.GetType() == typeof(LerpedTrajectoryWorker))
            {
                return lerpedTrajectoryWorker;
            }
            return defaultTrajectoryWorker;
        }

    }
    public class VerbProperties_CIWSSkyfaller : VerbProperties_CIWS
    {
        public VerbProperties_CIWSSkyfaller()
        {
            this.verbClass = typeof(VerbCIWSSkyfaller);
            this.holdFireIcon = "UI/Buttons/CE_CIWS_Skyfaller";
            this.holdFireLabel = "HoldCloseInSkyfallersFire";
            this.holdFireDesc = "HoldCloseInSkyfallersFireDesc";
        }
        protected override IEnumerable<ThingDef> InitAllTargets() => DefDatabase<ThingDef>.AllDefsListForReading.Where(x => (typeof(Skyfaller).IsAssignableFrom(x.thingClass) && typeof(IActiveDropPod).IsAssignableFrom(x.thingClass)));
    }
}
