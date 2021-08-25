using Verse;
using UnityEngine;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public static class CE_GizmoTex
    {
        public static Texture2D BGTex = ContentFinder<Texture2D>.Get("UI/Widgets/DesButBG", true);
        public static Texture2D ReloadTex = ContentFinder<Texture2D>.Get("UI/Buttons/Reload", true);
        public static Texture2D UnloadTex = ContentFinder<Texture2D>.Get("UI/Buttons/Unload", true);
    }
}
