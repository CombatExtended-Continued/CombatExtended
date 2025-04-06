using System;
using System.Linq;
using Verse;
using RimWorld;
using CombatExtended.AI;

using System.Collections.Generic;
using System.Text;

using Verse.AI;
using Verse.Grammar;
using UnityEngine;
using CombatExtended.Utilities;

namespace CombatExtended
{
    public class Verb_ThrowGrenade : Verb_ShootCEOneUse
    {
        private ShootLine _line;
        private ShootLine _direct;
        private bool hasLine = false;
        private List<IntVec3> ringDrawCells = null;
        private int lastCacheTick = 0;

        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            var r = base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
            if (r)
            {
                this.CasterPawn.Drawer.Notify_WarmingCastAlongLine(_direct, this.caster.Position);
            }
            return r;
        }

        public override bool TryFindCEShootLineFromTo(IntVec3 root, LocalTargetInfo targ, out ShootLine resultingLine, out Vector3 targetPos)
        {
            hasLine = base.TryFindCEShootLineFromTo(root, targ, out _line, out targetPos);
            _direct = resultingLine = new ShootLine(root, targ.Cell);
            return hasLine;
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            if (target.IsValid && this.CanHitTarget(target))
            {
                GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
                base.DrawHighlightFieldRadiusAroundTarget(target);
            }
            int thisTick = Find.TickManager.TicksAbs;
            if (thisTick != lastCacheTick)
            {
                GenDraw.DrawRadiusRing(this.caster.Position, this.EffectiveRange, Color.white, (IntVec3 c) => this.CanHitTarget(c));
                ringDrawCells = new List<IntVec3>(GenDraw.ringDrawCells);
                lastCacheTick = thisTick;
            }
            else
            {
                GenDraw.DrawFieldEdges(ringDrawCells, Color.white, null);
            }
        }

        public override float EffectiveRange
        {
            get
            {
                var ar = this.verbProps.AdjustedRange(this, this.CasterPawn);
                var manip = ShooterPawn?.health?.capacities?.GetLevel(PawnCapacityDefOf.Manipulation) ?? 1.0f;
                return ar * manip;
            }
        }

        private bool FindAngle(LocalTargetInfo target, out float smokeDensity, out bool roofed, out float launchAngle, out float velocity, out int ticks)
        {
            Map map = caster.Map;
            Thing targetThing = target.Thing;
            roofed = false;

            smokeDensity = 0;
            launchAngle = 0f;
            velocity = -1f;
            ticks = 0;
            // Iterate through all cells on line of sight and check for cover and smoke
            var cells = GenSightCE.AllPointsOnLineOfSight(target.Cell, caster.Position);

            int endCell = cells.Count;
            float X;
            if (targetThing != null)
            {
                var proj = (targetThing.DrawPos - caster.TrueCenter());
                proj.y = 0;
                X = proj.magnitude;
            }

            else
            {
                var tc = caster.TrueCenter();
                X = new Vector2(tc.x - target.Cell.x, tc.z - target.Cell.z).magnitude;
            }
            //TODO: Allow different gravity for SOS2 and similar
            float gravity = 1.96f / GenTicks.TicksPerRealSecond;

            for (int i = 0; i < endCell; i++)
            {
                var cell = cells[i];

                // Check for smoke
                var gas = cell.GetGas(map);
                // TODO 1.4: Figure out how the new hardcoded gas system will work for our smoke and custom gases
                if (cell.AnyGas(map, GasType.BlindSmoke))
                {
                    smokeDensity += GasUtility.BlindingGasAccuracyPenalty;
                }
                roofed = roofed || map.roofGrid.RoofAt(cell) != null;

                Pawn pawn = cell.GetFirstPawn(map);
                if ((pawn == null ? cell.GetCover(map) : pawn) is Thing cover)
                {
                    if (targetThing == null || !cover.Equals(targetThing)) // Something is in the way, so we calculate the minimum angle and velocity needed to clear it.
                    {
                        bool midway = i >= endCell / 2;
                        float p_x = (midway ? cover.TrueCenter().y + 0.5f : cover.TrueCenter().y - 0.5f);
                        float p_y = new CollisionVertical(cover).Max * 1.1f;
                        float a = p_y / (p_x * (p_x - X));
                        float b = -a * X;
                        float vertex_x = -b / (2 * a);
                        float vertex_y = a * vertex_x*vertex_x + b * vertex_x;
                        float t = Mathf.Sqrt(2 * vertex_y / gravity);
                        float theta = Mathf.Atan((4 * vertex_y) / X);
                        float v0 = X / (2 * t * Mathf.Cos(theta));
                        if (theta > launchAngle) // If the needed angle for /this/ cover is steeper than the cached angle, use the new angle.
                        {
                            launchAngle = theta;
                            velocity = v0;
                        }
                    }
                }

            }
            float manip = ShooterPawn?.health?.capacities?.GetLevel(PawnCapacityDefOf.Manipulation) ?? 1.0f;
            if (velocity == -1) //No cover required adjusting the angle up, so line-drive it straight, or lob it if we need the distance
            {
                float v0 = ShotSpeed * manip / GenTicks.TicksPerRealSecond * 3;
                float target_height = 0;
                if (targetThing != null) {
                    var cv = new CollisionVertical(targetThing);
                    target_height = (cv.HeightRange.max + cv.HeightRange.min) / 2;
                }
                float H_offset = target_height - ShotHeight;
                float discriminant = v0*v0*v0*v0 - gravity * (gravity * X*X + 2 * H_offset * v0*v0);
                if (discriminant < 0) // At max speed, and optimal angle, we can't reach the target
                {
                    return false;
                }

                float positive_angle = Mathf.Atan((v0*v0 + Mathf.Sqrt(discriminant)) / (gravity * X));
                float negative_angle = Mathf.Atan((v0*v0 - Mathf.Sqrt(discriminant)) / (gravity * X));

                launchAngle = Mathf.Min(positive_angle, negative_angle);
                velocity = v0;
            }

            if (velocity > ShotSpeed * manip) // Required velocity is higher than we can throw.
            {
                return false;
            }
            ticks = (int)(X / (Mathf.Cos(launchAngle) * velocity)) + 1;
            ProjectilePropertiesCE pprop = Projectile.projectile as ProjectilePropertiesCE;
            ticks = Rand.RangeInclusive(ticks, pprop.explosionDelay);
            //TODO: The pawn should delay ticks equal difference between this value and the default grenade detonation delay, to properly simulate cooking grenades.
            return true;
        }

