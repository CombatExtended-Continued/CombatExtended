using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using System.Xml;

namespace CombatExtended
{
    public class ApparelPartialStat
    {
        public StatDef stat;

        public float value;

        public List<BodyPartDef> parts;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {

            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "stat", xmlRoot.FirstChild.Name, null, null);
            this.value = ParseHelper.FromString<float>(xmlRoot.FirstChild.InnerText);
            if (parts == null)
            {
                parts = new List<BodyPartDef>();
            }
            foreach (XmlNode node in xmlRoot.LastChild.ChildNodes)
            {
              

                DirectXmlCrossRefLoader.RegisterListWantsCrossRef(parts, node.InnerText);
            }
        }
    }
}
