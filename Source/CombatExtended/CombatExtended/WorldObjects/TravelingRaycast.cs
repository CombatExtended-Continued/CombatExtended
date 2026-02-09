using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CombatExtended;
public class TravelingRaycast : TravelingShell
{
    public Verb_ShootCE verbToUse;
    public float spreadDegrees;
    public float aperatureSize;
    public Thing equipement;

    protected override void LaunchProjectile(IntVec3 sourceCell, LocalTargetInfo target, Map map, float shotSpeed = 20)
    {
        Vector3 source = new Vector3(sourceCell.x, arrivedShotHeight, sourceCell.z);
        Vector3 targetPos = target.Cell.ToVector3Shifted();

        ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(shellDef);
        ProjectilePropertiesCE pprops = projectile.def.projectile as ProjectilePropertiesCE;
        float shotRotation = pprops.TrajectoryWorker.ShotRotation(pprops, source, targetPos);
        float shotAngle = pprops.TrajectoryWorker.ShotAngle(pprops, source, targetPos, shotSpeed);

        projectile.canTargetSelf = false;
        projectile.Position = sourceCell;
        projectile.SpawnSetup(map, false);

        if (pprops.isInstant)
        {
            if (verbToUse == null)
            {
                Log.Warning("Instant shelling needs a ShootingCE Verb in order to work.");
                return;
            }

            float tsa = verbToUse.AdjustShotHeight(launcher, target, ref arrivedShotHeight);
            projectile.RayCast(launcher,
                verbToUse.verbProps,
                new Vector2(source.x, source.z),
                shotAngle,
                shotRotation,
                arrivedShotHeight + tsa,
                shotSpeed,
                spreadDegrees,
                aperatureSize,
                null,
                true // Allow beam to be drawn correctly
            );
        }
        else
        {
            // classic shell behavior
            Log.Warning("TravellingRaycast called for a classic projectile. Aborted.");
        }
    }
}
