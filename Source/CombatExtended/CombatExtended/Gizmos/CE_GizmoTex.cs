using Verse;
using UnityEngine;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public static class CE_GizmoTex
    {
        public static Texture2D BGTex = ContentFinder<Texture2D>.Get("UI/Widgets/DesButBG", true);
    }
}
