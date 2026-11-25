using System;
using System.Xml;
using Verse;

namespace CombatExtended;

[Obsolete("ConditionalGeneric patch op has been deprecated in favor of PatchOperationSettingsConditional", false)]
public class PatchOperation_ConditionalGeneric : PatchOperation
{
    public PatchOperation standard;
    public PatchOperation generic;

    public override bool ApplyWorker(XmlDocument xml)
    {
        if (Controller.settings.GenericAmmo)
        {
            if (generic != null)
            {
                return generic.Apply(xml);
            }
        }
        else if (standard != null)
        {
            return standard.Apply(xml);
        }

        return true;
    }

    public override void Complete(string modIdentifier)
    {
        base.Complete(modIdentifier);
        Log.WarningOnce($"[{modIdentifier}] PatchOperation_ConditionalGeneric has been deprecated in favor of PatchOperationSettingsConditional and will be removed in a future version", modIdentifier.GetHashCode());
    }
}
