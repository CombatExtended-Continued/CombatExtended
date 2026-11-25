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

    private static void RecoilCE(Thing eq, float aimAngle, CompEquippable compEquippable)
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

        float yAngle = matrix.rotation.eulerAngles.y;
        matrix.SetTRS(position + posVec.RotatedBy(yAngle) + recoilOffset, Quaternion.AngleAxis(yAngle + muzzleJump, Vector3.up), scale);
        CompEquippable compEquippable = eq.TryGetComp<CompEquippable>();
        if (compEquippable != null && compEquippable.PrimaryVerb is Verb_ShootCE verbCE)
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

        // Capture the equipment-component load used to compute vanilla recoil. CE only needs
        // the component, equipment and aim angle, so it can run immediately after Recoil.
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
        int afterRecoil = matcher.Pos + recoilPattern.Length;

        matcher.Advance(recoilPattern.Length).MatchStartForward(CodeMatch.Calls(drawMesh));
        if (matcher.IsInvalid || codes.Count(code => code.Calls(drawMesh)) != 1 || matcher.Instruction.blocks.Count != 0)
        {
            Log.Error("Combat Extended :: DrawEquipmentAiming expected one Graphics.DrawMesh(Mesh, Matrix4x4, Material, int) after recoil, outside an exception boundary. Skipping renderer patch.");
            return codes;
        }

        // Edit the later site first. Branches targeting the draw call must also load CE's
        // three extra arguments. Copy its labels without modifying the incoming instructions.
        int drawIndex = matcher.Pos;
        codes[drawIndex] = CodeInstruction.Call(typeof(Harmony_PawnRenderer_DrawEquipmentAiming), nameof(DrawMesh));
        codes.InsertRange(drawIndex,
        [
            new CodeInstruction(OpCodes.Ldarg_0).WithLabels(matcher.Instruction.labels),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldarg_2),
        ]);
        codes.InsertRange(afterRecoil,
        [
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldarg_2),
            componentLoad,
            CodeInstruction.Call(typeof(Harmony_PawnRenderer_DrawEquipmentAiming), nameof(RecoilCE)),
        ]);
        return codes;
    }
}
