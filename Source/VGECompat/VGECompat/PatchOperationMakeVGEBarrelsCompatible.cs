
using System.Xml;
using Verse;

namespace CombatExtended.Compatibility.VGECompat;

public class PatchOperationMakeVGEBarrelsCompatible : PatchOperation
{
    public string turretDefName;
    public string turretGunDefName;

    public override bool ApplyWorker(XmlDocument xml)
    {
        XmlNode turretNode = xml.SelectSingleNode($"Defs/ThingDef[defName=\"{turretDefName}\"]");
        XmlNode turretGunNode = xml.SelectSingleNode($"Defs/ThingDef[defName=\"{turretGunDefName}\"]");

        if (turretNode == null || turretNode.NodeType != XmlNodeType.Element)
        {
            Log.Error($"[PatchOperationMakeVGEBarrelsCompatible] turretDefName did not match anything or is not an element: {turretDefName}");
            return false;
        }

        if (turretGunNode == null || turretGunNode.NodeType != XmlNodeType.Element)
        {
            Log.Error($"[PatchOperationMakeVGEBarrelsCompatible] turretDefName did not match anything or is not an element: {turretGunDefName}");
            return false;
        }

        XmlElement turretElement = (XmlElement)turretNode;
        XmlElement turretGunElement = (XmlElement)turretGunNode;

        // Find turret extensions
        XmlNode oldExtensionNode = turretElement.SelectSingleNode("modExtensions/li[@Class=\"VanillaGravshipExpanded.TurretExtension_Barrels\"]");
        if (oldExtensionNode == null || oldExtensionNode.NodeType != XmlNodeType.Element)
        {
            Log.Error(
                $"[PatchOperationMakeVGEBarrelsCompatible] VanillaGravshipExpanded.TurretExtension_Barrels mod extension not found in {turretDefName}"
            );
            return false;
        }

        // Copy this node
        XmlElement newExtensionElement = (XmlElement)oldExtensionNode.CloneNode(true);

        // replace the class
        newExtensionElement.SetAttribute("Class", "CombatExtended.MultiBarrelExtension");

        // get barrel node
        XmlNode barrelNode = newExtensionElement.SelectSingleNode("barrels");

        if (barrelNode == null || barrelNode.NodeType != XmlNodeType.Element)
        {
            Log.Error(
                $"[PatchOperationMakeVGEBarrelsCompatible] Failed to find compatible barrels node"
            );
            return false;
        }

        // get child nodes
        XmlNodeList barrelChildNodes = barrelNode.ChildNodes;

        // add a new offsets node
        XmlElement offsetsElement = xml.CreateElement("offsets");
        foreach (XmlNode item in barrelChildNodes)
        {
            // Offsets are Vector3 in VGE but we need Vector2 (btw their y is useless)
            string[] coordinates = item.InnerText.Trim('(', ')').Split(',');
            if (coordinates.Length >= 3)  // is Vector3
            {
                coordinates = [coordinates[0], coordinates[2]]; // get x, z
                item.InnerText = $"({string.Join(", ", coordinates)})"; // recreate the vector
            }
            offsetsElement.AppendChild(item);

        }
        newExtensionElement.AppendChild(offsetsElement);

        // remove the barrelNode
        newExtensionElement.RemoveChild(barrelNode);

        // Select or Create the modExtensions from Gun Def
        XmlNode turretGunExtensionsNode = turretGunNode.SelectSingleNode("modExtensions");
        if (turretGunExtensionsNode == null)
        {
            turretGunExtensionsNode = xml.CreateElement("modExtensions");
            turretGunNode.AppendChild(turretGunExtensionsNode);
        }

        // Add the new extension to the gun
        turretGunExtensionsNode.AppendChild(newExtensionElement);

        // Remove the old extension from the turret
        turretNode.SelectSingleNode("modExtensions").RemoveChild(oldExtensionNode);

        return true;
    }
}
