using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.Loader {
    public interface ISettingsCE {
        public void DoWindowContents(Rect canvas, ref int offset);
    }
}
