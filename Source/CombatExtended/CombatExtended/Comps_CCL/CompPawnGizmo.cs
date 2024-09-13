using System.Collections.Generic;
using Verse;

namespace CombatExtended
{
    public class CompPawnGizmo : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var pawn = parent as Pawn;
            var equip = pawn != null
                        ? pawn.equipment.Primary
                        : null;

            if (
                (equip != null) &&
                (!equip.AllComps.NullOrEmpty())
            )
            {
                foreach (var comp in equip.AllComps)
                {
                    var gizmoGiver = comp as CompRangedGizmoGiver;
                    if (
                        (gizmoGiver != null) &&
                        (gizmoGiver.isRangedGiver)
                    )
                    {
                        foreach (var gizmo in gizmoGiver.CompGetGizmosExtra())
                        {
                            yield return gizmo;
                        }
                    }
                }
            }
        }

    }

}
