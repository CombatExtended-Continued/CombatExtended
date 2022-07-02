using System;
using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using CombatExtended.Utilities;

namespace CombatExtended
{
    //Cloned from vanilla, completely unmodified
    public class ProjectileCE_Explosive : ProjectileCE
    {
        private int ticksToDetonation;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.ticksToDetonation, "ticksToDetonation", 0, false);
        }
        public override void Tick()
        {
            base.Tick();
            if (this.ticksToDetonation > 0)
            {
                this.ticksToDetonation--;
                if (this.ticksToDetonation <= 0)
                {
                    //Explosions are all handled in base
                    base.Impact(null);
                    return;
                }
                if (landed)
                {
                    if (def.projectile.damageDef.harmsHealth)
                    {
                        var suppressThings = new List<Pawn>();
                        suppressThings.AddRange(ExactPosition.ToIntVec3().PawnsInRange(Map,
                            SuppressionRadius + def.projectile.explosionRadius + (def.projectile.applyDamageToExplosionCellsNeighbors ? 2.2f : 0f)));
                        foreach (var thing in suppressThings)
                            ApplySuppression(thing, 1f - (ticksToDetonation / def.projectile.explosionDelay), true);
                    }
                }
            }
        }
        public override void Impact(Thing hitThing)
        {
            // Snap to target so we hit multi-tile pawns with our explosion
            if (hitThing is Pawn)
            {
                var newPos = hitThing.DrawPos;
                newPos.y = ExactPosition.y;
                ExactPosition = newPos;
                Position = ExactPosition.ToIntVec3();
            }
            if (def.projectile.explosionDelay == 0)
            {
                //Explosions are all handled in base
                base.Impact(null);
                return;
            }
            landed = true;
            ticksToDetonation = def.projectile.explosionDelay;
            if (def.projectile.damageDef.harmsHealth)
            {
                var dangerAmount = def.projectile.damageAmountBase * dangerAmountFactor + ticksToDetonation;
                var explosionSuppressionRadius = def.projectile.explosionRadius + SuppressionRadius + (def.projectile.applyDamageToExplosionCellsNeighbors ? 2.2f : 0f);
                DangerTracker.Notify_DangerRadiusAt(Position,
                    Math.Max(explosionSuppressionRadius * Mathf.Sqrt(dangerAmount * SuppressionUtility.dangerAmountFactor) *
                        0.2f, explosionSuppressionRadius) + 0.9f,
                    dangerAmount);
                GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, this.def.projectile.damageDef, this.launcher?.Faction);
            }
        }
    }
}
