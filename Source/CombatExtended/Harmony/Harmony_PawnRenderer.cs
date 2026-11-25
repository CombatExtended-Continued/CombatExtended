using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE;

[HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming))]
internal static class Harmony_PawnRenderer_DrawEquipmentAiming
{
    private static Vector3 casingDrawPos;

    public static void Prefix(Vector3 drawLoc) => casingDrawPos = drawLoc;

    private static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material mat, int layer, Thing eq, Vector3 position, float aimAngle, CompEquippable equippable)
    {
        var verb = equippable?.PrimaryVerb;
        // Recoil belongs to this draw. A weapon or setting that skips it uses zero offsets.
        Vector3 recoilOffset = Vector3.zero;
        float muzzleJump = 0f;
        if (Controller.settings.RecoilAnim && verb?.verbProps is VerbPropertiesCE)
        {
            CE_Utility.Recoil(eq.def, verb, out recoilOffset, out muzzleJump, aimAngle, true);
        }

        var drawData = eq.def.GetModExtension<GunDrawExtension>();
        var size = drawData?.DrawSize ?? Vector2.one;
        if (size == Vector2.one)
        {
            size = eq.def.graphicData.drawSize;
        }
        var offset = drawData?.DrawOffset ?? Vector2.zero;
        var casing = drawData?.CasingOffset ?? Vector2.zero;
        Vector3 scale = new(size.x, 1, size.y);
        Vector3 posVec = new(offset.x, 0, offset.y);
        Vector3 casingOffset = new(casing.x, 0, casing.y);
        if (aimAngle > 200 && aimAngle < 340)
        {
            posVec.x *= -1;
            muzzleJump = -muzzleJump;
            casingOffset.x *= -1;
        }

        float yAngle = matrix.rotation.eulerAngles.y;
        matrix.SetTRS(position + posVec.RotatedBy(yAngle) + recoilOffset, Quaternion.AngleAxis(yAngle + muzzleJump, Vector3.up), scale);
        if (verb is Verb_ShootCE verbCE)
        {
            verbCE.drawPos = casingDrawPos + (casingOffset + posVec).RotatedBy(yAngle);
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

    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var recoil = AccessTools.Method(typeof(EquipmentUtility), nameof(EquipmentUtility.Recoil),
            [typeof(ThingDef), typeof(Verb_LaunchProjectile), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float)]);
        var drawMesh = AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh),
            [typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int)]);

        // Reuse the component already retrieved by vanilla instead of looking it up again at draw time.
        var recoilPattern = new CodeMatch[]
        {
            new(code => code.IsLdloc()),
            CodeMatch.Calls(AccessTools.PropertyGetter(typeof(CompEquippable), nameof(CompEquippable.AllVerbs))),
            CodeMatch.Calls(AccessTools.Method(typeof(EquipmentUtility), nameof(EquipmentUtility.GetRecoilVerb))),
            new(code => code.opcode == OpCodes.Ldloca || code.opcode == OpCodes.Ldloca_S),
            new(code => code.opcode == OpCodes.Ldloca || code.opcode == OpCodes.Ldloca_S),
            new(code => code.IsLdarg(2)),
            CodeMatch.Calls(recoil),
        };
        var matcher = new CodeMatcher(codes).MatchStartForward(recoilPattern);
        if (matcher.IsInvalid || codes.Count(code => code.Calls(recoil)) != 1)
        {
            Log.Error("Combat Extended :: DrawEquipmentAiming expected one recognizable EquipmentUtility.Recoil sequence. Skipping renderer patch.");
            return codes;
        }
        var componentLoad = matcher.Instruction.Clone();

        matcher.Advance(recoilPattern.Length).MatchStartForward(CodeMatch.Calls(drawMesh));
        if (matcher.IsInvalid || codes.Count(code => code.Calls(drawMesh)) != 1 || matcher.Instruction.blocks.Count != 0)
        {
            Log.Error("Combat Extended :: DrawEquipmentAiming expected one Graphics.DrawMesh(Mesh, Matrix4x4, Material, int) after recoil, outside an exception boundary. Skipping renderer patch.");
            return codes;
        }

        // Branches into the draw call also need CE's arguments. Preserve their labels on the first load.
        int drawIndex = matcher.Pos;
        codes[drawIndex] = CodeInstruction.Call(typeof(Harmony_PawnRenderer_DrawEquipmentAiming), nameof(DrawMesh));
        codes.InsertRange(drawIndex,
        [
            new CodeInstruction(OpCodes.Ldarg_0).WithLabels(matcher.Instruction.labels),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldarg_2),
            componentLoad,
        ]);
        return codes;
    }
}
