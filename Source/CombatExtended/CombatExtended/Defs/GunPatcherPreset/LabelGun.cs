using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;
using RimWorld;

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

        public List<StatModifier> stats;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            foreach (XmlNode child in xmlRoot.ChildNodes)
            {
                switch (child.Name)
                {
                    case "magCap":
                        magCap = ParseHelper.FromString<int>(child.InnerText);
                        break;
                    case "reloadTime":
                        reloadTime = ParseHelper.FromString<float>(child.InnerText);
                        break;
                    case "mass":
                        mass = ParseHelper.FromString<float>(child.InnerText);
                        break;
                    case "bulk":
                        bulk = ParseHelper.FromString<float>(child.InnerText);
                        break;
                    case "caliber":
                        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "caliber", child.InnerText);
                        break;
                    case "names":
                        if (names == null)
                        {
                            names = new List<string>();
                        }
                        foreach (XmlNode node in child.ChildNodes)
                        {
                            names.Add(node.InnerText);
                        }
                        break;
                    case "stats":
                        if (stats == null)
                        {
                            stats = new List<StatModifier>();
                        }
                        foreach (XmlNode node in child.ChildNodes)
                        {
                            var newMod = new StatModifier();

                            Log.Message(node.ToString());
                            newMod.LoadDataFromXmlCustom(node);

                            stats.Add(newMod);
                        }
                        break;
                }
            }

           

           
        }
    }
}
