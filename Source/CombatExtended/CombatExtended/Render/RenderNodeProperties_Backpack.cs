using RimWorld;
using Verse;

namespace CombatExtended
{
    public class RenderNodeProperties_Backpack : PawnRenderNodeProperties
    {
        public RenderNodeProperties_Backpack()
        {
            nodeClass = typeof(PawnRenderNode_Apparel);
            workerClass = typeof(PawnRenderNodeWorker_Backpack);
        }
        public WornGraphicData offsetData;
    }
}
