using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class Smoke : Gas
    {
        private int _ticksUntilMove;
        private const int BaseTicksUntilMove = 40;
        private const int TicksUntilMoveDelta = 20;
        private const float InhalationPerSec = 0.0075f / GenTicks.TicksPerRealSecond;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _ticksUntilMove, "ticksUntilMove");
        }

        private bool CanMoveTo(IntVec3 pos)
        {
            return !pos.Filled(Map) || (pos.GetDoor(Map)?.Open ?? false) || pos.GetFirstThing<Building_Vent>(Map) != null;
        }

        public override void Tick()
        {
            ApplyHediffs();

            _ticksUntilMove--;
            if (!Position.Roofed(Map))
            {
                destroyTick--;
                base.Tick();
                return;
            }

            // Don't decay if there's lots of smoke around
            var region = Position.GetRegion(Map);
            var smokeFillage = region?.ListerThings.ThingsOfDef(CE_ThingDefOf.Gas_BlackSmoke).Count / region?.CellCount ?? 0;
            if (region != null && !Rand.Chance(smokeFillage * 2))
                destroyTick++;

            if (_ticksUntilMove > 0)
            {
                base.Tick();
                return;
            }

            _ticksUntilMove = BaseTicksUntilMove + Rand.RangeInclusive(-TicksUntilMoveDelta, TicksUntilMoveDelta);

            var freeCells = GenAdjFast.AdjacentCells8Way(Position).InRandomOrder().Where(CanMoveTo).ToList();
            var unroofedCell = freeCells.FirstOrFallback(c => !c.Roofed(Map), IntVec3.Invalid);

            // Move towards unroofed cells if possible
            if (unroofedCell.IsValid)
            {
                Position = unroofedCell;
                base.Tick();
                return;
            }

            // Find gas-free tile to move to
            foreach (var cell in freeCells)
            {
                if (cell.GetGas(Map) == null)
                {
                    Position = cell;
                    base.Tick();
                    return;
                }
            }

            base.Tick();
        }

        private void ApplyHediffs()
        {
            var pawns = Position.GetThingList(Map).Where(t => t is Pawn).ToList();
            foreach (var cell in GenAdjFast.AdjacentCells8Way(Position))
            {
                pawns.AddRange(cell.GetThingList(Map).Where(t => t is Pawn));
            }

            foreach (Pawn pawn in pawns)
            {

                var severity = InhalationPerSec * pawn.GetStatValue(CE_StatDefOf.SmokeSensitivity);
                HealthUtility.AdjustSeverity(pawn, CE_HediffDefOf.SmokeInhalation, severity);
            }
        }
    }
}