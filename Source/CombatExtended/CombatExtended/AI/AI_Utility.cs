using System;
using System.Collections.Generic;
using System.Linq;
using CombatExtended.Utilities;
using RimWorld;
using RimWorld.BaseGen;
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
                if (cover != null && cover.def.Fillage == FillCategory.Partial)
                    return true;
            }
            return false;
        }


        public static void TrySetFireMode(this CompFireModes fireModes, FireMode mode)
        {
            if (fireModes.CurrentFireMode == mode && fireModes.AvailableFireModes.Contains(mode))
                return;
            int m = (int)mode;
            int i = 1;
            while (i < 3)
            {
                mode = (FireMode)((m + i) % 3);
                i++;
                if (fireModes.AvailableFireModes.Contains(mode))
                {
                    fireModes.CurrentFireMode = mode;
                    break;
                }
            }
        }

        public static void TrySetAimMode(this CompFireModes fireModes, AimMode mode)
        {
            if (fireModes.CurrentAimMode == mode && fireModes.AvailableAimModes.Contains(mode))
                return;
            int m = (int)mode;
            int i = 1;
            while (i < 3)
            {
                mode = (AimMode)((m + i) % 3);
                i++;
                if (fireModes.AvailableAimModes.Contains(mode))
                {
                    fireModes.CurrentAimMode = mode;
                    break;
                }
            }

        }

        public static bool EdgingCloser(this Pawn pawn, Pawn other)
        {
            float curDist = other.Position.DistanceTo(pawn.Position);
            if (other.pather.moving && curDist > other.pather.destination.Cell.DistanceTo(pawn.Position))
                return true;
            if (pawn.pather.moving && curDist > pawn.pather.destination.Cell.DistanceTo(other.Position))
                return true;
            return false;
        }

        public static bool EdgingAway(this Pawn pawn, Pawn other)
        {
            float curDist = other.Position.DistanceTo(pawn.Position);
            if (other.pather.moving && curDist < other.pather.destination.Cell.DistanceTo(pawn.Position))
                return true;
            if (pawn.pather.moving && curDist < pawn.pather.destination.Cell.DistanceTo(other.Position))
                return true;
            return false;
        }

        public static bool VisibilityGoodAt(this Map map, Pawn shooter, IntVec3 target, float nightVisionEfficiency = -1)
        {
            LightingTracker tracker = map.GetLightingTracker();
            if (!tracker.IsNight)
                return true;
            if (target.DistanceTo(shooter.Position) < 15)
                return true;
            if (tracker.CombatGlowAtFor(shooter.Position, target) >= 0.5f)
                return true;
            if ((nightVisionEfficiency == -1 ? shooter.GetStatValue(CE_StatDefOf.NightVisionEfficiency) : nightVisionEfficiency) > 0.6)
                return true;
            if (target.Roofed(map))
                return true;
            return false;
        }
    }
}
