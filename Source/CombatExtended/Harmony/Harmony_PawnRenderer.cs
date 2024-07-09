using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE
{

    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming))]
    internal static class Harmony_PawnRenderer_DrawEquipmentAiming
    {
        public static Rot4 south = Rot4.South;

        private static Thing equipment;

        private static Vector3 recoilOffset = new Vector3();

        private static float muzzleJump = 0;

        private static Vector3 casingDrawPos;

        private static readonly Matrix4x4 TBot5 = Matrix4x4.Translate(new Vector3(0, -0.006f, 0));

        private static readonly Matrix4x4 TBot3 = Matrix4x4.Translate(new Vector3(0, -0.004f, 0));

        public static void Prefix(Thing eq, Vector3 drawLoc)
        {
            equipment = eq;
            casingDrawPos = drawLoc;
        }

        private static void RecoilCE(Thing eq, Vector3 position, float aimAngle, float num, CompEquippable compEquippable)
        {
            if (Controller.settings.RecoilAnim && compEquippable.PrimaryVerb.verbProps is VerbPropertiesCE)
            {
                CE_Utility.Recoil(eq.def, compEquippable.PrimaryVerb, out var drawOffset, out var angleOffset, aimAngle, true);
                recoilOffset = drawOffset;
                muzzleJump = angleOffset;
            }
        }

        private static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material mat, int layer, Thing eq, Vector3 position, float aimAngle)
        {
            GunDrawExtension drawData = eq.def.GetModExtension<GunDrawExtension>() ?? new GunDrawExtension() { DrawSize = eq.def.graphicData.drawSize };
            if (drawData.DrawSize == Vector2.one) { drawData.DrawSize = eq.def.graphicData.drawSize; }
            Vector3 scale = new Vector3(drawData.DrawSize.x, 1, drawData.DrawSize.y);
            Vector3 posVec = new Vector3(drawData.DrawOffset.x, 0, drawData.DrawOffset.y);
            Vector3 casingOffset = new Vector3(drawData.CasingOffset.x, 0, drawData.CasingOffset.y);
            if (aimAngle > 200 && aimAngle < 340)
            {
                posVec.x *= -1;
                muzzleJump = -muzzleJump;
                casingOffset.x *= -1;
            }
            matrix.SetTRS(position + posVec.RotatedBy(matrix.rotation.eulerAngles.y) + recoilOffset, Quaternion.AngleAxis(matrix.rotation.eulerAngles.y + muzzleJump, Vector3.up), scale);
            CompEquippable compEquippable = eq.TryGetComp<CompEquippable>();
            if (compEquippable != null && compEquippable.PrimaryVerb is Verb_ShootCE verbCE)
            {
                verbCE.drawPos = casingDrawPos + (casingOffset + posVec).RotatedBy(matrix.rotation.eulerAngles.y);
            }
            if (eq is WeaponPlatform platform)
            {
                platform.DrawPlatform(matrix, mesh == MeshPool.plane10Flip, layer);
            }
            else
            {
                Graphics.DrawMesh(mesh, matrix, mat, layer);
            }
        }


        /*
         * This replace the last DrawMesh in
         */
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var recoil_opcodes = new CodeInstruction[]
            {
                                    new CodeInstruction(OpCodes.Ldarg_0),
                                    new CodeInstruction(OpCodes.Ldarg_1),
                                    new CodeInstruction(OpCodes.Ldarg_2),
                                    new CodeInstruction(OpCodes.Ldloc_1),
                                    new CodeInstruction(OpCodes.Ldloc_2),
                                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_PawnRenderer_DrawEquipmentAiming), nameof(RecoilCE)))
            };
            bool foundRecoil = false;
            int index = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                if (foundRecoil && code.opcode == OpCodes.Stloc_1)
                {
                    index = i + 1;
                    break;
                }
                else if (code.opcode == OpCodes.Call && ReferenceEquals(code.operand, typeof(EquipmentUtility).GetMethod(nameof(EquipmentUtility.Recoil))))
                {
                    foundRecoil = true;
                }
            }
            codes.InsertRange(index, recoil_opcodes);
            codes[codes.Count - 2].operand =
                AccessTools.Method(typeof(Harmony_PawnRenderer_DrawEquipmentAiming), nameof(DrawMesh));
            codes.InsertRange(codes.Count - 2, new[]
            {
                                    new CodeInstruction(OpCodes.Ldarg_0),
                                    new CodeInstruction(OpCodes.Ldarg_1),
                                    new CodeInstruction(OpCodes.Ldarg_2)
                                });
            return codes;
        }
    }
}
