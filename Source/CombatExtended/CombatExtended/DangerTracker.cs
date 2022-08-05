using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class DangerTracker : MapComponent
    {
        //private const int DANGER_TICKS = 450;
        //private const int DANGER_TICKS_BULLET_STEP = 200;
        private const int DANGER_TICKS_SMOKE_STEP = 150;
        public const int DANGER_TICKS_MAX = 300; // 1s in real life = 60 ticks in game;
        //private const float DANGER_BULLET_MAX_DIST = 20f;

        private const float WEIGHTS_DIG = 0.8f;
        private const float WEIGHTS_COL = 0.5f;
        private const float WEIGHTSSUM = WEIGHTS_DIG * 4f + WEIGHTS_COL * 4f + 1f;

        private static readonly IntVec3[] AdjCells;
        private static readonly float[] AdjWeights;

        static DangerTracker()
        {
            AdjCells = new IntVec3[9];
            AdjWeights = new float[9];

            AdjCells[0] = new IntVec3(0, 0, 0);
            AdjWeights[0] = 1.0f;

            AdjCells[1] = new IntVec3(1, 0, 1);
            AdjWeights[1] = WEIGHTS_COL;
            AdjCells[2] = new IntVec3(-1, 0, 1);
            AdjWeights[2] = WEIGHTS_COL;
            AdjCells[3] = new IntVec3(1, 0, -1);
            AdjWeights[3] = WEIGHTS_COL;
            AdjCells[4] = new IntVec3(-1, 0, -1);
            AdjWeights[4] = WEIGHTS_COL;

            AdjCells[5] = new IntVec3(1, 0, 0);
            AdjWeights[5] = WEIGHTS_DIG;
            AdjCells[6] = new IntVec3(-1, 0, 0);
            AdjWeights[6] = WEIGHTS_DIG;
            AdjCells[7] = new IntVec3(0, 0, 1);
            AdjWeights[7] = WEIGHTS_DIG;
            AdjCells[8] = new IntVec3(0, 0, -1);
            AdjWeights[8] = WEIGHTS_DIG;
        }

        private int[] dangerArray;
        public DangerTracker(Map map) : base(map)
        {
            dangerArray = new int[map.cellIndices.NumGridCells];
        }

        public void Notify_BulletAt(IntVec3 pos, float dangerAmount)
        {
            for (int i = 0; i < 9; i++)
            {
                IntVec3 cell = pos + AdjCells[i];
                if (cell.InBounds(map))
                    IncreaseAt(cell, (int)Mathf.Ceil(AdjWeights[i] * dangerAmount));
            }
            if (Controller.settings.DebugDisplayDangerBuildup) FlashCells(pos);
        }

        public void Notify_DangerRadiusAt(IntVec3 pos, float radius, float dangerAmount)
        {
            //var radiusSqrd = radius * radius;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pos, radius, true))
            {
                if (!cell.InBounds(map) && !GenSight.LineOfSight(pos, cell, this.map))
                    continue;
                //dangerAmount *= Mathf.Clamp01(1 - ((pos.ToVector3() - cell.ToVector3()).sqrMagnitude / radiusSqrd));
                IncreaseAt(cell, (int)Mathf.Ceil(dangerAmount));

                if (Controller.settings.DebugDisplayDangerBuildup)
                {
                    float value = DangerAt(cell);
                    if (value > 0f) map.debugDrawer.FlashCell(cell, value, $"{value}");
                }
            }
        }

        public void Notify_SmokeAt(IntVec3 pos)
        {
            for (int i = 0; i < 9; i++)
            {
                IntVec3 cell = pos + AdjCells[i];
                if (cell.InBounds(map))
                    IncreaseAt(cell, (int)(DANGER_TICKS_SMOKE_STEP * AdjWeights[i]));
            }
            if (Controller.settings.DebugDisplayDangerBuildup) FlashCells(pos);
        }

        public float DangerAt(IntVec3 pos)
        {
            if (pos.InBounds(map)) return (float)Mathf.Clamp(dangerArray[map.cellIndices.CellToIndex(pos)] - GenTicks.TicksGame, 0, DANGER_TICKS_MAX) / (float)DANGER_TICKS_MAX;
            return 0f;
        }

        public float DangerAt(int index)
        {
            return this.DangerAt(map.cellIndices.IndexToCell(index));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            List<int> dangerList = dangerArray.ToList();
            Scribe_Collections.Look(ref dangerList, "dangerList", LookMode.Value);
            if (dangerList == null || dangerList.Count != map.cellIndices.NumGridCells)
                dangerArray = new int[map.cellIndices.NumGridCells];
        }

        private void FlashCells(IntVec3 pos)
        {
            for (int i = 0; i < 9; i++)
            {
                IntVec3 cell = pos + AdjCells[i];
                if (cell.InBounds(map))
                {
                    float value = DangerAt(cell);
                    if (value > 0f) map.debugDrawer.FlashCell(cell, value, $"{value}");
                }
            }
        }

        private void IncreaseAt(IntVec3 pos, int amount)
        {
            int index = map.cellIndices.CellToIndex(pos);
            int value = dangerArray[index];
            int ticks = GenTicks.TicksGame;
            dangerArray[index] = Mathf.Clamp(value + amount, ticks + amount, ticks + DANGER_TICKS_MAX);
        }
    }
}
