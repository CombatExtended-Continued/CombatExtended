using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Rimatomics;
using static UnityEngine.UI.CanvasScaler;
using static UnityEngine.UI.Image;
using System.Security.Cryptography;

namespace CombatExtended
{
    public class ProjectilePropertiesCE : ProjectileProperties
    {
        public TravelingShellProperties shellingProps;

        // public float armorPenetration = 0;
        public int pelletCount = 1;
        public float spreadMult = 1;
        public List<SecondaryDamage> secondaryDamage = new List<SecondaryDamage>();
        public bool damageAdjacentTiles = false;
        public bool dropsCasings = false;
        public string casingMoteDefname = "Fleck_EmptyCasing";
        public string casingFilthDefname = "Filth_RifleAmmoCasings";
        public float gravityFactor = 1;
        public bool isInstant = false;
        public bool damageFalloff = true; // Damage falloff for *instant* projectiles.
        public float armorPenetrationSharp;
        public float armorPenetrationBlunt;
        public bool castShadow = true;

        public float suppressionFactor = 1;
        public float airborneSuppressionFactor = 1;
        public float dangerFactor = 1;

        public FloatRange ballisticCoefficient = new FloatRange(1f, 1f);
        public FloatRange mass = new FloatRange(1f, 1f);
        public FloatRange diameter = new FloatRange(1f, 1f);

        public bool lerpPosition = true;
        public ThingDef detonateMoteDef;
        public FleckDef detonateFleckDef;
        public float detonateEffectsScaleOverride = -1;
        public int? tickToTruePos;
        //If undefined, use 3 or the tick it needed to cover 10 cells, whichever is larger.
        public int TickToTruePos => tickToTruePos ?? Mathf.Max(3, Mathf.CeilToInt(600 / speed));
        [MustTranslate]
        public string genericLabelOverride = null;

        #region Bunker Buster fields
        /// <summary>
        /// Amount of tiles ProjectileCE_BunkerBuster will detonate after after penetrating an obstacle
        /// </summary>
        public int fuze_delay = 2;

        public bool HP_penetration = false;

        public float HP_penetration_ratio = 1f;

        #endregion

        public int armingDelay = 0;
        public float aimHeightOffset = 0;

        public float empShieldBreakChance = 1f;
        public float collideDistance = 1f;
        public float impactChance = 1f;

        public float Gravity => CE_Utility.GravityConst * gravityFactor;
        public ThingDef CIWSVersion;
        #region Moving methods
        public virtual Vector2 Vec2Position(Vector2 origin, Vector2 destination, float startingTicksToImpact, int ticks)
        {
            return Vector2.Lerp(origin, destination, ticks / startingTicksToImpact);
        }
        public Vector3 MoveForward(float shotRotation,
            float shotAngle,
            Vector2 origin,
            Vector2 destination,
            float startingTicksToImpact,
            float shotHeight,
            ref bool kinit,
            ref Vector3 velocity,
            ref float shotSpeed,
            ref Vector3 curPosition,
            ref float mass,
            ref float ballisticCoefficient,
            ref float radius,
            ref float gravity,
            ref float initialSpeed,
            ref int flightTicks)
        {
            flightTicks++;
            if (lerpPosition)
            {
                return LerpedMoveForward(origin, destination, startingTicksToImpact, shotHeight, shotSpeed, shotAngle, flightTicks);
            }
            else
            {
                return NonLerpedMoveForward(shotRotation, shotAngle, ref kinit, ref velocity, ref shotSpeed, ref curPosition, ref mass, ref ballisticCoefficient, ref radius, ref gravity, ref initialSpeed);
            }

        }
        protected Vector3 LerpedMoveForward(Vector2 origin, Vector2 destination, float startingTicksToImpact, float shotHeight, float shotSpeed, float shotAngle, int ticks)
        {
            var v = Vec2Position(origin, destination, startingTicksToImpact, ticks);
            return new Vector3(v.x, GetHeightAtTicks(shotHeight, shotSpeed, shotAngle, ticks), v.y);

        }
        protected virtual Vector3 NonLerpedMoveForward(float shotRotation, float shotAngle, ref bool kinit, ref Vector3 velocity, ref float shotSpeed, ref  Vector3 curPosition, ref float mass, ref float ballisticCoefficient, ref float radius, ref float gravity, ref float initialSpeed)
        {
            if (!kinit)
            {
                float sr = shotRotation * Mathf.Deg2Rad + 3.14159f / 2.0f;
                kinit = true;
                ballisticCoefficient = this.ballisticCoefficient.RandomInRange;
                mass = this.mass.RandomInRange;
                radius = diameter.RandomInRange / 2000;
                gravity = Gravity;
                float sspt = shotSpeed / GenTicks.TicksPerRealSecond;
                velocity = new Vector3(Mathf.Cos(sr) * Mathf.Cos(shotAngle) * sspt, Mathf.Sin(shotAngle) * sspt, Mathf.Sin(sr) * Mathf.Cos(shotAngle) * sspt);
                initialSpeed = sspt;
            }
            Vector3 newPosition = curPosition + velocity;
            Accelerate(radius, ballisticCoefficient, mass, gravity, ref velocity, ref shotSpeed);
            return newPosition;
        }

        protected virtual void Accelerate(float radius, float ballisticCoefficient, float mass, float gravity, ref Vector3 velocity, ref float shotSpeed)
        {
            AffectedByDrag(radius, shotSpeed, ballisticCoefficient, mass, ref velocity);
            AffectedByGravity(gravity, ref velocity);
        }

        protected void AffectedByGravity(float gravity, ref Vector3 velocity)
        {
            velocity.y -= gravity / GenTicks.TicksPerRealSecond;
        }

        protected void AffectedByDrag(float radius, float shotSpeed, float ballisticCoefficient, float mass, ref Vector3 velocity)
        {
            float crossSectionalArea = radius;
            crossSectionalArea *= crossSectionalArea * 3.14159f;
            // 2.5f is half the mass of 1m² x 1cell of air.
            var q = 2.5f * shotSpeed * shotSpeed;
            var dragForce = q * crossSectionalArea / ballisticCoefficient;
            // F = mA
            // A = F / m
            var a = (float)-dragForce / mass;
            var normalized = velocity.normalized;
            velocity.x += a * normalized.x;
            velocity.y += a * normalized.y;
            velocity.z += a * normalized.z;
        }
        protected float GetHeightAtTicks(float shotHeight, float shotSpeed, float shotAngle, int ticks)
        {
            var seconds = ((float)ticks) / GenTicks.TicksPerRealSecond;
            return (float)Math.Round(shotHeight + shotSpeed * Mathf.Sin(shotAngle) * seconds - (Gravity * seconds * seconds) / 2f, 3);
        }

        public virtual IEnumerable<Vector3> NextPositions(float shotRotation,
            float shotAngle,
            Vector2 origin,
            Vector2 destination,
            float ticksToImpact,
            float shotHeight,
            bool kinit,
            Vector3 velocity,
            float shotSpeed,
            Vector3 curPosition,
            float mass,
            float ballisticCoefficient,
            float radius,
            float gravity,
            float initialSpeed,
            int flightTicks)
        {
            for (; ticksToImpact >= 0; ticksToImpact--)
            {
                yield return MoveForward(shotRotation, shotAngle, origin, destination, ticksToImpact, shotHeight, ref kinit, ref velocity, ref shotSpeed, ref curPosition, ref mass, ref ballisticCoefficient, ref radius, ref gravity, ref initialSpeed, ref flightTicks);
            }
        }
        #endregion
    }
}
