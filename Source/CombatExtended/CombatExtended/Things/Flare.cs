using System;
using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class Flare : ThingWithComps
    {
        private const float BURN_TICKS = 35 * GenTicks.TicksPerRealSecond;
        private const float SPAWN_FLYOVER_START_ALT = 10;
        private const float SPAWN_FLYOVER_FINAL_ALT = 2;
        private const float SPAWN_DIRECT_ALT = 0;

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

        private int spawnTick = -1;
        private int nextSmokePuff = -1;

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
                            _startingAltitude = _startingAltitude == -1 ?
                                SPAWN_FLYOVER_START_ALT : _startingAltitude;
                            _finalAltitude = _finalAltitude == -1 ?
                                SPAWN_FLYOVER_FINAL_ALT : _finalAltitude;
                            break;
                        case FlareDrawMode.Direct:
                            _startingAltitude = SPAWN_DIRECT_ALT;
                            _finalAltitude = SPAWN_DIRECT_ALT;
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

        private Vector3 ShiftedDrawPos
        {
            get
            {
                Vector3 drawPosition = DrawPos;
                drawPosition.y += CurAltitude;
                return drawPosition;
            }
        }
        private float CurAltitude
        {
            get
            {
                return Mathf.Lerp(StartingAltitude, FinalAltitude, Progress);
            }
        }

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
            if (nextSmokePuff == -1)
            {
                FleckMaker.ThrowSmoke(ShiftedDrawPos, Map, size: Rand.Range(SMOKE_MIN_SIZE, SMOKE_MAX_SIZE));
                nextSmokePuff = Rand.Range(SMOKE_MIN_INTERVAL, SMOKE_MAX_INTERVAL);
            }
            nextSmokePuff--;
            if (Progress >= 0.99f) Destroy();
        }

        public override void Draw()
        {
            if (def.drawerType == DrawerType.RealtimeOnly) DrawAt(ShiftedDrawPos);
        }
    }
}
