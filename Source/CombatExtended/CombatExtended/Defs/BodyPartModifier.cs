using System;
using System.Collections.Generic;
using System.Xml;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public class BodyPartModifier
    {
        public BodyPartDef part;
        public float value;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "part", xmlRoot.Name);
            value = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
        }
    }
}
