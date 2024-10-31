using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class Verb_ShootCE_CIWS : Verb_ShootCE
    {
        public Building_CIWS_CE Turret => Caster as Building_CIWS_CE;
        public override ThingDef Projectile
        {
            get
            {
                var result = base.Projectile;
                var ciwsVersion = (result?.projectile as ProjectilePropertiesCE)?.CIWSVersion;
                if (ciwsVersion == null && typeof(ProjectileCE_CIWS).IsAssignableFrom(result.thingClass))
                {
                    Log.WarningOnce($"{result} is not a CIWS projectile and the projectile does not have the CIWS version specified in its properties. Must be on-ground projectile used for CIWS", result.GetHashCode());
                }
                return ciwsVersion ?? result;
            }
        }

        protected int maximumPredectionTicks = 40;
        public override bool TryFindCEShootLineFromTo(IntVec3 root, LocalTargetInfo targ, out ShootLine resultingLine)
        {
            var originV3 = Shooter.Position.ToVector3Shifted();
            var targetComp = targ.Thing?.TryGetComp<CompCIWSTarget>();
            var ticksToSkip = (int)verbProps.warmupTime;
            if (targetComp != null)
            {
                var result = targetComp.CalculatePointForPreemptiveFire(Projectile, originV3, out var targetPos, ticksToSkip);
                resultingLine = new ShootLine(originV3.ToIntVec3(), targetPos.ToIntVec3());
                return result;
            }
            else if (targ.Thing is Skyfaller skyfaller)
            {

            }
            else if (targ.Thing is ProjectileCE projectile)
            {
                var instant = Projectile.projectile is ProjectilePropertiesCE projectilePropertiesCE && projectilePropertiesCE.isInstant;
                if (instant)
                {
                    resultingLine = new ShootLine(root, projectile.Position);
                    return true;
                }
                int i = 1;
                if (projectile.def.projectile is ProjectilePropertiesCE target_ProjectilePropeties && Projectile.projectile is ProjectilePropertiesCE CIWS_ProjectilePropeties)
                {
                    var targetPos1 = new Vector2(projectile.Position.x, projectile.Position.z);
                    foreach (var pos in projectile.NextPositions.Skip(ticksToSkip))
                    {
                        if (i > maximumPredectionTicks)
                        {
                            break;
                        }
                        var report = ShiftVecReportFor(pos.ToIntVec3());
                        ShiftTarget(report);

                        Vector2 originV2 = new Vector2(originV3.x, originV3.z), destinationV2 = new Vector2(pos.x, pos.z);
                        var positions = CIWS_ProjectilePropeties.NextPositions(shotRotation, shotAngle, originV2, destinationV2, maximumPredectionTicks, ShotHeight, false, Vector3.zero, ShotSpeed, originV3, -1f, -1f, -1f, -1f, ShotSpeed, 0).Skip(i - 1).Take(2).ToList();
                        if (positions.Count < 2)
                        {
                            resultingLine = default(ShootLine);
                            return false;
                        }
                        Vector2 ciwsPos1 = new Vector2(positions.First().x, positions.First().z), ciwsPos2 = new Vector2(positions.Last().x, positions.Last().z), targetPos2 = new Vector2(pos.x, pos.z);

                        if (CE_Utility.TryFindIntersectionPoint(ciwsPos1, ciwsPos2, targetPos1, targetPos2, out var point))
                        {
                            resultingLine = new ShootLine(Shooter.Position, point.ToVector3().ToIntVec3());
                            return true;
                        }
                        targetPos1 = targetPos2;
                        i++;
                    }
                }
            }
            return base.TryFindCEShootLineFromTo(root, targ, out resultingLine);
        }
    }
}
