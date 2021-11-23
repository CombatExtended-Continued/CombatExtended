using System;
using ProjectileImpactFX;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatExtended
{
    public class ProjectileCE_Fake : ProjectileCE
    {       
        public override void Tick()
        {
            base.Tick();
            if (landed)
            {
                Destroy();
                return;
            }
            LastPos = ExactPosition;
            ticksToImpact--;
            if (!ExactPosition.InBounds(Map))
            {                
                Destroy();
                return;
            }           
            Position = ExactPosition.ToIntVec3();           
            if (ticksToImpact == 60 && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal && def.projectile.soundImpactAnticipate != null)
            {
                def.projectile.soundImpactAnticipate.PlayOneShot(this);
            }
            //TODO : It appears that the final steps in the arc (past ticksToImpact == 0) don't CheckForCollisionBetween.
            if (ticksToImpact <= 0)
            {
                Destroy();
                return;
            }
            if (ambientSustainer != null)
            {
                ambientSustainer.Maintain();
            }
            if (def.HasModExtension<TrailerProjectileExtension>())
            {
                var trailer = def.GetModExtension<TrailerProjectileExtension>();
                if (trailer != null)
                {
                    if (ticksToImpact % trailer.trailerMoteInterval == 0)
                    {
                        for (int i = 0; i < trailer.motesThrown; i++)
                        {
                            TrailThrower.ThrowSmoke(DrawPos, trailer.trailMoteSize, Map, trailer.trailMoteDef);
                        }
                    }
                }
            }            
        }

        public override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);
            Destroy();
        }
    }
}

