using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AOC17CE
{
    // Character Editor assumes a Graphic_Random declaration also yields an
    // unwrapped Graphic_Random. A2E inherits ground rotation from BaseWeapon,
    // so GraphicData.Graphic actually returns Graphic_RandomRotated instead.
    // Use A2E's deliberately specified UI icon for this definition-only preview.
    [HarmonyPatch]
    public static class A2ECharacterEditorPreview
    {
        static bool Prepare() => AccessTools.TypeByName("CharacterEditor.TextureTool") != null;

        static MethodBase TargetMethod() => AccessTools.Method(
            AccessTools.TypeByName("CharacterEditor.TextureTool"), "GetTexture");

        public static bool Prefix(ThingDef __0, ref Texture2D __result)
        {
            if (__0?.defName != "AOC_A2E")
            {
                return true;
            }

            __result = __0.uiIcon;
            return false;
        }
    }
}
