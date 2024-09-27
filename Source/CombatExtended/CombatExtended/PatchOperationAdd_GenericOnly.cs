using System.Xml;
using Verse;

namespace CombatExtended
{
    public class PatchOperationAdd_GenericOnly : PatchOperationAdd
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
