using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    class JobGiver_HunkerDown : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            //if (pawn.TryGetComp<CompSuppressable>().isHunkering && pawn.GetPosture() != PawnPosture.Standing)
            //{
            //    return null;
            //}

            if (!pawn.Position.Standable(pawn.Map) && !pawn.Position.ContainsStaticFire(pawn.Map))
            {
                return null;
            }
			
            // UNDONE: Use ThrowMetaIcon or similar attached mote for this. There is a problem with attached motes not being visible though.
            MoteMaker.MakeColonistActionOverlay(pawn, CE_ThingDefOf.Mote_HunkerIcon);
            //MoteMaker.ThrowMetaIcon(pawn.Position, pawn.Map, CE_ThingDefOf.Mote_HunkerIcon);
            return new Job(CE_JobDefOf.HunkerDown, pawn)
            {
//                playerForced = true,
            };
        }
    }
}
