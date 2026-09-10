using UnityEngine;
using Verse;
namespace CombatExtended.Compatibility.VFES
{
    // Invisible, non-interactable thing that exists purely to flip a cell's
    // standability/walkability when a concealed structure is deployed. It is
    // never drawn and never selectable; CompConcealedCE owns its lifecycle
    // (spawn on deploy, destroy on submerge). The def decides whether the
    // deployed cell is a soft obstacle (PassThroughOnly + pathCost) or a hard
    // wall (Impassable) -- see the dummy ThingDefs in the VFE security patch.
    public class ConcealedDummy : Thing
    {
        public override Graphic Graphic => null;

        protected override void DrawAt(Vector3 drawLoc, bool flip = false) { }
    }
}
