using System.Collections.Generic;
using System.Xml;
using RimWorld;
using Verse;

namespace CombatExtended
{  
    public class ApparelBodyPartStatModifier
    {
        public StatDef stat;
        public List<BodyPartModifier> parts = new List<BodyPartModifier>();

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "stat", xmlRoot.Name);

            this.parts = new List<BodyPartModifier>();

            XmlNodeList nodeList = xmlRoot.SelectNodes("parts");
            if (nodeList != null)
            {
                foreach (XmlNode node in nodeList)
                {
                    BodyPartModifier modifier = new BodyPartModifier();
                    modifier.LoadDataFromXmlCustom(node);
                    this.parts.Add(modifier);
                }
            }
        }
    }    
}
