using System;
using Verse;

namespace CombatExtended;
public class PawnRenderNodeWorker_Backpack : PawnRenderNodeWorker_Apparel_Body
{
    public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
    {
        if (!Controller.settings.ShowBackpacks)
        {
            return false;
        }
        return base.CanDrawNow(node, parms);
    }

    public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
    {
        return parms.facing.AsInt switch
        {
            0 => 93f, // North
            2 => -3f, // South
            _ => 30f
        };
    }
}
