using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using UnityEngine;
using CombatExtended.CombatExtended.LoggerUtils;
using CombatExtended.CombatExtended.Jobs.Utils;
using RimWorld.Planet;
using System.Xml;

namespace CombatExtended
{
    public class Building_CiwsGunCE : Building_TurretGunCE
    {
        private const int MINSHOTSREQUIRED = 30;
        private const int MAXSHOTSREQUIRED = 120;
        
        private int shotsFired = 0;
        private int shotsRequired = 0;
        private int ticksSinceLastSearched = 0;
        private LocalTargetInfo target = LocalTargetInfo.Invalid;                   
        
        public override LocalTargetInfo CurrentTarget => target;

        public Building_CiwsGunCE() : base()
        {            
        }

        public override void Tick()
        {            
            if (!target.IsValid || ticksSinceLastSearched != -1)
            {
                ticksSinceLastSearched = 0;
                if (TryFindNewTarget())
                {
                    shotsFired = 0;
                    shotsRequired = Rand.Range(MINSHOTSREQUIRED, MAXSHOTSREQUIRED);
                }
            }
            if(target.IsValid && !target.ThingDestroyed)
            {
                if(target.Thing == null || !target.Thing.Spawned)
                {
                    Reset();
                }
                else
                {
                    BurstNow();
                }
            }
            this.top.TurretTopTick();            
        }

        protected override IEnumerable<Gizmo> GetTurretGizmos()
        {
            // Ammo gizmos
            if (CompAmmo != null && (PlayerControlled || Prefs.DevMode))
            {
                foreach (Command com in CompAmmo.CompGetGizmosExtra())
                {
                    if (!PlayerControlled && Prefs.DevMode && com is GizmoAmmoStatus)
                        (com as GizmoAmmoStatus).prefix = "DEV: ";

                    yield return com;
                }
            }
        }    

        private new bool TryFindNewTarget()
        {
            FlyOverProjectileTracker tracker = Map.GetComponent<FlyOverProjectileTracker>();
            if(tracker.ProjectilesCE.TryRandomElement(out ProjectileCE projectile) && !projectile.Destroyed)
            {
                target = new LocalTargetInfo(projectile);
                return true;
            }            
            return false;
        }            

        private void BurstNow()
        {
            int count = Rand.Range(1, 4);
            shotsFired += count;
            Vector3 drawPos = DrawPos;
            Vector3 targetPos = ((ProjectileCE) target.Thing).ExactPosition;            
            Vector2 origin = new Vector2(drawPos.x, drawPos.z);
            Vector2 destination = new Vector2(targetPos.x, targetPos.z);            
            Vector2 w = (destination - origin);
            float shotRotation = (-90 + Mathf.Rad2Deg * Mathf.Atan2(w.y, w.x)) % 360;
            float shotAngle = Mathf.Atan(targetPos.y / (Vector2.Distance(origin, destination) + 1e-5f));
            while (count-- > 0)
            {
                ProjectileCE projectile = (ProjectileCE)ThingMaker.MakeThing(CE_ThingDefOf.CIWS_Fake);
                projectile.Position = DrawPos.ToIntVec3();
                projectile.SpawnSetup(Map, false);
;               projectile.Launch(this, origin, shotAngle, shotRotation, 1.0f, Rand.Range(400, 600), null);
            }
        }

        private void Reset()
        {
            target = LocalTargetInfo.Invalid;
            ticksSinceLastSearched = 0;
            shotsFired = 0;
            shotsRequired = 0;            
        }
    }
}

