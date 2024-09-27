using System.Xml;
using Verse;

namespace CombatExtended
{
    public class PatchOperationReplace_GenericOnly : PatchOperationReplace
    {
        public override bool ApplyWorker(XmlDocument xml)
        {
            if (!Controller.settings.GenericAmmo)
            {
                return true; //Early return without a failure
            }
            return base.ApplyWorker(xml);
        }
    }
}
