using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CombatExtended.HarmonyCE;


[HarmonyPatch(typeof(Gravship), "CopyCellContents")]
internal static class Harmony_Gravship_CopyCellContents_Gas
{
    internal static void Postfix(Gravship __instance, Map oldMap, IntVec3 origin, HashSet<IntVec3> engineFloors)
    {
        CEGasGrid grid = oldMap.GetComponent<CEGasGrid>();
        if (grid is not { AnyGas: true })
        {
            return;
        }

        IReadOnlyList<GasLayer> layers = grid.Layers;
        // Settle simulation before reading densities for export.
        for (int i = 0; i < layers.Count; i++)
        {
            layers[i].CompletePendingInterval();
        }

        GravshipGasEntry[] entryPerLayer = new GravshipGasEntry[layers.Count];
        foreach (IntVec3 floor in engineFloors)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                float density = layers[i].DensityAt(floor);
                if (density <= 0f)
                {
                    continue;
                }
                entryPerLayer[i] ??= new GravshipGasEntry { gas = layers[i].Def };
                entryPerLayer[i].cells[floor - origin] = layers[i].Def.ToSaved(density);
                layers[i].SetDensityAt(floor, 0f);
            }
        }

        List<GravshipGasEntry> entries = [];
        foreach (GravshipGasEntry entry in entryPerLayer)
        {
            if (entry != null)
            {
                entries.Add(entry);
            }
        }
        if (entries.Count > 0)
        {
            __instance.GasCargo = entries;
        }
    }
}

[HarmonyPatch(typeof(Gravship), nameof(Gravship.ExposeData))]
internal static class Harmony_Gravship_ExposeData_Gas
{
    internal static void Postfix(Gravship __instance)
    {
        List<GravshipGasEntry> entries = Scribe.mode == LoadSaveMode.Saving ? __instance.GasCargo : null;
        Scribe_Collections.Look(ref entries, "ceGasCargo", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.LoadingVars && entries != null)
        {
            __instance.GasCargo = entries;
        }
    }
}

[HarmonyPatch(typeof(GravshipPlacementUtility), "SpawnGas")]
internal static class Harmony_GravshipPlacementUtility_SpawnGas_Gas
{
    internal static void Postfix(Gravship gravship, Map map, IntVec3 root)
    {
        List<GravshipGasEntry> entries = gravship.GasCargo;
        CEGasGrid grid = map.GetComponent<CEGasGrid>();
        bool mapHasGas = grid is { AnyGas: true };
        if (entries == null && !mapHasGas)
        {
            return;
        }

        // Like vanilla's SetDirect, the ship's air replaces what was in each of its cells, including cells that carried no gas.
        IReadOnlyList<GasLayer> layers = grid.Layers;
        foreach ((IntVec3 offset, _) in gravship.Gases)
        {
            IntVec3 cell = root + offset;
            if (!cell.InBounds(map))
            {
                continue;
            }
            for (int i = 0; i < layers.Count; i++)
            {
                layers[i].SetDensityAt(cell, 0f);
            }
        }

        if (entries != null)
        {
            foreach (GravshipGasEntry entry in entries)
            {
                if (entry.gas == null)
                {
                    continue;
                }

                GasLayer layer = grid.LayerFor(entry.gas);
                foreach ((IntVec3 offset, ushort saved) in entry.cells)
                {
                    IntVec3 cell = root + PrefabUtility.GetAdjustedLocalPosition(offset, gravship.Rotation);
                    if (cell.InBounds(map))
                    {
                        layer.SetDensityAt(cell, entry.gas.FromSaved(saved));
                    }
                }
            }
        }
        gravship.GasCargo = null;
    }
}
