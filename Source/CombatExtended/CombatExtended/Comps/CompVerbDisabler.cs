using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended;
public class CompVerbDisabler : ThingComp
{
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }
        var verbs = parent.GetComp<CompEquippable>()?.AllVerbs;
        if (verbs != null)
        {
            foreach (var verb in verbs)
            {
                if (!(verb is IVerbDisableable disableableVerb))
                {
                    continue;
                }
                if (verb is VerbCIWS && !Controller.settings.EnableCIWS)
                {
                    continue;
                }
                var command = new Command_Toggle()
                {
                    defaultDesc = disableableVerb.HoldFireDesc.Translate(),
                    defaultLabel = disableableVerb.HoldFireLabel.Translate(),
                    icon = disableableVerb.HoldFireIcon,
                    isActive = () => disableableVerb.HoldFire,
                    toggleAction = () => disableableVerb.HoldFire = !disableableVerb.HoldFire,

                };
                yield return command;
            }
        }
    }
}
