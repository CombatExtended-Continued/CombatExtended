using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CombatExtended;

public sealed class SectionLayer_CEGas : SectionLayer
{
    // Small offset per gas so overlapping gases do not z-fight.
    private const float AltitudeStepPerGas = 0.001f;

    private const int VerticesPerQuad = 4;

    private static readonly FloatRange PuffOffsetRange = new FloatRange(-0.45f, 0.45f);
    private static readonly FloatRange PuffSizeFactorRange = new FloatRange(0.8f, 1.2f);

    private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();

    public SectionLayer_CEGas(Section section) : base(section) => relevantChangeTypes = CE_MapMeshFlagDefOf.CE_Gas;

    public override bool Visible => DebugViewSettings.drawGas;

    public override void Regenerate()
    {
        ClearSubMeshes(MeshParts.All);

        CEGasGrid grid = Map.GetComponent<CEGasGrid>();
        if (grid is not { AnyGas: true })
        {
            return;
        }

        float altitude = AltitudeLayer.Gas.AltitudeFor();
        IReadOnlyList<GasLayer> layers = grid.Layers;
        for (int i = 0; i < layers.Count; i++)
        {
            if (!layers[i].HasActiveCells)
            {
                continue;
            }

            LayerSubMesh subMesh = GetSubMesh(layers[i].Def.Material);
            AddCells(layers[i], subMesh, altitude + (i * AltitudeStepPerGas));
            if (subMesh.verts.Count > 0)
            {
                subMesh.FinalizeMesh(MeshParts.All);
            }
        }
    }

    // Copied from SectionLayer_Gas.Regenerate
    private void AddCells(GasLayer layer, LayerSubMesh subMesh, float altitude)
    {
        CellRect cells = section.CellRect.ClipInsideMap(Map);
        int puffSeed = cells.minX;
        foreach (IntVec3 cell in cells)
        {
            float density = layer.DensityAtIndex(Map.cellIndices.CellToIndex(cell));
            if (layer.Def.IsDrawn(density))
            {
                AddCell(layer.Def, density, cell, puffSeed, subMesh, altitude);
            }
            puffSeed++;
        }
    }

    // Copied from SectionLayer_Gas
    private static void AddCell(GasDef def, float density, IntVec3 cell, int puffSeed, LayerSubMesh subMesh, float altitude)
    {

        Rand.PushState(puffSeed);
        float halfWidth = PuffSizeFactorRange.RandomInRange * def.drawSize / 2f;
        float halfHeight = PuffSizeFactorRange.RandomInRange * def.drawSize / 2f;
        Vector3 center = cell.ToVector3Shifted();
        center.x += PuffOffsetRange.RandomInRange;
        center.z += PuffOffsetRange.RandomInRange;
        Rand.PopState();
        Color32 color = def.ColorFor(density);

        int firstVertex = subMesh.verts.Count;
        subMesh.verts.Add(new Vector3(center.x - halfWidth, altitude, center.z - halfHeight));
        subMesh.verts.Add(new Vector3(center.x - halfWidth, altitude, center.z + halfHeight));
        subMesh.verts.Add(new Vector3(center.x + halfWidth, altitude, center.z + halfHeight));
        subMesh.verts.Add(new Vector3(center.x + halfWidth, altitude, center.z - halfHeight));
        subMesh.uvs.Add(new Vector3(0f, 0f, puffSeed));
        subMesh.uvs.Add(new Vector3(0f, 1f, puffSeed));
        subMesh.uvs.Add(new Vector3(1f, 1f, puffSeed));
        subMesh.uvs.Add(new Vector3(1f, 0f, puffSeed));
        for (int corner = 0; corner < VerticesPerQuad; corner++)
        {
            subMesh.colors.Add(color);
        }
        subMesh.tris.Add(firstVertex);
        subMesh.tris.Add(firstVertex + 1);
        subMesh.tris.Add(firstVertex + 2);
        subMesh.tris.Add(firstVertex);
        subMesh.tris.Add(firstVertex + 2);
        subMesh.tris.Add(firstVertex + 3);
    }

    public override void DrawLayer()
    {
        if (!Visible)
        {
            return;
        }

        _propertyBlock.SetFloat(ShaderPropertyIDs.AgeSecsPausable, RealTime.UnpausedRealTime);
        for (int i = 0; i < subMeshes.Count; i++)
        {
            LayerSubMesh subMesh = subMeshes[i];
            if (subMesh.finalized && !subMesh.disabled)
            {
                Graphics.DrawMesh(subMesh.mesh, Vector3.zero, Quaternion.identity, subMesh.material, 0, null, 0, _propertyBlock);
            }
        }
    }
}