        public override bool TryCastShot()
        {
            float smokeDensity;
            bool roofed;
            float launchAngle;
            float velocity;
            int ticks;
            if (!FindAngle(currentTarget, out smokeDensity, out roofed, out launchAngle, out velocity, out ticks))
            {
                return false;
            }
            Vector3 _;
            base.TryFindCEShootLineFromTo(caster.Position, currentTarget, out _line, out _);

            ProjectilePropertiesCE pprop = Projectile.projectile as ProjectilePropertiesCE;
            this.CasterPawn.Drawer.Notify_WarmingCastAlongLine(_line, this.caster.Position);
            this.CasterPawn.Drawer.leaner.leanOffsetCurPct = 1.0f;
            ProjectileCE_Explosive projectile = (ProjectileCE_Explosive)ThingMaker.MakeThing(Projectile, null);
            Vector3 u = caster.TrueCenter();
            sourceLoc.Set(u.x, u.z);

            GenSpawn.Spawn(projectile, u.ToIntVec3(), caster.Map);


            var disp = (_line.Dest - _line.Source);

            float targetDistance = Mathf.Sqrt(disp.x * disp.x + disp.z * disp.z);

            projectile.minCollisionDistance = GetMinCollisionDistance(targetDistance);
            projectile.intendedTarget = currentTarget;
            projectile.mount = caster.Position.GetThingList(caster.Map).FirstOrDefault(t => t is Pawn && t != caster);

            float Y_0 = ShotHeight;
            float V_x = Mathf.Cos(launchAngle) * velocity;
            float time = targetDistance / V_x;
            projectile.ticksToDetonation = ticks;

            float t2 = time * time;
            float V_y = Mathf.Sin(launchAngle) * velocity;

            Vector3 heading = new Vector3(disp.x / targetDistance * V_x, V_y, disp.z / targetDistance * V_x);

            projectile.Throw(
                              Shooter,    //Shooter instead of caster to give turret operators' records the damage/kills obtained
                              new Vector3(sourceLoc.x, Y_0, sourceLoc.y),
                              heading,
                              EquipmentSource);

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
            this.CasterPawn.Drawer.Notify_WarmingCastAlongLine(_direct, this.caster.Position);
            lastShotTick = Find.TickManager.TicksGame;
            this.SelfConsume();
            return true;

        }

        public override void Notify_EquipmentLost()
        {
            if (this.state == VerbState.Bursting && this.burstShotsLeft < this.verbProps.burstShotCount)
            {
                this.SelfConsume();
            }
        }
        private void SelfConsume()
        {
            var inventory = ShooterPawn?.TryGetComp<CompInventory>();
            if (!this.EquipmentSource?.Destroyed ?? false)
            {
                this.EquipmentSource.Destroy(DestroyMode.Vanish);
            }
            if (inventory != null && ShooterPawn?.jobs.curJob?.def != CE_JobDefOf.OpportunisticAttack)
            {
                var newGun = inventory.rangedWeaponList?.FirstOrDefault(t => t.def == EquipmentSource?.def);
                if (newGun != null)
                {
                    inventory.TrySwitchToWeapon(newGun);
                }
                else
                {
                    inventory.SwitchToNextViableWeapon();
                }
            }
        }
    }
}
