using System.Collections.Generic;
using Verse;

namespace CombatExtended
{
    public class CompPawnGizmo : ThingComp
    {
        bool duplicate = false;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            foreach (var comp in parent.comps)
            {
                if (comp is CompPawnGizmo && comp != this)
                {
                    duplicate = true;
                    Log.ErrorOnce($"{parent.def.defName} has multiple CompPawnGizmo, duplicates has been deactivated. Please report this to the patch provider of {parent.def.modContentPack.Name} or CE team if the patch is integrated in CE.", parent.def.GetHashCode());
                }
            }
        }


        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!duplicate)
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
            };
        }
    }
}
