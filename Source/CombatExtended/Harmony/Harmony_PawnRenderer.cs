using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE
{

    /*
       Check this patch if:
       - Apparel is rendered slightly off from the pawn sprite (update YOffset constants based on PawnRenderer values


       If all apparel worn on pawns is the drop image of that apparel,
           CHECK Harmony_ApparelGraphicRecordGetter.cs
           INSTEAD!
    */

    //[HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal")]
    //internal static class Harmony_PawnRenderer_RenderPawnInternal
    //{
    //    // Sync these with vanilla PawnRenderer constants
    //    private const float YOffsetBehind = 0.00306122447f;
    //    private const float YOffsetHead = 0.0244897958f;
    //    private const float YOffsetOnHead = 0.0306122452f;
    //    private const float YOffsetPostHead = 0.03367347f;
    //    private const float YOffsetIntervalClothes = 0.00306122447f;

    //    private static Rot4 north = Rot4.North; // this otherwise creates a new instance per invocation
    //    private static int[] headwearGraphics = new int[16];
    //    // Working under the assumption that
    //    //  A. The rendering function will always be single threaded (if not this needs to be an array of threadID length)
    //    //  B. A single pawn will never have more than 16 headlayers at once (this number could be decreased... but what would it change?)

    //    private static void DrawHeadApparel(PawnRenderer renderer, Mesh mesh, Vector3 rootLoc, Vector3 headLoc, Vector3 headOffset, Rot4 bodyFacing, Quaternion quaternion, bool portrait, ref bool hideHair)
    //    {
    //        var apparelGraphics = renderer.graphics.apparelGraphics;
    //        var headwearSize = 0;
    //        int i = 0;

    //        // Using a manual loop without a List or an Array prevents allocation or MoveNext() functions being called, especially since this array should normally be quite small.
    //        for (i = 0; i < apparelGraphics.Count; i++)
    //        {
    //            if (apparelGraphics[i].sourceApparel.def.apparel.LastLayer.GetModExtension<ApparelLayerExtension>()?.IsHeadwear ?? false)
    //            {
    //                headwearGraphics[headwearSize++] = i; // Store index to apparelrecord instead of the actual apparel
    //            }
    //        }

    //        if (headwearSize == 0)
    //            return;

    //        var interval = YOffsetIntervalClothes / headwearSize;
    //        var headwearPos = headLoc;

    //        for (i = 0; i < headwearSize; i++)
    //        {
    //            var apparelRecord = apparelGraphics[headwearGraphics[i]]; // originalArray[indexWeFoundApparelRecordAt]

    //            if (!apparelRecord.sourceApparel.def.apparel.hatRenderedFrontOfFace)
    //            {
    //                headwearPos.y += interval;
    //                hideHair = true;
    //                var apparelMat = apparelRecord.graphic.MatAt(bodyFacing);
    //                apparelMat = renderer.graphics.flasher.GetDamagedMat(apparelMat);
    //                GenDraw.DrawMeshNowOrLater(mesh, headwearPos, quaternion, apparelMat, portrait);
    //            }
    //            else
    //            {
    //                var maskMat = apparelRecord.graphic.MatAt(bodyFacing);
    //                maskMat = renderer.graphics.flasher.GetDamagedMat(maskMat);
    //                var maskLoc = rootLoc + headOffset;
    //                maskLoc.y += !(bodyFacing == north) ? YOffsetPostHead : YOffsetBehind;
    //                GenDraw.DrawMeshNowOrLater(mesh, maskLoc, quaternion, maskMat, portrait);
    //            }
    //        }
    //    }

    //    private static float GetPostShellOffset(PawnRenderer renderer)
    //    {
    //        var apparelGraphics = renderer.graphics.apparelGraphics.Where(a => a.sourceApparel.def.apparel.LastLayer.drawOrder >= ApparelLayerDefOf.Shell.drawOrder).ToList();
    //        return apparelGraphics.Count == 0 ? 0 : YOffsetIntervalClothes / apparelGraphics.Count;
    //    }

    //    private static bool IsPreShellLayer(ApparelLayerDef layer)
    //    {
    //        return layer.drawOrder < ApparelLayerDefOf.Shell.drawOrder
    //               || (layer.GetModExtension<ApparelLayerExtension>()?.IsHeadwear ?? false)
    //               || layer == ApparelLayerDefOf.Belt;  //Belt is not actually a pre-shell layer, but we want to treat it as such in this patch, to avoid rendering bugs with utility items (e.g: broadshield pack)
    //    }

    //    [HarmonyPriority(Priority.Last)] // Fix for VFE-V compat (:
    //    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //    {
    //        int stage = 0;

    //        foreach (var code in instructions)
    //        {
    //            { // 1. Insert calls for head renderer

    //                // Look for Ldloc.s 14 (only one in the method), VFE vikings modify the IL just before, so it is not easy to contextualise. If it breaks make sure to check compat with Alien Races & VFE-Vikings/Beards
    //                if (code.operand is LocalBuilder lb && lb.LocalIndex == 14 && code.opcode == OpCodes.Ldloc_S)
    //                {
    //                    // Insert new calls for head renderer
    //                    yield return new CodeInstruction(OpCodes.Ldarg_0);
    //                    yield return new CodeInstruction(OpCodes.Ldloc_S, 15);
    //                    yield return new CodeInstruction(OpCodes.Ldarg_1);
    //                    yield return new CodeInstruction(OpCodes.Ldloc_S, 13);
    //                    yield return new CodeInstruction(OpCodes.Ldloc_S, 11);
    //                    yield return new CodeInstruction(OpCodes.Ldarg, 4);
    //                    yield return new CodeInstruction(OpCodes.Ldloc_0);
    //                    yield return new CodeInstruction(OpCodes.Ldarg, 7);
    //                    yield return new CodeInstruction(OpCodes.Ldloca_S, 14);
    //                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Harmony_PawnRenderer_RenderPawnInternal), nameof(DrawHeadApparel)));

    //                    yield return code;

    //                    continue;
    //                }

    //            }

    //            { // 2. Replace Post Shell Rendering 

    //                // Replace ApparelLayerDef::lastLayer != ApparelLayerDefOf::Shell with IsPreShellLayer(ApparelLayerDef::lastLayer)

    //                if (stage == 0 && code.opcode == OpCodes.Ldsfld && ReferenceEquals(code.operand, AccessTools.Field(typeof(ApparelLayerDefOf), nameof(ApparelLayerDefOf.Shell))))
    //                {
    //                    code.opcode = OpCodes.Callvirt;
    //                    code.operand = AccessTools.Method(typeof(Harmony_PawnRenderer_RenderPawnInternal), nameof(IsPreShellLayer));

    //                    stage = 1;
    //                }
    //                else

    //                // We replace ApparelDef != ApparelDef with a bool, so we need to change bne.un.s to brtrue
    //                if (stage == 1)
    //                {
    //                    code.opcode = OpCodes.Brtrue;

    //                    stage = 2;
    //                }

    //            }

    //            { // 3. Insert our own Post Shell Offset Code
    //                if (stage == 2 && code.opcode == OpCodes.Call && ReferenceEquals(code.operand, AccessTools.Method(typeof(GenDraw), nameof(GenDraw.DrawMeshNowOrLater))))
    //                {
    //                    yield return new CodeInstruction(OpCodes.Ldloca_S, 2);
    //                    yield return new CodeInstruction(OpCodes.Dup);
    //                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.y)));
    //                    yield return new CodeInstruction(OpCodes.Ldarg_0);
    //                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Harmony_PawnRenderer_RenderPawnInternal), nameof(GetPostShellOffset)));
    //                    yield return new CodeInstruction(OpCodes.Add);
    //                    yield return new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.y)));

    //                    stage = -1;
    //                }
    //            }

    //            yield return code;
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(PawnRenderer), "DrawEquipmentAiming")]
    //internal static class Harmony_PawnRenderer_DrawEquipmentAiming
    //{
    //    public static Rot4 south = Rot4.South; // creates new invocation per call otherwise

    //    private static void DrawMeshModified(Mesh mesh, Vector3 position, Quaternion rotation, Material mat, int layer, Thing eq, float aimAngle)
    //    {
    //        var drawData = eq.def.GetModExtension<GunDrawExtension>() ?? new GunDrawExtension();
    //        var scale = new Vector3(drawData.DrawSize.x, 1, drawData.DrawSize.y);
    //        var posVec = new Vector3(drawData.DrawOffset.x, 0, drawData.DrawOffset.y);
    //        if (aimAngle > 200 && aimAngle < 340)
    //        {
    //            posVec.x *= -1;
    //        }

    //        posVec = posVec.RotatedBy(rotation.eulerAngles.y);

    //        var matrix = new Matrix4x4();
    //        matrix.SetTRS(position + posVec, rotation, scale);

    //        Graphics.DrawMesh(mesh, matrix, mat, layer);
    //    }

    //    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //    {
    //        var codes = instructions.ToList();
    //        codes[codes.Count - 2].operand =
    //            AccessTools.Method(typeof(Harmony_PawnRenderer_DrawEquipmentAiming), nameof(DrawMeshModified));
    //        codes.InsertRange(codes.Count - 2, new[]
    //        {
    //            new CodeInstruction(OpCodes.Ldarg_1),
    //            new CodeInstruction(OpCodes.Ldarg_3)
    //        });

    //        return codes;
    //    }

    //    internal static void Prefix(PawnRenderer __instance, Pawn ___pawn, ref Vector3 drawLoc)
    //    {
    //        if (___pawn.Rotation == south)
    //        {
    //            drawLoc.y++;
    //        }
    //    }
    //}

}
