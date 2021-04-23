using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace CombatExtended.Lasers
{
    public class LaserBeamGraphicCE :Thing
    {
        public LaserBeamDefCE projDef;
        float beamWidth;
        float beamLength;
        int ticks;
        int colorIndex = 2;
        Vector3 a;
        Vector3 b;

        public Matrix4x4 drawingMatrix = default(Matrix4x4);
        Material materialBeam;
        Mesh mesh;
        Thing launcher;
        ThingDef equipmentDef;
        public List<Mesh> meshes = new List<Mesh>();


        public float Opacity => (float)Math.Sin(Math.Pow(1.0 - 1.0 * ticks / projDef.lifetime, projDef.impulse) * Math.PI);
        public bool Lightning => projDef.LightningBeam;
        public bool Static => projDef.StaticLightning;
        public int ticksToDetonation;
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref beamWidth, "beamWidth");
            Scribe_Values.Look(ref beamLength, "beamLength");
            Scribe_Values.Look(ref ticks, "ticks");
            Scribe_Values.Look(ref colorIndex, "colorIndex");
            Scribe_Values.Look(ref a, "a");
            Scribe_Values.Look(ref b, "b");
            Scribe_Defs.Look(ref projDef, "projectileDef");
        }

        public override void Tick()
        {
            if (def==null || ticks++ > projDef.lifetime)
            {
                Destroy(DestroyMode.Vanish);
            }
            if (this.ticksToDetonation > 0)
            {
                this.ticksToDetonation--;
                if (this.ticksToDetonation <= 0)
                {
                    this.Explode();
                }
            }
        }

        void SetColor(Thing launcher)
        {
            IBeamColorThing gun = null;

            Pawn pawn = launcher as Pawn;
            if (pawn != null && pawn.equipment != null) gun = pawn.equipment.Primary as IBeamColorThing;
            if (gun == null) gun = launcher as IBeamColorThing;

            if (gun != null && gun.BeamColor != -1)
            {
                colorIndex = gun.BeamColor;
            }
            if (gun !=null)
            {
                this.equipmentDef = pawn.equipment.Primary.def;
            }
        }

        public void Setup(Thing launcher, Vector3 origin, Vector3 destination)
        {
            //SetColor(launcher);
            this.launcher = launcher;
            a = origin;
            b = destination;
        }

        public void SetupDrawing()
        {
            if (mesh != null) return;

            materialBeam = projDef.GetBeamMaterial(colorIndex) ?? LaserBeamGraphicCE.BeamMat;

            if (this.def.graphicData != null)
            {
                if (this.def.graphicData.graphicClass != null)
                {
                    if (this.def.graphicData.graphicClass == typeof(Graphic_Random))
                    {
                        materialBeam = projDef.GetBeamMaterial(0) ?? def.graphicData.Graphic.MatSingle;
                    }
                }
            }
            beamWidth = projDef.beamWidth;
            Quaternion rotation = Quaternion.LookRotation(b - a);
            Vector3 dir = (b - a).normalized;
            beamLength = (b - a).magnitude;

            Vector3 drawingScale = new Vector3(beamWidth, 1f, beamLength);
            Vector3 drawingPosition = (a + b) / 2;
            drawingPosition.y = AltitudeLayer.MetaOverlays.AltitudeFor();
            drawingMatrix.SetTRS(drawingPosition, rotation, drawingScale);

            float textureRatio = 1.0f * materialBeam.mainTexture.width / materialBeam.mainTexture.height;
            float seamTexture = projDef.seam < 0 ? textureRatio : projDef.seam;
            float capLength = beamWidth / textureRatio / 2f * seamTexture;
            float seamGeometry = beamLength <= capLength * 2 ? 0.5f : capLength * 2 / beamLength;

            if (Lightning)
            {
                float distance = Vector3.Distance(a, b);
                for (int i = 0; i < projDef.ArcCount; i++)
                {
                    Mesh m = LightningLaserBoltMeshMaker.NewBoltMesh(-(distance + 0.25f), projDef.LightningVariance, beamWidth);
                    meshes.Add(m);
                }
            //    this.mesh = LightningBoltMeshMakerOG.NewBoltMesh(new Vector2(0, -(distance + 0.25f)), def.LightningVariance);
            }
            else
            {
                this.mesh = MeshMakerLaser.Mesh(seamTexture, seamGeometry);
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (def==null || projDef.decorations == null || respawningAfterLoad) return;

            foreach (var decoration in projDef.decorations)
            {
                float spacing = decoration.spacing * projDef.beamWidth;
                float initalOffset = decoration.initialOffset * projDef.beamWidth;

                Vector3 dir = (b - a).normalized;
                float angle = (b - a).AngleFlat();
                Vector3 offset = dir * spacing;
                Vector3 position = a + offset * 0.5f + dir * initalOffset;
                float length = (b - a).magnitude - spacing;

                int i = 0;
                while (length > 0)
                {
                    MoteLaserDectorationCE moteThrown = ThingMaker.MakeThing(decoration.mote, null) as MoteLaserDectorationCE;
                    if (moteThrown == null) break;

                    moteThrown.beam = this;
                    moteThrown.airTimeLeft = projDef.lifetime;
                    moteThrown.Scale = projDef.beamWidth;
                    moteThrown.exactRotation = angle;
                    moteThrown.exactPosition = position;
                    moteThrown.SetVelocity(angle, decoration.speed);
                    moteThrown.baseSpeed = decoration.speed;
                    moteThrown.speedJitter = decoration.speedJitter;
                    moteThrown.speedJitterOffset = decoration.speedJitterOffset * i;
                    GenSpawn.Spawn(moteThrown, a.ToIntVec3(), map, WipeMode.Vanish);

                    position += offset;
                    length -= spacing;
                    i++;
                }
            }
        }

        public override void Draw()
        {
            SetupDrawing();

            float opacity = Opacity;
            Color color = projDef.graphicData.color;
            color.a *= opacity;
            LaserBeamGraphicCE.MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, color);
            if (Lightning)
            {
                Vector3 vector;
                vector.x = (float)b.x;
                vector.y = (float)b.y;
                vector.z = (float)b.z;
                float distance = Vector3.Distance(a, b);

                for (int i = 0; i < projDef.ArcCount; i++)
                {
                    if (this.def.graphicData != null)
                    {
                        if (this.def.graphicData.graphicClass != null)
                        {
                            if (this.def.graphicData.graphicClass == typeof(Graphic_Flicker))
                            {
                                if (!Find.TickManager.Paused && Find.TickManager.TicksGame % this.projDef.flickerFrameTime == 0)
                                {
                                    materialBeam = projDef.GetBeamMaterial((this.projDef.materials.IndexOf(materialBeam) + 1 < this.projDef.materials.Count ? this.projDef.materials.IndexOf(materialBeam) + 1 : 0)) ?? def.graphicData.Graphic.MatSingle;
                                }
                            }
                        }
                    }
                    meshes[i] = Find.TickManager.Paused || Find.TickManager.TicksGame % this.projDef.flickerFrameTime != 0 || meshes[i] != null && Static ? meshes[i] : LightningLaserBoltMeshMaker.NewBoltMesh(-(distance + 0.25f), projDef.LightningVariance, beamWidth);
                    Graphics.DrawMesh(this.meshes[i], b, Quaternion.LookRotation((vector - this.a).normalized), materialBeam, 0, null, 0, LaserBeamGraphicCE.MatPropertyBlock, 0);

                }
            }
            else
            {
                if (this.def.graphicData != null)
                {
                    if (this.def.graphicData.graphicClass != null)
                    {
                        if (this.def.graphicData.graphicClass == typeof(Graphic_Flicker))
                        {
                            if (!Find.TickManager.Paused && Find.TickManager.TicksGame % this.projDef.flickerFrameTime == 0)
                            {
                                materialBeam = projDef.GetBeamMaterial((this.projDef.materials.IndexOf(materialBeam) + 1 < this.projDef.materials.Count ? this.projDef.materials.IndexOf(materialBeam) + 1 : 0)) ?? def.graphicData.Graphic.MatSingle;
                            }
                        }
                    }
                }
                Graphics.DrawMesh(mesh, drawingMatrix, materialBeam, 0, null, 0, LaserBeamGraphicCE.MatPropertyBlock);
                //   Graphics.DrawMesh(mesh, drawingMatrix, FadedMaterialPool.FadedVersionOf(materialBeam, opacity), 0);
            }
        }
        protected virtual void Explode()
        {
            Map map = base.Map;
            IntVec3 intVec = this.b.ToIntVec3();
            bool flag = this.def.projectile.explosionEffect != null;
            if (flag)
            {
                Effecter effecter = this.def.projectile.explosionEffect.Spawn();
                effecter.Trigger(new TargetInfo(intVec, map, false), new TargetInfo(intVec, map, false));
                effecter.Cleanup();
            }
            IntVec3 center = intVec;
            Map map2 = map;
            float explosionRadius = this.def.projectile.explosionRadius;
            DamageDef damageDef = this.def.projectile.damageDef;
            Thing launcher = this.launcher;
            int damageAmount = this.def.projectile.GetDamageAmount(1f, null);
            SoundDef soundExplode = this.def.projectile.soundExplode;
            ThingDef equipmentDef = this.equipmentDef;
            ThingDef def = this.def;
            ThingDef postExplosionSpawnThingDef = this.def.projectile.postExplosionSpawnThingDef;
            float postExplosionSpawnChance = this.def.projectile.postExplosionSpawnChance;
            int postExplosionSpawnThingCount = this.def.projectile.postExplosionSpawnThingCount;
            ThingDef preExplosionSpawnThingDef = this.def.projectile.preExplosionSpawnThingDef;
            GenExplosion.DoExplosion(center, map2, explosionRadius, damageDef, launcher, damageAmount, 0f, soundExplode, equipmentDef, def, null, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, this.def.projectile.applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef, this.def.projectile.preExplosionSpawnChance, this.def.projectile.preExplosionSpawnThingCount, this.def.projectile.explosionChanceToStartFire, this.def.projectile.explosionDamageFalloff);
        }

        private static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly Material BeamEndMat = MaterialPool.MatFrom("Other/OrbitalBeamEnd", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);
        private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();
    }
}
