using System;
using System.Linq;
using CombatExtended.WorldObjects;
using RimWorld;
using RimWorld.Planet;
using Verse;
using LudeonTK;

namespace CombatExtended;
public static class CE_DebugUtility
{
    [CE_DebugTooltip(CE_DebugTooltipType.Map)]
    public static string CellPositionTip(Map map, IntVec3 cell)
    {
        return $"Cell: ({cell.x}, {cell.z})";
    }

    [CE_DebugTooltip(CE_DebugTooltipType.World)]
    public static string TileIndexTip(World world, int tile)
    {
        return $"Tile index: {tile}";
    }

    [DebugOutputAttribute("CE", name = "Not patched WorldObjecDefs")]
    public static void NotPatchedWorldObjectDefs()
    {
        var notPatched = DefDatabase<WorldObjectDef>.AllDefsListForReading.Where(x => !x.comps.Any(comp => comp is WorldObjectCompProperties_Health) || !x.comps.Any(comp => comp is WorldObjectCompProperties_Hostility));
        TableDataGetter<WorldObjectDef>[] array = new TableDataGetter<WorldObjectDef>[5];
        array[0] = new TableDataGetter<WorldObjectDef>("ModName", (WorldObjectDef d) => $"{d.modContentPack?.Name}");
        array[1] = new TableDataGetter<WorldObjectDef>("def", (WorldObjectDef d) => $"{d.defName}");
        array[2] = new TableDataGetter<WorldObjectDef>("label", (WorldObjectDef d) => $"{d.label}");
        array[3] = new TableDataGetter<WorldObjectDef>("HostilityPatched", (WorldObjectDef d) => d.comps.Any(comp => comp is WorldObjectCompProperties_Hostility));
        array[4] = new TableDataGetter<WorldObjectDef>("HealthPatched", (WorldObjectDef d) => d.comps.Any(comp => comp is WorldObjectCompProperties_Health));
        DebugTables.MakeTablesDialog<WorldObjectDef>(notPatched, array);
    }
    [DebugAction("CE", actionType = DebugActionType.ToolWorld)]
    public static void Heal()
    {
        int tileID = GenWorld.MouseTile(false);
        foreach (WorldObject worldObject in Find.WorldObjects.ObjectsAt(tileID).ToList<WorldObject>())
        {
            HealthComp comp;
            if ((comp = worldObject.GetComponent<HealthComp>()) != null)
            {
                comp.Health = 1;
                comp.recentShells.Clear();
            }
        }
    }
}
