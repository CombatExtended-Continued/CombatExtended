using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatExtended
{
    public class Verb_ShootMortarCE : Verb_ShootCE
    {
        /// <summary>
        /// When targeting a global target, this is used to set an exit point for mortars.
        /// </summary>
        public LocalTargetInfo mokeTargetInfo = LocalTargetInfo.Invalid;
        /// <summary>
        /// Global target info on another map.
        /// </summary>
        public GlobalTargetInfo globalTargetInfo = GlobalTargetInfo.Invalid;
        /// <summary>
        /// Source information
        /// </summary>
        public GlobalTargetInfo globalSourceInfo = GlobalTargetInfo.Invalid;

        // for global target only
        //        
        private int startingTile;
        private int destinationTile;
        private int globalDistance;
        private Vector3 direction;
        private int numShotsFired;
        private ShiftVecReport globalVecReport;

        public override void ExposeData()
        {
            base.ExposeData();            
            Scribe_Values.Look(ref startingTile, "startingTile");
            Scribe_Values.Look(ref destinationTile, "destinationTile");
            Scribe_Values.Look(ref globalDistance, "globalDistance");
            Scribe_Values.Look(ref shotRotation, "shotRotation");
            Scribe_Values.Look(ref shotAngle, "shotAngle");
            CE_Scriber.Late(this.caster, (id) =>
            {
                Scribe_TargetInfo.Look(ref globalTargetInfo, "targetInfo_" + id);
                Scribe_TargetInfo.Look(ref globalSourceInfo, "sourceInfo_" + id);
                Scribe_TargetInfo.Look(ref mokeTargetInfo, "localTarget_" + id);
            });
        }        

        public bool TryStartShelling(GlobalTargetInfo sourceInfo, GlobalTargetInfo targetInfo)
        {
            this.globalTargetInfo = targetInfo;
            this.globalSourceInfo = sourceInfo;
            this.mokeTargetInfo = GetLocalTargetFor(targetInfo);            
            if (!TryStartCastOn(mokeTargetInfo, true, true, false))
            {
                this.globalTargetInfo = this.globalSourceInfo = GlobalTargetInfo.Invalid;
                this.mokeTargetInfo = LocalTargetInfo.Invalid;
                return false;
            }            
            return true;
        }       

        public override ShiftVecReport ShiftVecReportFor(LocalTargetInfo target)
        {
            ShiftVecReport report = base.ShiftVecReportFor(target);            
            report.circularMissRadius = this.GetMissRadiusForDist(report.shotDist);

            // Check for marker
            ArtilleryMarker marker = null;
            if (this.currentTarget.HasThing && this.currentTarget.Thing.HasAttachment(ThingDef.Named(ArtilleryMarker.MarkerDef)))
            {
                marker = (ArtilleryMarker)this.currentTarget.Thing.GetAttachment(ThingDef.Named(ArtilleryMarker.MarkerDef));
            }
            else if (currentTarget.Cell.InBounds(caster.Map))
            {
                marker = (ArtilleryMarker)this.currentTarget.Cell.GetFirstThing(caster.Map, ThingDef.Named(ArtilleryMarker.MarkerDef));
            }
            if (marker != null)
            {
                report.aimingAccuracy = marker.aimingAccuracy;
                report.sightsEfficiency = marker.sightsEfficiency;
                report.weatherShift = marker.weatherShift;
                report.lightingShift = marker.lightingShift;
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(CE_ConceptDefOf.CE_Spotting, KnowledgeAmount.SpecificInteraction);
            }
            // If we don't have a marker check for indirect fire and apply penalty
            else if (report.shotDist > 75 || !GenSight.LineOfSight(this.caster.Position, report.target.Cell, caster.Map, true))
            {
                report.indirectFireShift = this.VerbPropsCE.indirectFirePenalty * report.shotDist;
                report.weatherShift = 0f;
                report.lightingShift = 0f;
            }
            return report;
        }        
         

        protected virtual LocalTargetInfo GetLocalTargetFor(GlobalTargetInfo targetInfo)
        {            
            this.startingTile = caster.Map.Tile;
            this.destinationTile = targetInfo.Tile;
            this.direction = (Find.WorldGrid.GetTileCenter(startingTile) - Find.WorldGrid.GetTileCenter(destinationTile)).normalized;
            this.globalDistance = (int)CE_Utility.DistanceBetweenTiles(targetInfo.Tile, caster.Map.Tile);
            
            var shellingRay = new Ray(caster.DrawPos, direction);
            var exitCell = shellingRay.ExitCell(caster.Map);                             
            return new LocalTargetInfo(exitCell);
        }

        public virtual bool TryCastGlobalShot()
        {
            ShootLine shootLine = new ShootLine(caster.positionInt, mokeTargetInfo.Cell);
            if (projectilePropsCE.pelletCount < 1)
            {
                Log.Error(EquipmentSource.LabelCap + " tried firing with pelletCount less than 1.");
                return false;
            }
            bool instant = false;                       
            if (Projectile.projectile is ProjectilePropertiesCE pprop)
            {
                instant = pprop.isInstant;               
            }

            ShiftVecReport reportGlobal = this.globalVecReport = ShiftVecReportFor(globalTargetInfo);
            ShiftVecReport report = ShiftVecReportFor(currentTarget);            
                      
            bool pelletMechanicsOnly = false;
            for (int i = 0; i < projectilePropsCE.pelletCount; i++)
            {                
                ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(Projectile, null);
                GenSpawn.Spawn(projectile, shootLine.Source, caster.Map);                
                ShiftGlobalTarget(reportGlobal);
                ShiftTarget(report, pelletMechanicsOnly, instant);

                float shotSpeed = ShotSpeed;
                if (this.globalTargetInfo.IsValid)
                {
                    shotSpeed *= 5;
                }
                //New aiming algorithm
                projectile.globalTargetInfo = globalTargetInfo;                
                projectile.canTargetSelf = false;                
                projectile.intendedTarget = currentTarget;
                projectile.globalSourceInfo = globalSourceInfo;
                projectile.mount = caster.Position.GetThingList(caster.Map).FirstOrDefault(t => t is Pawn && t != caster);
                projectile.AccuracyFactor = report.accuracyFactor * report.swayDegrees * ((numShotsFired + 1) * 0.75f);
                projectile.Launch(
                                    Shooter,    //Shooter instead of caster to give turret operators' records the damage/kills obtained
                                    sourceLoc,
                                    shotAngle,
                                    shotRotation,
                                    ShotHeight,
                                    shotSpeed,
                                    EquipmentSource);                
                pelletMechanicsOnly = true;
            }

            /*
             * Notify the lighting tracker that shots fired with muzzle flash value of VerbPropsCE.muzzleFlashScale
             */
            LightingTracker.Notify_ShotsFiredAt(caster.Position, intensity: VerbPropsCE.muzzleFlashScale);
            pelletMechanicsOnly = false;
            numShotsFired++;
            if (ShooterPawn != null)
            {
                if (CompAmmo != null && !CompAmmo.CanBeFiredNow)
                {
                    CompAmmo?.TryStartReload();
                }
                if (CompReloadable != null)
                {
                    CompReloadable.UsedOnce();
                }
            }
            return true;            
        }

        public override bool TryCastShot()
        {                        
            if (!globalTargetInfo.IsValid)
            {                
                return base.TryCastShot();                
            }            
            if (CompAmmo != null)
            {
                if (!CompAmmo.TryReduceAmmoCount(VerbPropsCE.ammoConsumedPerShotCount))
                {
                    return false;
                }
            }
            if (this.TryCastGlobalShot())
            {
                return this.OnCastSuccessful();
            }
            return false;
        }        

        protected virtual float GetMissRadiusForDist(float targDist)
        {
            float maxRange = this.verbProps.range;
            if (this.CompCharges != null)
            {
                Vector2 bracket;
                if (this.CompCharges.GetChargeBracket(targDist, ShotHeight, projectilePropsCE.Gravity, out bracket))
                {
                    maxRange = bracket.y;
                }
            }
            float rangePercent = targDist / maxRange;
            float missRadiusFactor = rangePercent <= 0.5f ? 1 - rangePercent : 0.5f + ((rangePercent - 0.5f) / 2);
            return VerbPropsCE.circularError * missRadiusFactor;
        }

        protected virtual float GetGlobalMissRadiusForDist(float targDist)
        {
            float maxRange = this.projectilePropsCE.shellingProps.range * 5f;          
            float rangePercent = targDist / maxRange;
            float missRadiusFactor = rangePercent <= 0.5f ? 1 - rangePercent : 0.5f + ((rangePercent - 0.5f) / 2);
            return VerbPropsCE.circularError * missRadiusFactor;
        }

        private void ShiftGlobalTarget(ShiftVecReport report)
        {
            if(report == null || globalTargetInfo.Map == null || !globalTargetInfo.Cell.IsValid)
            {
                return;
            }
            report.shotDist = Mathf.Max(report.shotDist, report.maxRange * 0.334f);
            var target = new Vector2(globalTargetInfo.Cell.x, globalTargetInfo.Cell.z);
            var direction = (Find.WorldGrid.GetTileCenter(startingTile) - Find.WorldGrid.GetTileCenter(destinationTile)).normalized;
            report.weatherShift = (1f - globalTargetInfo.Map.weatherManager.CurWeatherAccuracyMultiplier) * 1.5f + (1 - globalSourceInfo.Map.weatherManager.CurWeatherAccuracyMultiplier) * 0.5f;
            report.lightingShift = 0.5f; 
                                  
            var estimatedTargDist = report.GetRandDist();
            var spreadVec  = UnityEngine.Random.insideUnitCircle * Mathf.Clamp(report.spreadDegrees * Mathf.PI / 360f, -1f, 1f) * (estimatedTargDist - report.shotDist);
            var ray = new Ray2D(target, -1 * direction);
            var shiftedTarg  = ray.GetPoint(estimatedTargDist - report.shotDist + spreadVec.x) + report.GetRandCircularVec();
            
            globalTargetInfo.cellInt = new IntVec3((int)shiftedTarg.x, 0, (int)shiftedTarg.y);            
        }
    }
}
