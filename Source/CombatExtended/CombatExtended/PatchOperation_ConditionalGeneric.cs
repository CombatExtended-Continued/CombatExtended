using System.Xml;
using Verse;

namespace CombatExtended
{
    public class PatchOperation_ConditionalGeneric : PatchOperation
    {
        public PatchOperation standard;
        public PatchOperation generic;

        public override bool ApplyWorker(XmlDocument xml)
        {
            if (Controller.settings.GenericAmmo && generic != null)
            {
                return generic.Apply(xml);
            }
            else if (standard != null)
            {
                return standard.Apply(xml);
            }

            return true;
        }
    }
}
