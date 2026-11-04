using System.Collections.Generic;
using LudeonTK;
using Verse;

namespace CombatExtended;

public static class GasDebugActions
{
    private const int OpenAreaRadius = 8;

    [DebugAction("CE", "Gas: fill room...", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void FillRoom()
    {
        PickGasThen("Fill", (layer, cell) => layer.SetDensityAt(cell, layer.Def.maxDensity));
    }

    [DebugAction("CE", "Gas: clear room...", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void ClearRoom()
    {
        PickGasThen("Clear", (layer, cell) => layer.SetDensityAt(cell, 0f));
    }

    [DebugAction("CE", "Gas: clear map", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void ClearMap()
    {
        IReadOnlyList<GasLayer> layers = Find.CurrentMap.GetComponent<CEGasGrid>().Layers;
        for (int i = 0; i < layers.Count; i++)
        {
            layers[i].RemoveAllGas();
        }
    }


    private static void PickGasThen(string verb, System.Action<GasLayer, IntVec3> apply)
    {
        var options = new List<DebugMenuOption>();
        foreach (GasLayer layer in Find.CurrentMap.GetComponent<CEGasGrid>().Layers)
        {
            GasLayer picked = layer;
            options.Add(new DebugMenuOption(picked.Def.LabelCap, DebugMenuOptionMode.Action, () =>
            {
                DebugTools.curTool = new DebugTool($"{verb} {picked.Def.label}", () => ForRoomUnderMouse(cell => apply(picked, cell)));
            }));
        }
        Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
    }

    private static void ForRoomUnderMouse(System.Action<IntVec3> action)
    {
        Map map = Find.CurrentMap;
        IntVec3 clicked = UI.MouseCell();
        if (!clicked.InBounds(map))
        {
            return;
        }

        Room room = clicked.GetRoom(map);
        IEnumerable<IntVec3> cells = room is { TouchesMapEdge: false } ? room.Cells : GenRadial.RadialCellsAround(clicked, OpenAreaRadius, true);

        foreach (IntVec3 cell in cells)
        {
            if (cell.GetEdifice(map)?.def.Fillage != FillCategory.Full)
            {
                action(cell);
            }
        }
    }
}
