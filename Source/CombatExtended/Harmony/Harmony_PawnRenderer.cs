using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE
{

    /*
     *   Check this patch if:
     *   - Apparel is rendered slightly off from the pawn sprite (update YOffset constants based on PawnRenderer values
     *
     *
     *   If all apparel worn on pawns is the drop image of that apparel,
     *       CHECK Harmony_ApparelGraphicRecordGetter.cs
     *       INSTEAD!
     *
     * - Patch Harmony_PawnRenderer_DrawBodyApparel 
     * 
     * This patch is used to enable rendering of backpacks and tac vest and similar items.
     *      
     * - Patch Harmony_PawnRenderer_DrawHeadHair 
     * 
     * Unknown.    
     */
    [HarmonyPatch(typeof(PawnRenderer), "DrawBodyApparel")]
    internal static class Harmony_PawnRenderer_DrawBodyApparel
    {
        /*
         * Sync these with vanilla PawnRenderer constants
         */
        private const float YOffsetBehind = 0.00306122447f;

        private const float YOffsetHead = 0.0244897958f;

        private const float YOffsetOnHead = 0.0306122452f;

        private const float YOffsetPostHead = 0.03367347f;

        private const float YOffsetIntervalClothes = 0.00306122447f;

        private static MethodBase mDrawMeshNowOrLater = AccessTools.Method(typeof(GenDraw), nameof(GenDraw.DrawMeshNowOrLater), parameters: new[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(bool) });

        private static FieldInfo fShell = AccessTools.Field(typeof(ApparelLayerDefOf), nameof(ApparelLayerDefOf.Shell));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                /* 
                 * Replace ApparelLayerDef::lastLayer != ApparelLayerDefOf::Shell with IsPreShellLayer(ApparelLayerDef::lastLayer)
                 * by poping the first part and replacin the second part and changing != to brtrue
                 */
                if (code.opcode == OpCodes.Ldsfld && code.OperandIs(fShell))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_PawnRenderer_DrawBodyApparel), nameof(Harmony_PawnRenderer_DrawBodyApparel.IsVisibleLayer)));
                    i++;
                    yield return new CodeInstruction(OpCodes.Brfalse_S, codes[i].operand);
                    continue;
                }
                /* 
                 * Add the offset to loc before calling mDrawMeshNowOrLater
                 */
                if (code.opcode == OpCodes.Call && code.OperandIs(mDrawMeshNowOrLater))
                {
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 5) { labels = code.labels };
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.y)));
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_PawnRenderer_DrawBodyApparel), nameof(GetPostShellOffset)));
                    yield return new CodeInstruction(OpCodes.Add);
                    yield return new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.y)));
                    code.labels = new List<Label>();
                }
                yield return code;
            }
        }

        /*
         * Add some type of offset (reasoning is in the old code below)
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetPostShellOffset(PawnRenderer renderer)
        {
            List<ApparelGraphicRecord> apparelGraphics = renderer.graphics.apparelGraphics
                .Where(a => a.sourceApparel.def.apparel.LastLayer.drawOrder >= ApparelLayerDefOf.Shell.drawOrder).ToList();
            return apparelGraphics.Count == 0 ? 0 : YOffsetIntervalClothes / apparelGraphics.Count;
        }

        /*
         * This allows us to allow the new layers to render (backpacks, etc)
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsVisibleLayer(ApparelLayerDef layer)
        {
            /* 
             * Belt is not actually a pre-shell layer, but we want to treat it as such in this patch,
             * to avoid rendering bugs with utility items (e.g: broadshield pack)                        
             */
            return true
                && layer.drawOrder >= ApparelLayerDefOf.Shell.drawOrder
                && layer != ApparelLayerDefOf.Belt
                && !(layer.GetModExtension<ApparelLayerExtension>()?.IsHeadwear ?? false);
            // Log.Message(record.sourceApparel.Label + $" Layer: {layer.defName} IsVisibleLayer: {result}  shellRenderedBehindHead: {!record.sourceApparel.def.apparel.shellRenderedBehindHead}");
            // return result;
        }
    }

    //[HarmonyPatch(typeof(PawnRenderer), "DrawHeadHair")]
    //internal static class Harmony_PawnRenderer_DrawHeadHair
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

    //    /*
    //     * For VFE vikings compatiblity 
    //     * Required for better compatiblity 
    //     */
    //    [HarmonyPriority(Priority.Last)]
    //    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //    {
    //        foreach (var code in instructions)
    //        {
    //            {   /* 
    //                 * 1. Insert calls for head renderer
    //                 * 
    //                 * Look for Ldloc.s 14 (only one in the method), VFE vikings modify the IL just before, so it is not easy to contextualise. If it 
    //                 * breaks make sure to check compat with Alien Races & VFE-Vikings/Beards
    //                 */
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
    //                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Harmony_PawnRenderer_DrawHeadHair), nameof(DrawHeadApparel)));

    //                    yield return code;

    //                    continue;
    //                }

    //            }
    //            yield return code;
    //        }
    //    }
    //}

    //[HarmonyPatch(typeof(PawnRenderer), "DrawEquipmentAiming")]
    //internal static class Harmony_PawnRenderer_DrawEquipmentAiming
    //{
    //    public static Rot4 south = Rot4.South;


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

    //    /*
    //     * This replace the last DrawMesh in 
    //     */
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
