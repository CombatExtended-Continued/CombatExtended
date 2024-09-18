using System.Xml;
using Verse;

namespace CombatExtended.CombatExtended
{
    public abstract class PatchOperationConditional_Abstract : PatchOperation
    {
        private PatchOperation match;

        private PatchOperation nomatch;

        protected abstract bool Applicable(XmlDocument xml);

        public override bool ApplyWorker(XmlDocument xml)
        {
            if (Applicable(xml))
            {
                if (match != null)
                {
                    return match.Apply(xml);
                }
            }
            else if (nomatch != null)
            {
                return nomatch.Apply(xml);
            }
            if (match == null)
            {
                return nomatch != null;
            }
            return true;
        }
    }

    public class PatchOperationConditional_GenericAmmo : PatchOperationConditional_Abstract
    {
        protected override bool Applicable(XmlDocument xml)
        {
            return Controller.settings.GenericAmmo;
        }
    }
    public class PatchOperationConditional_AmmoSystemOff : PatchOperationConditional_Abstract
    {
        protected override bool Applicable(XmlDocument xml)
        {
            return !Controller.settings.EnableAmmoSystem;
        }
    }
}
