using System;
using Verse;

namespace CombatExtended;

[Obsolete]
public class PawnRenderNodeWorker_Webbing : PawnRenderNodeWorker_Apparel_Body
{
    public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
    {
        if (!Controller.settings.ShowTacticalVests)
        {
            return false;
        }

        return base.CanDrawNow(node, parms);
    }
}
