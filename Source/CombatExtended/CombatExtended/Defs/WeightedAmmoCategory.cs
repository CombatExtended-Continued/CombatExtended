using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using System.Xml;

namespace CombatExtended;
public class WeightedAmmoCategory
{
    public AmmoCategoryDef ammoCategory;

    public float chance;

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "ammoCategory", xmlRoot.Name);

            chance = ParseHelper.FromString<float>(xmlRoot.InnerText);
        }
    }
}
