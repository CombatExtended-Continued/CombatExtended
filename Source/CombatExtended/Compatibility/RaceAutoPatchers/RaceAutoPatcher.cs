using System.Text;
using Verse;

namespace CombatExtended.Compatibility;
[StaticConstructorOnStartup]
public class RaceAutoPatcher
{
    static RaceAutoPatcher()
    {
        if (!Controller.settings.EnableRaceAutopatcher)
        {
            return;
        }

        #region HAR patching if HAR is loaded
        if (ModLister.HasActiveModWithName("Humanoid Alien Races"))
        {
            RaceUtil.PatchHARs();
        }
        #endregion

        #region Race patching

        int patchCount = 0;
        int racePropsCount = 0;
        int durabilityCount = 0;
        StringBuilder patchedAnimals = new StringBuilder();
        StringBuilder patchedRaceProps = new StringBuilder();
        StringBuilder patchedArmorDurability = new StringBuilder();

        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
        {
            if (def.race == null)
            {
                continue;
            }
            if (def.GetModExtension<RacePropertiesExtensionCE>() == null)
            {
                def.modExtensions ??= [];
                def.modExtensions.Add(new RacePropertiesExtensionCE
                {
                    bodyShape = CE_BodyShapeDefOf.Quadruped
                });
                racePropsCount++;
                patchedRaceProps.AppendLine(def.defName);
            }

            if (def.tools.NullOrEmpty())
            {
                continue;
            }
            bool alreadyPatched = false;
            for (int i = 0; i < def.tools.Count; i++)
            {
                if (def.tools[i] is not ToolCE)
                {
                    continue;
                }
                alreadyPatched = true;
                break;
            }
            if (alreadyPatched)
            {
                continue;
            }
            for (int i = 0; i < def.tools.Count; i++)
            {
                Tool tool = def.tools[i];

                if (tool is null or ToolCE)
                {
                    continue;
                }

                def.tools[i] = tool.ConvertTool();
            }

            RaceUtil.TryPatchSharpArmor(def, out float bodyPartSharpArmor);
            RaceUtil.TryPatchBluntArmor(def, out float bodyPartBluntArmor);

            if (bodyPartSharpArmor >= 2f || bodyPartBluntArmor >= 4f)
            {
                CompProperties_ArmorDurability comp = def.GetCompProperties<CompProperties_ArmorDurability>();

                if (comp == null)
                {
                    def.comps ??= [];

                    def.comps.Add(new CompProperties_ArmorDurability
                    {
                        Durability = 0.5f * def.race.baseBodySize * def.race.baseHealthScale,
                        Regenerates = true,
                        RegenInterval = 600,
                        RegenValue = 5f
                    });
                    durabilityCount++;
                    patchedArmorDurability.AppendLine(def.defName);
                }
            }

            patchCount++;
            patchedAnimals.AppendLine(def.ToString());
        }

        if (racePropsCount > 0)
        {
            Log.Message("CE added fallback race properties to " + racePropsCount + " races.\nRace Defs with fallback properties:\n" + patchedRaceProps);
        }

        if (patchCount > 0)
        {
            Log.Message("CE successfully patched " + patchCount.ToString() + " animals.\nAnimal Defs autopatched:" + patchedAnimals);
        }

        if (durabilityCount > 0)
        {
            Log.Message("CE added armor durability to " + durabilityCount + " races.\nRace Defs given armor durability:\n" + patchedArmorDurability);
        }

        #endregion

    }
}
