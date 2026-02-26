using Verse;

namespace CombatExtended;

public class PawnRenderSubWorker_Webbing : PawnRenderSubWorker
{
    public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
    {
        return Controller.settings.ShowTacticalVests;
    }
}
