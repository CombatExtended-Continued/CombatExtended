using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Xml;

namespace CombatExtended
{
    public class ApparelPartialStat
    {
        public StatDef stat;

        public List<BodyPartDef> parts;

        public float statValue = 1f;

        public bool isStatValueStatic = false;

        public float GetStatValue(float baseValue)
        {
            return isStatValueStatic ? statValue : statValue * baseValue;
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            int index = 0;
            if (xmlRoot.FirstChild.Name.Contains("use"))
            {
                isStatValueStatic = ParseHelper.FromString<bool>(xmlRoot.FirstChild.InnerText);
                index = 1;
            }

            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "stat", xmlRoot.ChildNodes[index].Name, null, null);
            this.statValue = ParseHelper.FromString<float>(xmlRoot.ChildNodes[index].InnerText);

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
