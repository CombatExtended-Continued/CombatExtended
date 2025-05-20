using System;
using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class FlareModExtensionCE : DefModExtension
    {
        public ThingDef SmokeMote = CE_ThingDefOf.Mote_FlareSmoke;
        public ThingDef FireMote = CE_ThingDefOf.Mote_FlareGlow;
        public FleckDef LandglowFleck = FleckDefOf.FireGlow;
        public ThingDef FlareThingDef = CE_ThingDefOf.Flare;

        public float BurnSeconds = 35;
        public float AltitudeDrawFactor = 0.7f;
        public float FlyoverStartAltitude = 30;
        public float FlyoverFinalAltitude = 4;
        public float DirectAltitude = 0;

        public float LandglowMinAltitude = 20;
        public float LandglowScale = 4.0f;

        //Note: watersplash has been disabled in RW since 1.3, but still exists in code
        public float WaterSplashMinAltitude = 6;
        public float WatersplashVelocity = 5.0f;
        public float WatersplashSize = 2.5f;

        public int SmokeMinInterval = 18;
        public int SmokeMaxInterval = 25;

        public float SmokeMinSize = 0.15f;
        public float SmokeMaxSize = 1.25f;
    }

    public class Flare : ThingWithComps
    {
        public FlareModExtensionCE modExtension;

        public ThingDef FlareSmokeMote = CE_ThingDefOf.Mote_FlareSmoke;
        public ThingDef FlareGlowMote = CE_ThingDefOf.Mote_FlareGlow;
        public FleckDef LandglowFleck = FleckDefOf.FireGlow;

        public float BURN_TICKS = 35 * GenTicks.TicksPerRealSecond;
        public float ALTITUDE_DRAW_FACTOR = 0.7f;
        public float DEFAULT_FLYOVER_START_ALT = 30;
        public float DEFAULT_FLYOVER_FINAL_ALT = 4;
        public float DEFAULT_DIRECT_ALT = 0;

        public float LANDGLOW_MIN_ALTITUDE = 20;
        public float LANDGLOW_SCALE = 4.0f;

        public float WATERSPLASH_MIN_ALTITUDE = 6;
        public float WATERSPLASH_VELOCITY = 5.0f;
        public float WATERSPLASH_SIZE = 2.5f;

        private int SMOKE_MIN_INTERVAL = 18;
        private int SMOKE_MAX_INTERVAL = 25;

        private float SMOKE_MIN_SIZE = 0.15f;
        private float SMOKE_MAX_SIZE = 1.25f;

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

        private Vector3 _directDrawPos = new Vector3(0, 0, -1);
        public Vector3 DirectDrawPos
        {
            get
            {
                if (_directDrawPos.z == -1)
                {
                    Log.Error($"CE: Tried to draw a miss configured flare {this} at {Position} with unknown DirectDrawPos. Defaulting to base.DirectDrawPos");
                    _directDrawPos = base.DrawPos;
                }
                return _directDrawPos;
            }
            set
            {
                _directDrawPos = value;
            }
        }
        public override Vector3 DrawPos
        {
            get
            {
                if (DrawMode == FlareDrawMode.FlyOver)
                {
                    Vector3 pos = base.DrawPos;
                    pos.y = CurAltitude;
                    pos.z += CurAltitude * ALTITUDE_DRAW_FACTOR;
                    return pos;
                }
                return DirectDrawPos;
            }
        }
        private float CurAltitude
        {
            get
            {
                return StartingAltitude * (1 - Progress) + FinalAltitude * Progress;
            }
        }
        private float HeightDrawScale
        {
            get
            {
                return (1.0f - (Progress * 2 - 0.5f) / 2f);
            }
        }

        private int spawnTick = -1;
        private int nextSmokePuff = -1;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            if (spawnTick == -1)
            {
                spawnTick = GenTicks.TicksGame;
            }
            /*
             * Must have a properly configured flare before spawning
             * Will default to flyover
             */
            if (_drawMode == FlareDrawMode.Unknown)
            {
                Log.Error($"CE: Tried to draw a miss configured flare {this} at {Position} with unknown FlareDrawMode. Defaulting to flyover");
                DrawMode = FlareDrawMode.FlyOver;
            }

            if (modExtension != null)
            {
                FlareSmokeMote = modExtension.SmokeMote ?? FlareSmokeMote;
                FlareGlowMote = modExtension.FireMote ?? FlareGlowMote;
                LandglowFleck = modExtension.LandglowFleck ?? LandglowFleck;
                BURN_TICKS = modExtension.BurnSeconds * GenTicks.TicksPerRealSecond;

                ALTITUDE_DRAW_FACTOR = modExtension.AltitudeDrawFactor;
                DEFAULT_FLYOVER_START_ALT = modExtension.FlyoverStartAltitude;
                DEFAULT_FLYOVER_FINAL_ALT = modExtension.FlyoverFinalAltitude;
                DEFAULT_DIRECT_ALT = modExtension.DirectAltitude;

                LANDGLOW_MIN_ALTITUDE = modExtension.LandglowMinAltitude;
                LANDGLOW_SCALE = modExtension.LandglowScale;
                WATERSPLASH_MIN_ALTITUDE = modExtension.WaterSplashMinAltitude;
                WATERSPLASH_VELOCITY = modExtension.WatersplashVelocity;
                WATERSPLASH_SIZE = modExtension.WatersplashSize;

                SMOKE_MIN_INTERVAL = modExtension.SmokeMinInterval;
                SMOKE_MAX_INTERVAL = modExtension.SmokeMaxInterval;

                SMOKE_MIN_SIZE = modExtension.SmokeMinSize;
                SMOKE_MAX_SIZE = modExtension.SmokeMaxSize;
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

        private static readonly Vector3 _moteDrawOffset = new Vector3(0, 0, -0.5f);

        public override void Tick()
        {
            base.Tick();
            if (nextSmokePuff <= 0)
            {
                /*
                 * Both are copies form vanilla 1.2 smoke and fireglow motes.
                 * Incase this breaks use the fleck system below.
                 */

                if (Compatibility.Multiplayer.InMultiplayer)
                {
                    Rand.PushState();
                }
                MoteThrownCE smokeMote = (MoteThrownCE)ThingMaker.MakeThing(FlareSmokeMote);
                smokeMote.Scale = Rand.Range(1.5f, 2.5f) * Rand.Range(SMOKE_MIN_SIZE, SMOKE_MAX_SIZE) * HeightDrawScale;
                smokeMote.rotationRate = Rand.Range(-30f, 30f);
                smokeMote.SetVelocity(Rand.Range(30, 40), Rand.Range(0.5f, 0.7f));
                smokeMote.positionInt = Position;
                smokeMote.exactPosition = DrawPos;
                smokeMote.drawOffset = _moteDrawOffset;
                smokeMote.attachedAltitudeThing = this;
                smokeMote.SpawnSetup(Map, false);

                MoteThrownCE glowMote = (MoteThrownCE)ThingMaker.MakeThing(FlareGlowMote);
                glowMote.Scale = Rand.Range(4f, 6f) * 0.6f * HeightDrawScale;
                glowMote.rotationRate = Rand.Range(-3f, 3f);
                glowMote.SetVelocity(Rand.Range(0, 360), 0.12f);
                glowMote.positionInt = Position;
                glowMote.drawOffset = _moteDrawOffset;
                glowMote.exactPosition = DrawPos;
                glowMote.attachedAltitudeThing = this;
                glowMote.SpawnSetup(Map, false);

                if (CurAltitude < LANDGLOW_MIN_ALTITUDE)
                {
                    //parameters of ThrowFireGlow() method adapted to use any fleck
                    float size = LANDGLOW_SCALE * (1f - (CurAltitude - FinalAltitude) / (LANDGLOW_MIN_ALTITUDE - FinalAltitude));
                    FleckCreationData dataStatic = FleckMaker.GetDataStatic(DrawPos + size * new Vector3(Rand.Value - 0.5f, 0f, Rand.Value - 0.5f), Map, LandglowFleck, Rand.Range(4f, 6f) * size);
                    dataStatic.rotationRate = Rand.Range(-3f, 3f);
                    dataStatic.velocityAngle = Rand.Range(0, 360f);
                    dataStatic.velocitySpeed = 0.12f;
                    Map.flecks.CreateFleck(dataStatic);
                }
                if (CurAltitude < WATERSPLASH_MIN_ALTITUDE)
                {
                    FleckMaker.WaterSplash(Position.ToVector3Shifted(), Map, Rand.Range(0.8f, 1.2f) * WATERSPLASH_SIZE * (1f - (CurAltitude - FinalAltitude) / (WATERSPLASH_MIN_ALTITUDE - FinalAltitude)), WATERSPLASH_VELOCITY);
                }
                if (Compatibility.Multiplayer.InMultiplayer)
                {
                    Rand.PopState();
                }
                /*
                 * Use incase motes start breaking
                 */
                // FleckMaker.ThrowSmoke(DrawPos, Map, Rand.Range(SMOKE_MIN_SIZE, SMOKE_MAX_SIZE));
                // FleckMaker.ThrowFireGlow(DrawPos, Map, 0.6f);

                nextSmokePuff = Rand.Range(SMOKE_MIN_INTERVAL, SMOKE_MAX_INTERVAL);
            }
            nextSmokePuff--;
            if (Progress >= 0.99f)
            {
                Destroy();
            }
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            var scale = HeightDrawScale;
            var matrix = new Matrix4x4();
            var rot = Rotation;

            matrix.SetTRS(DrawPos + Graphic.DrawOffset(rot), Graphic.QuatFromRot(rot), new Vector3(scale, 0, scale));
            Graphics.DrawMesh(Graphic.MeshAt(rot), matrix, Graphic.MatAt(rot), 0);
        }
    }
}
