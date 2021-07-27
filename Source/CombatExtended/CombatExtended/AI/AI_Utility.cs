using System;
using System.Collections.Generic;
using Verse;

namespace CombatExtended.AI
{
    public static class AI_Utility
    {
        private static readonly Dictionary<Pawn, CompTacticalManager> _compTactical = new Dictionary<Pawn, CompTacticalManager>(2048);

        public static CompTacticalManager GetTacticalManager(this Pawn pawn)
        {
            return _compTactical.TryGetValue(pawn, out var comp) ? comp : _compTactical[pawn] = pawn.TryGetComp<CompTacticalManager>();
        }

        public static bool HiddingBehindCover(this Pawn pawn, LocalTargetInfo targetFacing)
        {
            Map map = pawn.Map;
            IntVec3 startPos = pawn.Position;
            IntVec3 endPos = targetFacing.Cell;
            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(startPos, new IntVec3(
                    (int)((startPos.x * 3 + endPos.x) / 4f),
                    (int)((startPos.y * 3 + endPos.y) / 4f),
                    (int)((startPos.z * 3 + endPos.z) / 4f))))
            {
                Thing cover = cell.GetCover(map);
                map.debugDrawer.FlashCell(cell);
                if (cover != null && cover.def.Fillage == FillCategory.Partial)
                    return true;
            }
            return false;
        }
    }
}
