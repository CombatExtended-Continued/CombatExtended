using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatExtended
{
    public class FleckSystem_Casing : FleckSystemBase<Fleck_Casing>
    {
    }
    public struct Fleck_Casing : IFleck
    {
        public FleckStatic baseData;

        public float airTimeLeft;

        public Vector3 velocity;

        public float rotationRate;

        public float delay;

        public bool Flying => airTimeLeft > 0f;

        public Vector3 Velocity
        {
            get
            {
                return velocity;
            }
            set
            {
                velocity = value;
            }
        }

        public float MoveAngle
        {
            get
            {
                return velocity.AngleFlat();
            }
            set
            {
                SetVelocity(value, Speed);
            }
        }

        public float Speed
        {
            get
            {
                return velocity.MagnitudeHorizontal();
            }
            set
            {
                if (value == 0f)
                {
                    velocity = Vector3.zero;
                }
                else if (velocity == Vector3.zero)
                {
                    velocity = new Vector3(value, 0f, 0f);
                }
                else
                {
                    velocity = velocity.normalized * value;
                }
            }
        }

        public void Setup(FleckCreationData creationData)
        {
            baseData = default(FleckStatic);
            baseData.Setup(creationData);
            airTimeLeft = creationData.airTimeLeft ?? 999999f;
            baseData.position.worldPosition += creationData.def.attachedDrawOffset;
            rotationRate = creationData.rotationRate;
            SetVelocity(creationData.velocityAngle, creationData.velocitySpeed);
            if (creationData.velocity.HasValue)
            {
                velocity += creationData.velocity.Value;
            }
        }

        public bool TimeInterval(float deltaTime, Map map)
        {
            if (baseData.TimeInterval(deltaTime, map))
            {
                return true;
            }
            if (!Flying)
            {
                return false;
            }
            Vector3 vector = NextExactPosition(deltaTime);
            IntVec3 intVec = new IntVec3(vector);
            if (intVec != new IntVec3(baseData.position.ExactPosition))
            {
                if (!intVec.InBounds(map))
                {
                    return true;
                }
                if (baseData.def.collide && intVec.Filled(map))
                {
                    WallHit();
                    return false;
                }
            }
            baseData.position.worldPosition = vector;
            if (baseData.def.speedPerTime != FloatRange.Zero)
            {
                Speed = Mathf.Max(Speed + baseData.def.speedPerTime.RandomInRange * deltaTime, 0f);
            }
            if (airTimeLeft > 0f)
            {
                if (baseData.def.rotateTowardsMoveDirection && velocity != default(Vector3))
                {
                    baseData.exactRotation = velocity.AngleFlat() + baseData.def.rotateTowardsMoveDirectionExtraAngle;
                }
                else
                {
                    baseData.exactRotation += rotationRate * deltaTime;
                }
                velocity += baseData.def.acceleration * deltaTime;
                airTimeLeft -= deltaTime;
                if (airTimeLeft < 0f)
                {
                    airTimeLeft = 0f;
                }
                if (airTimeLeft <= 0f && !baseData.def.landSound.NullOrUndefined())
                {
                    baseData.def.landSound.PlayOneShot(new TargetInfo(new IntVec3(baseData.position.ExactPosition), map));
                }
            }
            return false;
        }

        private Vector3 NextExactPosition(float deltaTime)
        {
            return baseData.position.ExactPosition + velocity * deltaTime;
        }

        public void SetVelocity(float angle, float speed)
        {
            velocity = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * speed;
        }

        public void Draw(DrawBatch batch)
        {
            baseData.Draw(batch);
        }

        private void WallHit()
        {
            airTimeLeft = 0f;
            Speed = 0f;
            rotationRate = 0f;
        }
    }
}
