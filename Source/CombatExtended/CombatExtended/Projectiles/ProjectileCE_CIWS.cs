using Mono.Unix.Native;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatExtended
{
    public class ProjectileCE_CIWS : ProjectileCE
    {
        #region Caching
        protected static Dictionary<Map, List<ProjectileCE_CIWS>> CIWSProjectiles = new Dictionary<Map, List<ProjectileCE_CIWS>>();
        public static IEnumerable<ProjectileCE_CIWS> ProjectilesAt(Map map)
        {
            CIWSProjectiles.TryGetValue(map, out var targets);
            return targets ?? Enumerable.Empty<ProjectileCE_CIWS>();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (map == null)
            {
                return;
            }
            if (!CIWSProjectiles.TryGetValue(map, out var targetList))
            {
                CIWSProjectiles[map] = targetList = new List<ProjectileCE_CIWS>();
            }
            targetList.Add(this);
        }
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = Map;
            base.DeSpawn(mode);
            if (CIWSProjectiles.TryGetValue(map, out var targetList))
            {
                targetList.Remove(this);
                if (targetList.Count <= 0)
                {
                    CIWSProjectiles.Remove(map);
                }
            }
        }
        #endregion

        public virtual float CollideDistance => (def.projectile as ProjectilePropertiesCE)?.collideDistance ?? 1f;
        public virtual float ImpactChance => (def.projectile as ProjectilePropertiesCE)?.impactChance ?? 1f;
        protected override bool ShouldCollideWithSomething => ExactPosition.y <= 0f;

        public override Quaternion ExactRotation => DrawRotation;
        public override void Tick()
        {
            if (!(TrajectoryWorker is BallisticsTrajectoryWorker) && intendedTarget.ThingDestroyed)
            {
                if (shotAngle > 0.7853f) // 45 degrees
                {
                    Destroy(DestroyMode.Vanish);
                    return;
                }
            }
            ticksToImpact++; //do not allow it hit zero
            base.Tick();
            TryCollideWith(intendedTargetThing);
        }
        protected override bool CanCollideWith(Thing thing, out float dist)
        {
            dist = 0f;
            if (thing.Destroyed)
            {
                return false;
            }
            if (!Rand.Chance(ImpactChance))
            {
                return false;
            }
            var ciwsTargetCompResult = thing.TryGetComp<CompCIWSTarget>()?.CanCollideWith(this, out dist);
            if (ciwsTargetCompResult != null)
            {
                return ciwsTargetCompResult.Value;
            }
            dist = (thing.DrawPos.Yto0() - this.DrawPos.Yto0()).MagnitudeHorizontalSquared();
            var collideDistance = CollideDistance;
            if (dist < collideDistance * collideDistance)
            {
                dist = Mathf.Sqrt(dist);
                return true;
            }
            return false;

        }

        public override void Impact(Thing hitThing)
        {
            hitThing?.TryGetComp<CompCIWSImpactHandler>()?.OnImpact(this, DamageInfo);
            base.Impact(hitThing);
        }
        protected override bool CheckForCollisionBetween()
        {
            return false;
        }
    }
}
