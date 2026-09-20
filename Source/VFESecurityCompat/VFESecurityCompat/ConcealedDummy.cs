using UnityEngine;
using Verse;
namespace CombatExtended.Compatibility.VFES
{
    // non-interactable thing whose only job is flipping a cell's
    // walkability when the structure's deployed
    public class ConcealedDummy : Thing
    {
        public override Graphic Graphic => null;

        protected override void DrawAt(Vector3 drawLoc, bool flip = false) { }
    }
}
