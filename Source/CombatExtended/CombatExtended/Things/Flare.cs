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
        public const float SPAWN_FLYOVER_START_ALT = 40;
        public const float SPAWN_FLYOVER_FINAL_ALT = 10;
        public const float SPAWN_DIRECT_ALT = 0;

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
                                SPAWN_FLYOVER_START_ALT : _startingAltitude;
                            _finalAltitude = _finalAltitude <= 0 ?
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
        private Vector3 DrawPosShifted
        {
            get
            {
                Vector3 pos = DrawPos;
                pos.y = CurAltitude;
                pos.z += pos.y * ALTITUDE_FACTOR;
                return pos;
            }
        }
        private Quaternion Quat
        {
            get
            {
                return def.graphicData.Graphic.QuatFromRot(Rot4.North);
            }
        }
        private float CurAltitude
        {
            get
            {
                return StartingAltitude * (1 - Progress) + FinalAltitude * Progress;
            }
        }
        private Mesh _mesh = null;
        private Mesh FlareMesh
        {
            get
            {
                if (_mesh == null) _mesh = def.graphicData.Graphic.MeshAt(Rot4.North);
                return _mesh;
            }
        }
        private Material _material = null;
        private Material FlareMat
        {
            get
            {
                if (_material == null) _material = def.graphicData.Graphic.MatSingle;
                return _material;
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
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(DrawPosShifted, Map, FleckDefOf.Smoke, Rand.Range(1.5f, 2.5f) * Rand.Range(SMOKE_MIN_SIZE, SMOKE_MAX_SIZE));
                dataStatic.rotationRate = Rand.Range(-30f, 30f);
                dataStatic.velocityAngle = Rand.Range(30, 40);
                dataStatic.velocitySpeed = Rand.Range(0.5f, 0.7f);
                dataStatic.spawnPosition = DrawPosShifted;
                Map.flecks.CreateFleck(dataStatic);
                nextSmokePuff = Rand.Range(SMOKE_MIN_INTERVAL, SMOKE_MAX_INTERVAL);
            }
            nextSmokePuff--;
            if (Progress >= 0.99f) Destroy();
        }

        private readonly Vector3 _drawSize = new Vector3(1, 0, 1);

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Matrix4x4 drawMat = new Matrix4x4();
            drawLoc.y = CurAltitude;
            drawLoc.z += drawLoc.y * ALTITUDE_FACTOR;
            drawMat.SetTRS(drawLoc, Quat, _drawSize);
            Graphics.DrawMesh(MeshPool.GridPlane(base.def.graphicData.drawSize), drawMat, FlareMat, 0);
        }
    }
}
