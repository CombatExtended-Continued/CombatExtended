using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended.Detours
{
    [StaticConstructorOnStartup]
    internal class Initializer : ITab
    {
        protected static GameObject iconControllerObject;

        static Initializer()
        {
            iconControllerObject = new GameObject("Combat Realism :: Detour Core Initializer");
            iconControllerObject.AddComponent<InitializerBehaviour>();
            Object.DontDestroyOnLoad(iconControllerObject);
        }

        protected override void FillTab()
        {

        }
    }
}