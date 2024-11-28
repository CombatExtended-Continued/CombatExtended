using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended
{
    public class CompVerbDisabler : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            var verbs = parent.GetComp<CompEquippable>()?.AllVerbs.OfType<IVerbDisableable>();
            if (verbs != null)
            {
                foreach (var verb in verbs)
                {
                    var command = new Command_Toggle()
                    {
                        defaultDesc = verb.HoldFireDesc.Translate(),
                        defaultLabel = verb.HoldFireLabel.Translate(),
                        icon = verb.HoldFireIcon,
                        isActive = () => verb.HoldFire,
                        toggleAction = () => verb.HoldFire = !verb.HoldFire,

                    };
                    yield return command;
                }
            }
        }
    }
}
