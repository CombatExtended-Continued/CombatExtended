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

        public float mult;

        public List<BodyPartDef> parts;

        public float staticValue = 0f;

        public bool useStatic = false;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            int index = 1;
            if (xmlRoot.FirstChild.Name.Contains("use"))
            {
                this.useStatic = ParseHelper.FromString<bool>(xmlRoot.FirstChild.InnerText);
            }
            else
            {
                index = 0;
            }
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "stat", xmlRoot.ChildNodes[index].Name, null, null);
            if (useStatic)
            {
                this.staticValue = ParseHelper.FromString<float>(xmlRoot.ChildNodes[index].InnerText);
            }
            else
            {
                this.mult = ParseHelper.FromString<float>(xmlRoot.ChildNodes[index].InnerText);
            }


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
