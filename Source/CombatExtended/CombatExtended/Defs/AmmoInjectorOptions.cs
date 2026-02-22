using System.Collections.Generic;
using System.Xml;
using Verse;

namespace CombatExtended;

public class AmmoInjectorOptions : Def
{
    public BenchesByTag benchesByTag;

}
public class BenchesByTag
{
    public Dictionary<string, List<string>> data = new Dictionary<string, List<string>>();

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        foreach (XmlNode tagNode in xmlRoot.ChildNodes)
        {
            if (tagNode.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            string tagName = tagNode.Name;
            List<string> benchList = DirectXmlToObject.ObjectFromXml<List<string>>(tagNode, doPostLoad: false);
            if (benchList is { Count: > 0 })
            {
                data[tagName] = benchList;
            }
        }
    }
}

