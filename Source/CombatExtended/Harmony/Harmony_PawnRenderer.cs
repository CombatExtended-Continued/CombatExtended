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

        private static Vector3 Vec2ToVec3(Vector2 vector)
        {
            return new Vector3(vector.x, 0, vector.y);
        }

        /*
         * Compatiblity stuff IMPORTANT
         * 
         * These are patched by compatibility patches so we can have custom drawsize, drawoffsets for mods like Alien races
         * IF YOU ARE A MODDER PLEASE READ THIS...
         * 
         * In order to make compatiblity with CE easier, we've implemented several empty functions that return a default value. 
         * These are to be patched by either other mods or by CE it self inorder to make the process of changing simple things in CE a bit simpler.
         * 
         * <==================================== Example of adding support for customHeadDrawSize form HAR in CE ====================================>               
         */

        /// <summary>
        /// Intended to allow an easy point that allow other mods or CE to patch the rendering in runtime
        /// </summary>                
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Mesh GetHeadMesh(PawnRenderFlags renderFlags, Pawn pawn, Rot4 headFacing, PawnGraphicSet graphics)
        {
            return null;
        }

        /// <summary>
        /// Intended to allow an easy point that allow other mods or CE to patch the rendering in runtime
        /// </summary>        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Vector2 GetHeadCustomSize(ThingDef def)
        {
            return Vector2.one;
        }

        /// <summary>
        /// Intended to allow an easy point that allow other mods or CE to patch the rendering in runtime
        /// </summary>        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Vector2 GetHeadCustomOffset(ThingDef def)
        {
            return Vector2.zero;
        }

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
                // Moved to since backpacks use 
                if (def == CE_ApparelLayerDefOf.Backpack)
                    return false;
                // Enable toggling webbing rendering             
                if (def == CE_ApparelLayerDefOf.Webbing && !Controller.settings.ShowTacticalVests)
                    return false;
                return true;
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                List<CodeInstruction> codes = instructions.ToList();
                Label l1 = generator.DefineLabel();
                Label l2 = generator.DefineLabel();
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
                    /*
                     * Find and add a condition to utilityLoc to make backpacks render behind hair
                     */
                    if (code.opcode == OpCodes.Ldarg_2)
                    {
                        /* 
                         * Load apparelGraphicRecord from for(int i = 0....) { ApparelGraphicRecord apparelGraphicRecord = apparelGraphics[i];
                         * From the start of the function
                         */
                        yield return new CodeInstruction(OpCodes.Ldloc_3).MoveLabelsFrom(code);
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ApparelGraphicRecord), nameof(ApparelGraphicRecord.sourceApparel)));
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), nameof(Thing.def)));
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef), nameof(ThingDef.apparel)));
                        yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ApparelProperties), nameof(ApparelProperties.LastLayer)));                // Load current apparel last layer

                        yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(CE_ApparelLayerDefOf), nameof(CE_ApparelLayerDefOf.Backpack)));              // Load backpack

                        yield return new CodeInstruction(OpCodes.Bne_Un_S, l1); // Compare value

                        yield return new CodeInstruction(OpCodes.Ldarg_1);      // shellloc
                        yield return new CodeInstruction(OpCodes.Ldarg_S, 5);   // bodyFacing
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_PawnRenderer_DrawBodyApparel), nameof(GetBackpackOffset)));                          // shellloc
                        yield return new CodeInstruction(OpCodes.Br_S, l2);     // continue;

                        code.labels = new List<Label>() { l1 };
                        yield return code;
                        codes[i + 1].labels.Add(l2);
                        continue;
                    }
                    yield return code;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector3 GetBackpackOffset(Vector3 vector, Rot4 bodyFacing)
            {
                /*
                 * Need to subract  0.023166021f since if we don't backpacks will render 
                 * infront of pawns when facing the player (south)
                 */
                if (bodyFacing == Rot4.South)
                    vector.y -= 0.023166021f;
                return vector;
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

            private static void DrawHeadApparel(PawnRenderer renderer, Pawn pawn, Vector3 rootLoc, Vector3 headLoc, Vector3 headOffset, Rot4 bodyFacing, Quaternion quaternion, PawnRenderFlags flags, Rot4 headFacing, ref bool hideHair)
            {
                if (flags.FlagSet(PawnRenderFlags.Portrait) && Prefs.HatsOnlyOnMap)
                    return;
                if (!flags.FlagSet(PawnRenderFlags.Headgear))
                    return;
                List<ApparelGraphicRecord> apparelGraphics = renderer.graphics.apparelGraphics;

                // This will limit us to only 32 layers of headgear
                float interval = YOffsetIntervalClothes / 32;

                Vector3 customScale = Vec2ToVec3(GetHeadCustomSize(pawn.def));
                Vector3 headwearPos = headLoc + Vec2ToVec3(GetHeadCustomOffset(pawn.def));

                Mesh mesh = GetHeadMesh(flags, pawn, headFacing, renderer.graphics) ?? renderer.graphics.HairMeshSet.MeshAt(bodyFacing);

                for (int i = 0; i < apparelGraphics.Count; i++)
                {
                    ApparelGraphicRecord apparelRecord = apparelGraphics[i];
                    if (apparelRecord.sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.Overhead && !apparelRecord.sourceApparel.def.apparel.hatRenderedFrontOfFace)
                    {
                        hideHair = apparelRecord.sourceApparel?.def?.GetModExtension<ApperalRenderingExtension>()?.HideHair ?? true;
                    }
                    else if (apparelRecord.sourceApparel.def.apparel.LastLayer.GetModExtension<ApparelLayerExtension>()?.IsHeadwear ?? false)
                    {
                        Material apparelMat = GetMaterial(renderer, pawn, apparelRecord, bodyFacing, flags);

                        if (apparelRecord.sourceApparel.def.apparel.hatRenderedFrontOfFace)
                        {
                            Matrix4x4 matrix = new Matrix4x4();
                            Vector3 maskLoc = rootLoc + headOffset;
                            maskLoc.y += !(bodyFacing == north) ? YOffsetPostHead : YOffsetBehind;
                            matrix.SetTRS(maskLoc, quaternion, customScale);
                            GenDraw.DrawMeshNowOrLater(mesh, matrix, apparelMat, flags.FlagSet(PawnRenderFlags.DrawNow));
                        }
                        else
                        {
                            Matrix4x4 matrix = new Matrix4x4();
                            hideHair = apparelRecord.sourceApparel?.def?.GetModExtension<ApperalRenderingExtension>()?.HideHair ?? true;
                            headwearPos.y += interval;
                            matrix.SetTRS(headwearPos, quaternion, customScale);
                            GenDraw.DrawMeshNowOrLater(mesh, matrix, apparelMat, flags.FlagSet(PawnRenderFlags.DrawNow));
                        }
                    }
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
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
                var codes = instructions.ToList();
                var displayType = typeof(PawnRenderer).GetNestedTypes(AccessTools.all).First();
                //
                //var l1 = generator.DefineLabel();
                for (int i = 0; i < codes.Count; i++)
                {
                    var code = codes[i];
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
                        // Insert headgear global render check
                        //yield return new CodeInstruction(OpCodes.Ldloc_S, 4) { labels = code.labels }; // check if we should render head gear
                        //yield return new CodeInstruction(OpCodes.Brfalse_S, l1);

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

                        yield return new CodeInstruction(OpCodes.Ldloc_0); // PawnRenderFlags flags
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(displayType, "headFacing"));

                        yield return new CodeInstruction(OpCodes.Ldloca_S, 2);  // ref bool hideHair

                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_PawnRenderer_DrawHeadHair), nameof(DrawHeadApparel)));
                        code.labels = new List<Label>();
                    }
                    yield return code;
                }
            }
        }

        [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.BaseHeadOffsetAt))]
        internal static class Harmony_PawnRenderer_BaseHeadOffsetAt
        {
            public static void Postfix(PawnRenderer __instance, ref Vector3 __result, Rot4 rotation)
            {
                if (Controller.settings.CrouchingAnimation && __instance.pawn.IsCrouching())
                    Offset(rotation, ref __result, 1.0f);
            }

            private static void Offset(Rot4 rotation, ref Vector3 offset, float multiplier = 1f)
            {
                if (rotation == Rot4.East)
                {
                    offset.x += 0.09f * multiplier;
                    offset.z -= 0.098f * multiplier;
                }
                else if (rotation == Rot4.West)
                {
                    offset.x -= 0.09f * multiplier;
                    offset.z -= 0.098f * multiplier;
                }
                else if (rotation == Rot4.North)
                {
                    offset.z -= 0.1f * multiplier;
                }
                else if (rotation == Rot4.South)
                {
                    offset.z -= 0.07f * multiplier;
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
