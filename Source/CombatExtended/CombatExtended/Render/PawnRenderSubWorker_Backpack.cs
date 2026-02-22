using Verse;

namespace CombatExtended;

public class PawnRenderSubWorker_Backpack : PawnRenderSubWorker
{
    public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
    {
        return Controller.settings.ShowBackpacks;
    }
}
