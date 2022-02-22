using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace CombatExtended
{
    public class LabelGun
    {
        public List<string> names;

        public int magCap;

        public float reloadTime;

        public float mass;

        public float bulk;

        public AmmoSetDef caliber;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            foreach (XmlNode child in xmlRoot.ChildNodes)
            {
                Log.Message("name " + child.Name + " innter text " + child.InnerText);
            }

            magCap = ParseHelper.FromString<int>(xmlRoot.FirstChild.InnerText);

            reloadTime = ParseHelper.FromString<float>(xmlRoot.ChildNodes[1].InnerText);

            mass = ParseHelper.FromString<float>(xmlRoot.ChildNodes[2].InnerText);

            bulk = ParseHelper.FromString<float>(xmlRoot.ChildNodes[3].InnerText);

            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "caliber", xmlRoot.ChildNodes[4].InnerText);

            if (names == null)
            {
                names = new List<string>();
            }
            foreach (XmlNode node in xmlRoot.LastChild.ChildNodes)
            {
                names.Add(node.InnerText);
            }
        }
    }
}
