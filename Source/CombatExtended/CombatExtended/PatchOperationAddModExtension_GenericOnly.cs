using System.Xml;
using Verse;

namespace CombatExtended
{
    public class PatchOperationAddModExtension_GenericOnly : PatchOperationAddModExtension
    {
        public override bool ApplyWorker(XmlDocument xml)
        {
            if (!Controller.settings.GenericAmmo)
            {
                return true;
            }
            return base.ApplyWorker(xml);
        }
    }
}
