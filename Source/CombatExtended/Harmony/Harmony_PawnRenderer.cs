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

    internal static class Harmony_PawnRenderer
    {
        /*
         * 
         * Please remember to:
         *          - SYNC these with vanilla PawnRenderer constants
         *          - CHECK if any names changed.
         */

        private const float YOffsetBehind = 0.0028957527f;

        private const float YOffsetHead = 0.023166021f;

        private const float YOffsetOnHead = 0.03185328f;

        private const float YOffsetPostHead = 0.03367347f;

        private const float YOffsetIntervalClothes = 0.0028957527f;

        /*
         * 
         * Check this patch if:
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
         * Should render just after vanilla tattoos. Used to render headgrear with the CE apparel extension
         * 
         * - Patch Harmony_PawnRenderer_ShellFullyCoversHead
         * 
         * Should Allow headgear to render since most CE gear has full head coverage.
         */
        [HarmonyPatch(typeof(PawnRenderer), "DrawBodyApparel")]
        private static class Harmony_PawnRenderer_DrawBodyApparel
        {
            private static MethodBase mDrawMeshNowOrLater = AccessTools.Method(typeof(GenDraw), nameof(GenDraw.DrawMeshNowOrLater), parameters: new[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(bool) });

            private static FieldInfo fShell = AccessTools.Field(typeof(ApparelLayerDefOf), nameof(ApparelLayerDefOf.Shell));

            private static bool IsVisibleLayer(ApparelLayerDef def)
            {
                // If it's invisible skip everything
                if (!def.IsVisibleLayer())
                    return false;
                // Enable toggling backpacks and tactical vests rendering             
                if (!Controller.settings.ShowBackpacks && def == CE_ApparelLayerDefOf.Backpack)
                    return false;
                if (!Controller.settings.ShowTacticalVests && def == CE_ApparelLayerDefOf.Webbing)
                    return false;
                return true;
            }

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
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_PawnRenderer_DrawBodyApparel), nameof(Harmony_PawnRenderer_DrawBodyApparel.IsVisibleLayer), parameters: new[] { typeof(ApparelLayerDef) }));
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
        }

        [HarmonyPatch(typeof(PawnRenderer), "DrawHeadHair")]
        private static class Harmony_PawnRenderer_DrawHeadHair
        {
            private static Rot4 north = Rot4.North;

            private static MethodBase mOverrideMaterialIfNeeded = AccessTools.Method(typeof(PawnRenderer), "OverrideMaterialIfNeeded");

            private static void DrawHeadApparel(PawnRenderer renderer, Pawn pawn, Vector3 rootLoc, Vector3 headLoc, Vector3 headOffset, Rot4 bodyFacing, Quaternion quaternion, PawnRenderFlags flags, ref bool hideHair)
            {
                List<ApparelGraphicRecord> apparelGraphics = renderer.graphics.apparelGraphics;
                Mesh mesh = null;
                Material apparelMat;

                // This will limit us to only 32 layers of headgear
                float interval = YOffsetIntervalClothes / 32;

                Vector3 headwearPos = headLoc;

                for (int i = 0; i < apparelGraphics.Count; i++)
                {
                    ApparelGraphicRecord apparelRecord = apparelGraphics[i];

                    if (apparelRecord.sourceApparel.def.apparel.LastLayer.GetModExtension<ApparelLayerExtension>()?.IsHeadwear ?? false)
                    {
                        mesh = mesh ?? renderer.graphics.HairMeshSet.MeshAt(bodyFacing);
                        apparelMat = GetMaterial(renderer, pawn, apparelRecord, bodyFacing, flags);

                        if (apparelRecord.sourceApparel.def.apparel.hatRenderedFrontOfFace)
                        {
                            Vector3 maskLoc = rootLoc + headOffset;
                            maskLoc.y += !(bodyFacing == north) ? YOffsetPostHead : YOffsetBehind;
                            GenDraw.DrawMeshNowOrLater(mesh, maskLoc, quaternion, apparelMat, flags.FlagSet(PawnRenderFlags.DrawNow));
                        }
                        else
                        {
                            hideHair = apparelRecord.sourceApparel?.def?.GetModExtension<ApperalRenderingExtension>()?.HideHair ?? true;
                            headwearPos.y += interval;
                            GenDraw.DrawMeshNowOrLater(mesh, headwearPos, quaternion, apparelMat, flags.FlagSet(PawnRenderFlags.DrawNow));
                        }
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Material GetMaterial(PawnRenderer renderer, Pawn pawn, ApparelGraphicRecord record, Rot4 bodyFacing, PawnRenderFlags flags)
            {
                Material mat = record.graphic.MatAt(bodyFacing);
                if (flags.FlagSet(PawnRenderFlags.Cache)) return mat;

                return (Material)mOverrideMaterialIfNeeded.Invoke(renderer, new object[] { mat, pawn, flags.FlagSet(PawnRenderFlags.Portrait) });
            }

            /// <summary>
            /// Name of the compiler generated class containing from PawnRenderer.DrawHeadHair()
            /// 0. headfacing
            /// 1. bodyfacing
            /// 2. onheadloc
            /// 3. quat
            /// 3. flags
            /// 4. rootloc
            /// 5. headoffset
            /// </summary>
            //private const string displayClassName = "DisplayClass39";

            /*
             * For VFE vikings compatiblity 
             * Required for better compatiblity 
             */
            [HarmonyPriority(Priority.Last)]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                Type displayType = typeof(PawnRenderer).GetNestedTypes(AccessTools.all).First();
                foreach (var code in instructions)
                {
                    /* 
                     * 1. Insert calls for head renderer
                     * 
                     * Ldloc_2 is the bool used to controll the rendering of headstumps
                     *  
                     * Look for Ldloc_2 (only one in the method), VFE vikings modify the IL just before, so it is not easy to contextualise. If it 
                     * breaks make sure to check compat with Alien Races & VFE-Vikings/Beards
                     */

                    if (code.opcode == OpCodes.Ldloc_2)
                    {
                        // Insert new calls for headgear renderer
                        yield return new CodeInstruction(OpCodes.Ldarg_0) { labels = code.labels }; // PawnRenderer renderer

                        yield return new CodeInstruction(OpCodes.Ldarg_0); // render.pawn
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PawnRenderer), "pawn"));

                        yield return new CodeInstruction(OpCodes.Ldloc_0); // Vector3 rootLoc
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(displayType, "rootLoc"));

                        yield return new CodeInstruction(OpCodes.Ldloc_0); // Vector3 onHeadLoc
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(displayType, "onHeadLoc"));

                        yield return new CodeInstruction(OpCodes.Ldloc_0); // Vector3 headOffset
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(displayType, "headOffset"));

                        yield return new CodeInstruction(OpCodes.Ldloc_0); // Rot4 bodyFacing
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(displayType, "bodyFacing"));

                        yield return new CodeInstruction(OpCodes.Ldloc_0); // Quaternion quat
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(displayType, "quat"));

                        yield return new CodeInstruction(OpCodes.Ldloc_0); // PawnRenderFlags flags
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(displayType, "flags"));

                        yield return new CodeInstruction(OpCodes.Ldloca_S, 2);  // ref bool hideHair                        
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_PawnRenderer_DrawHeadHair), nameof(DrawHeadApparel)));
                        code.labels = new List<Label>();
                        Log.Message("Patched hair");
                    }
                    yield return code;
                }
            }
        }

        [HarmonyPatch(typeof(PawnRenderer), "DrawEquipmentAiming")]
        internal static class Harmony_PawnRenderer_DrawEquipmentAiming
        {
            public static Rot4 south = Rot4.South;


            private static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material mat, int layer, Thing eq, float aimAngle)
            {
                GunDrawExtension drawData = eq.def.GetModExtension<GunDrawExtension>() ?? new GunDrawExtension();
                Matrix4x4 matrix = new Matrix4x4();
                Vector3 scale = new Vector3(drawData.DrawSize.x, 1, drawData.DrawSize.y);
                Vector3 posVec = new Vector3(drawData.DrawOffset.x, 0, drawData.DrawOffset.y);

                if (aimAngle > 200 && aimAngle < 340)
                    posVec.x *= -1;

                matrix.SetTRS(position + posVec.RotatedBy(rotation.eulerAngles.y), rotation, scale);
                Graphics.DrawMesh(mesh, matrix, mat, layer);
            }

            /*
             * This replace the last DrawMesh in 
             */
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                codes[codes.Count - 2].operand =
                    AccessTools.Method(typeof(Harmony_PawnRenderer_DrawEquipmentAiming), nameof(DrawMesh));
                codes.InsertRange(codes.Count - 2, new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldarg_3)
                });
                return codes;
            }

            internal static void Prefix(PawnRenderer __instance, Pawn ___pawn, ref Vector3 drawLoc)
            {
                if (___pawn.Rotation == south)
                {
                    drawLoc.y++;
                }
            }
        }
    }
}
