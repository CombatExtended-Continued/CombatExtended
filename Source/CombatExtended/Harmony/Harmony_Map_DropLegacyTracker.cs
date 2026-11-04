using System.Xml;
using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE;

// Remove later after we are confident bulk of users have been migrated
[HarmonyPatch(typeof(Map), "ExposeComponents")]
internal static class Harmony_Map_DropLegacyTracker
{
    private const string OldTrackerClassName = "CombatExtended.BlackSmokeTracker";

    internal static void Prefix()
    {
        if (Scribe.mode != LoadSaveMode.LoadingVars)
        {
            return;
        }

        XmlElement components = Scribe.loader.curXmlParent?["components"];
        if (components == null)
        {
            return;
        }

        for (int i = components.ChildNodes.Count - 1; i >= 0; i--)
        {
            XmlNode entry = components.ChildNodes[i];
            if (entry.Attributes?["Class"]?.Value == OldTrackerClassName)
            {
                components.RemoveChild(entry);
            }
        }
    }
}
