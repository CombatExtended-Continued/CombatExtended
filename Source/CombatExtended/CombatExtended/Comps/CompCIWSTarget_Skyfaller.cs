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
    public class CompCIWSTarget_Skyfaller : CompCIWSTarget
    {
        public override IEnumerable<Vector3> PredictedPositions
        {
            get
            {
                return (parent as Skyfaller).DrawPositions().Select(x => x.WithY(45f));
            }
        }

        public override bool IsFriendlyTo(Thing caster)
        {
            return (parent as Skyfaller).ContainedThings().All(x => !x.HostileTo(caster));
        }
    }
}
