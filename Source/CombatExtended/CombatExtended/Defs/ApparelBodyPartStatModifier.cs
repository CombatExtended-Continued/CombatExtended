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
            parts ??= new List<BodyPartModifier>();
            foreach (XmlNode node in xmlRoot.ChildNodes)
            {
                BodyPartModifier modifier = new BodyPartModifier();
                modifier.LoadDataFromXmlCustom(node);                    
                parts.Add(modifier);               
            }
        }
    }    
}
