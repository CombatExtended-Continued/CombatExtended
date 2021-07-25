using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class LightingTracker : MapComponent
    {
        private const int FLASHAGE = 500;
        private const float MAX_GLOW_DIFF = 0.60f;
        private const float WEIGHTS_DIG = 0.8f;
        private const float WEIGHTS_COL = 0.5f;
        private const float WEIGHTSSUM = WEIGHTS_DIG * 4f + WEIGHTS_COL * 4f + 1f;

        private static readonly IntVec3[] AdjCells;
        private static readonly float[] AdjWeights;

        static LightingTracker()
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

        [StructLayout(LayoutKind.Sequential, Size = 8)]
        private struct MuzzleRecord : IExposable
        {
            private int createdAt;
            private float intensity;

            public float Intensity
            {
                get => intensity * (1.0f - Mathf.Min((float)GenTicks.TicksGame - createdAt, FLASHAGE) / FLASHAGE);
            }

            public bool Recent
            {
                get => createdAt >= 0 && GenTicks.TicksGame - createdAt <= FLASHAGE;
            }

            public int Age
            {
                get => GenTicks.TicksGame - createdAt;
            }

            public MuzzleRecord(float intensity = 0)
            {
                if (intensity > 1e-2f)
                {
                    this.createdAt = GenTicks.TicksGame;
                    this.intensity = Mathf.Clamp01(intensity);
                }
                else
                {
                    this.createdAt = -1;
                    this.intensity = 0f;
                }
            }

            public static MuzzleRecord operator +(MuzzleRecord first, MuzzleRecord second)
            {
                if (!first.Recent || !second.Recent)
                    return new MuzzleRecord(Mathf.Max(first.intensity, second.intensity, 0));
                int tick = GenTicks.TicksGame;
                float a = 1.0f - (float)Mathf.Min(tick - first.createdAt, FLASHAGE) / FLASHAGE;
                float b = 1.0f - (float)Mathf.Min(tick - second.createdAt, FLASHAGE) / FLASHAGE;
                return new MuzzleRecord(a * first.intensity + b * second.intensity);
            }

            public void ExposeData()
            {
                Scribe_Values.Look(ref createdAt, "createdAt");
                Scribe_Values.Look(ref intensity, "intensity", defaultValue: 0f);

                this.createdAt = intensity != 0 ? GenTicks.TicksGame : int.MinValue;
            }
        }

        private const float MUZZLEFLASH_MEAN = 13f;

        public int NumGridCells
        {
            get
            {
                return map.cellIndices.NumGridCells;
            }
        }

        public bool IsNight
        {
            get
            {
                return map.skyManager.CurSkyGlow < 0.5f;
            }
        }

        private float curSkyGlow = -1;
        public float CurSkyGlow
        {
            get
            {
                if (curSkyGlow == -1)
                {
                    curSkyGlow = map.skyManager.CurSkyGlow;
                }
                return curSkyGlow;
            }
        }

        private MuzzleRecord[] muzzle_grid;

        public LightingTracker(Map map) : base(map)
        {
            _cellIndices = map.cellIndices;

            muzzle_grid = new MuzzleRecord[NumGridCells];
        }

        public override void ExposeData()
        {
            base.ExposeData();
            /*
             * Save the muzzle flash grid
             */
            Expose_MuzzleGrid();
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();
            /*
             * Update CurSkyGlow which is the amount of natural light
             */
            curSkyGlow = map.skyManager.CurSkyGlow;
        }

        /*
         * Debugging vars
         */
        private readonly List<IntVec3> _removal = new List<IntVec3>();
        private readonly HashSet<IntVec3> _recent = new HashSet<IntVec3>();

        public override void MapComponentOnGUI()
        {
            if (Controller.settings.DebuggingMode && Controller.settings.DebugMuzzleFlash)
            {
                _removal.Clear();
                foreach (IntVec3 position in _recent)
                {
                    if (!position.InBounds(map))
                    {
                        _removal.Add(position);
                        continue;
                    }
                    MuzzleRecord record = muzzle_grid[CellToIndex(position)];
                    if (!record.Recent)
                    {
                        _removal.Add(position);
                    }
                    var glow = CombatGlowAt(position);
                    map.debugDrawer.FlashCell(position, glow, glow.ToStringByStyle(ToStringStyle.FloatOne), duration: 15);
                }
                foreach (IntVec3 position in _removal)
                    _recent.Remove(position);
            }
            else if (_recent.Count > 0)
                _recent.Clear();

            base.MapComponentOnGUI();
        }

        /// <summary>
        /// Notify the lighting tracker of shots fired at position. This allows it to create several records at the firing and neighbouring positions.
        /// These records are the "Game" memory of recent weapon firing events.
        /// </summary>
        /// <param name="position">The source of the shots</param>
        /// <param name="intensity">The muzzle flash intensity</param>
        public void Notify_ShotsFiredAt(IntVec3 position, float intensity = 0.8f)
        {
            if (!position.InBounds(map) || intensity <= 1e-3f)
                return;
            for (int i = 0; i < 9; i++)
            {
                IntVec3 cell = position + AdjCells[i];
                if (cell.InBounds(map)) muzzle_grid[CellToIndex(cell)] += new MuzzleRecord(intensity * AdjWeights[i] / MUZZLEFLASH_MEAN);
            }
            if (Controller.settings.DebuggingMode && Controller.settings.DebugMuzzleFlash)
            {
                for (int i = 0; i < 9; i++)
                    _recent.Add(position + AdjCells[i]);
            }
        }

        /// <summary>
        /// Used to retrive the combat lighting value in CE. It combines both the vanilla system and a new muzzle flash system with a diffusion system.
        /// </summary>
        /// <param name="position">Position</param>
        /// <returns>Amount of light at said position</returns>
        public float CombatGlowAt(IntVec3 position)
        {
            float result = 0f;
            for (int i = 0; i < 9; i++)
                result += AdjWeights[i] * GetGlowForCell(position + AdjCells[i]) / WEIGHTSSUM;
            return Mathf.Min(result, IsNight ? 0.5f : 1.0f);
        }

        /// <summary>
        /// Used to retrive the combat lighting value in CE in relation to an other position. It combines both the vanilla system and a new muzzle flash system with a diffusion system. It is used to balance the difference lighting during daytime hours.
        /// </summary>
        /// <param name="target">Position</param>
        /// <param name="source">Position</param>
        /// <returns>Amount of light at said position</returns>
        public float CombatGlowAtFor(IntVec3 source, IntVec3 target)
        {
            float glowAtSource = map.glowGrid.GameGlowAt(source);
            // Detect day light
            if (glowAtSource > 0.5f)
            {
                // Limit the advantage of being under a roof since the AI can be a bit stupid.                
                return Mathf.Max(CombatGlowAt(target), glowAtSource / 2f);
            }
            // Normally just return this
            return CombatGlowAt(target);
        }

        private float GetGlowForCell(IntVec3 position)
        {
            if (position.InBounds(map))
            {
                var glow = map.glowGrid.GameGlowAt(position);
                var record = muzzle_grid[CellToIndex(position)];
                if (!record.Recent)
                    return glow;
                return Mathf.Clamp01(Mathf.Max(glow, record.Intensity * 0.75f));
            }
            return 0f;
        }

        private readonly CellIndices _cellIndices;

        private int CellToIndex(IntVec3 position)
        {
            return _cellIndices.CellToIndex(position);
        }

        private void Expose_MuzzleGrid()
        {
            List<MuzzleRecord> grid = muzzle_grid?.ToList() ?? new List<MuzzleRecord>();
            Scribe_Collections.Look(ref grid, "muzzle_grid", LookMode.Deep);
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                if (grid == null || grid.Count < NumGridCells)
                    muzzle_grid = new MuzzleRecord[NumGridCells];
                else
                    muzzle_grid = grid.ToArray();
            }
            grid?.Clear();
        }
    }
}
