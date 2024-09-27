using System.Xml;
using Verse;

namespace CombatExtended
{
    public class PatchOperationRemove_GenericOnly : PatchOperationRemove
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
