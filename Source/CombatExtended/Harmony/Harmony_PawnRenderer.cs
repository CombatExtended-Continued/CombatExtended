using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(PawnRenderer), "DrawEquipmentAiming")]
    internal static class Harmony_PawnRenderer_DrawEquipmentAiming
    {
        private static void DrawMeshModified(Mesh mesh, Vector3 position, Quaternion rotation, Material mat, int layer, Thing eq, float aimAngle)
        {
            var drawData = eq.def.GetModExtension<GunDrawExtension>() ?? new GunDrawExtension();
            var scale = new Vector3(drawData.DrawSize.x, 1, drawData.DrawSize.y);
            var posVec = new Vector3(drawData.DrawOffset.x, 0, drawData.DrawOffset.y);
            if (aimAngle > 200 && aimAngle < 340)
            {
                posVec.x *= -1;
            }

            posVec = posVec.RotatedBy(rotation.eulerAngles.y);

            var matrix = new Matrix4x4();
            matrix.SetTRS(position + posVec, rotation, scale);

            Graphics.DrawMesh(mesh, matrix, mat, layer);
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            codes[codes.Count - 2].operand =
                AccessTools.Method(typeof(Harmony_PawnRenderer_DrawEquipmentAiming), nameof(DrawMeshModified));
            codes.InsertRange(codes.Count - 2, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldarg_3)
            });

            return codes;
        }
    }

    /// <summary>
    /// Patches renderer to skip separate drawing of shell layer.
    /// That layer is now drawn via Harmony_PawnGraphicSet.RenderShellAsNormalLayer, so that it obeys drawOrder.
    /// </summary>
    [HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal")]
    [HarmonyPatch(new Type[] { typeof(Vector3), typeof(float), typeof(bool), typeof(Rot4), typeof(Rot4), typeof(RotDrawMode), typeof(bool), typeof(bool) })]
    public static class SillyTests
    {
        enum PatchStage { Searching, ExtractTargetLabel, PurgingInstructions, Finished };

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            PatchStage currentStage = PatchStage.Searching;
            Label targetLabel = new Label();

            foreach (var code in instructions)
            {
                if (currentStage == PatchStage.Searching)
                {
                    if (code.operand == AccessTools.Field(typeof(ApparelLayerDefOf), "Shell"))
                    {
                        currentStage = PatchStage.ExtractTargetLabel;
                    }
                }
                else if (currentStage == PatchStage.ExtractTargetLabel)
                {
                    targetLabel = (Label)code.operand;
                    currentStage = PatchStage.PurgingInstructions;
                }
                else if (currentStage == PatchStage.PurgingInstructions)
                {
                    if (code.labels.Contains(targetLabel))
                    {
                        currentStage = PatchStage.Finished;
                    }
                    else
                    {
                        code.opcode = OpCodes.Nop;
                    }
                }

                yield return code;
            }
        }
    }
    
}