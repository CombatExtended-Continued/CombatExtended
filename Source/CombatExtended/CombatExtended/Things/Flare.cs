using System;
using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class Flare : ThingWithComps
    {
        public const float BURN_TICKS = 35 * GenTicks.TicksPerRealSecond;
        public const float ALTITUDE_FACTOR = 0.3f;
        public const float ALTITUDE_DRAW_FACTOR = 0.4f;
        public const float DEFAULT_FLYOVER_START_ALT = 15;
        public const float DEFAULT_FLYOVER_FINAL_ALT = 2;
        public const float DEFAULT_DIRECT_ALT = 0;

        private const int SMOKE_MIN_INTERVAL = 15;
        private const int SMOKE_MAX_INTERVAL = 30;

        private const float SMOKE_MIN_SIZE = 0.15f;
        private const float SMOKE_MAX_SIZE = 1.25f;

        public enum FlareDrawMode
        {
            /// <summary>
            /// Indicate that the flare has not been properly configured.
            /// </summary>
            Unknown = 0,
            /// <summary>
            /// Indicate that the flare should decent torwords the target. Typically used when flyover is set in the spawner verb.
            /// </summary>
            FlyOver = 1,
            /// <summary>
            /// Indicate that the flare should draw at an altitude of 0
            /// </summary>
            Direct = 2,
        }

        private float _startingAltitude = -1;
        public float StartingAltitude
        {
            get
            {
                if (_startingAltitude == -1)
                {
                    Log.Error("CE: Called StartingAltitude_get before setting a DrawMode");
                    _ = DrawMode;
                }
                return _startingAltitude;
            }
            set
            {
                _startingAltitude = value;
            }
        }
        private float _finalAltitude = -1;
        public float FinalAltitude
        {
            get
            {
                if (_finalAltitude == -1)
                {
                    Log.Error("CE: Called FinalAltitude_get before setting a DrawMode");
                    _ = DrawMode;
                }
                return _finalAltitude;
            }
            set
            {
                _finalAltitude = value;
            }
        }
        private FlareDrawMode _drawMode = FlareDrawMode.Unknown;
        public FlareDrawMode DrawMode
        {
            get
            {
                return _drawMode;
            }
            set
            {
                if (_drawMode != value)
                {
                    _drawMode = value;
                    switch (_drawMode)
                    {
                        case FlareDrawMode.FlyOver:
                            _startingAltitude = _startingAltitude <= 0 ?
                                DEFAULT_FLYOVER_START_ALT : _startingAltitude;
                            _finalAltitude = _finalAltitude <= 0 ?
                                DEFAULT_FLYOVER_FINAL_ALT : _finalAltitude;
                            break;
                        case FlareDrawMode.Direct:
                            _startingAltitude = DEFAULT_DIRECT_ALT;
                            _finalAltitude = DEFAULT_DIRECT_ALT;
                            break;
                    }
                }
            }
        }
        public float Progress
        {
            get
            {
                return (float)(GenTicks.TicksGame - spawnTick) / BURN_TICKS;
            }
        }
        public override Vector3 DrawPos
        {
            get
            {
                Vector3 pos = base.DrawPos;
                pos.y = def.Altitude;
                pos.z += CurAltitude * ALTITUDE_DRAW_FACTOR;
                return pos;
            }
        }
        private float CurAltitude
        {
            get
            {
                return StartingAltitude * (1 - Progress) + FinalAltitude * Progress;
            }
        }

        private int spawnTick = -1;
        private int nextSmokePuff = -1;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            if (spawnTick == -1) spawnTick = GenTicks.TicksGame;
            /*
             * Must have a properly configured flare before spawning
             * Will default to flyover
             */
            if (_drawMode == FlareDrawMode.Unknown)
            {
                Log.Error($"CE: Tried to draw a miss configured flare {this} at {Position} with unknown FlareDrawMode. Defaulting to flyover");
                DrawMode = FlareDrawMode.FlyOver;
            }
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref _drawMode, "drawMode", FlareDrawMode.Unknown);
            Scribe_Values.Look(ref _finalAltitude, "_finalAltitude");
            Scribe_Values.Look(ref _startingAltitude, "_startingAltitude");
            Scribe_Values.Look(ref spawnTick, "spawnTick", -1);

            base.ExposeData();
        }

        public override void Tick()
        {
            base.Tick();
            if (nextSmokePuff <= 0)
            {
                FleckMaker.ThrowSmoke(DrawPos, Map, Rand.Range(SMOKE_MIN_SIZE, SMOKE_MAX_SIZE));
                FleckMaker.ThrowFireGlow(DrawPos, Map, 1);
                nextSmokePuff = Rand.Range(SMOKE_MIN_INTERVAL, SMOKE_MAX_INTERVAL);
            }
            nextSmokePuff--;
            if (Progress >= 0.99f) Destroy();
        }
    }
}
