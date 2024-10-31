using Verse;

namespace CombatExtended
{
    public class PawnRenderNodeWorker_Backpack : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!Controller.settings.ShowBackpacks)
            {
                return false;
            }

            return base.CanDrawNow(node, parms);
        }
    }
}
