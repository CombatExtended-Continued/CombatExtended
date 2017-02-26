using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace CombatExtended
{
    static class CE_Utility
    {
        #region Misc

        public static List<ThingDef> allWeaponDefs = new List<ThingDef>();

        /// <summary>
        /// Generates a random Vector2 in a circle with given radius
        /// </summary>
        public static Vector2 GenRandInCircle(float radius)
        {
            //Fancy math to get random point in circle
            System.Random rand = new System.Random();
            double angle = rand.NextDouble() * Math.PI * 2;
            double range = Math.Sqrt(rand.NextDouble()) * radius;
            return new Vector2((float)(range * Math.Cos(angle)), (float)(range * Math.Sin(angle)));
        }

        /// <summary>
        /// Calculates the actual current movement speed of a pawn
        /// </summary>
        /// <param name="pawn">Pawn to calculate speed of</param>
        /// <returns>Move speed in cells per second</returns>
        public static float GetMoveSpeed(Pawn pawn)
        {
            float movePerTick = 60 / pawn.GetStatValue(StatDefOf.MoveSpeed, false);    //Movement per tick
            movePerTick +=  pawn.Map.pathGrid.CalculatedCostAt(pawn.Position, false, pawn.Position);
            Building edifice = pawn.Position.GetEdifice(pawn.Map);
            if (edifice != null)
            {
                movePerTick += (int)edifice.PathWalkCostFor(pawn);
            }

            //Case switch to handle walking, jogging, etc.
            if (pawn.CurJob != null)
            {
                switch (pawn.CurJob.locomotionUrgency)
                {
                    case LocomotionUrgency.Amble:
                        movePerTick *= 3;
                        if (movePerTick < 60)
                        {
                            movePerTick = 60;
                        }
                        break;
                    case LocomotionUrgency.Walk:
                        movePerTick *= 2;
                        if (movePerTick < 50)
                        {
                            movePerTick = 50;
                        }
                        break;
                    case LocomotionUrgency.Jog:
                        break;
                    case LocomotionUrgency.Sprint:
                        movePerTick = Mathf.RoundToInt(movePerTick * 0.75f);
                        break;
                }
            }
            return 60 / movePerTick;
        }

        /// <summary>
        /// Attempts to find a turret operator. Accepts any Thing as input and does a sanity check to make sure it is an actual turret.
        /// </summary>
        /// <param name="thing">The turret to check for an operator</param>
        /// <returns>Turret operator if one is found, null if not</returns>
        public static Pawn TryGetTurretOperator(Thing thing)
        {
            Pawn manningPawn = null;
            Building_TurretGun turret = thing as Building_TurretGun;
            if (turret != null)
            {
                CompMannable comp = turret.TryGetComp<CompMannable>();
                if (comp != null && comp.MannedNow)
                {
                    manningPawn = comp.ManningPawn;
                }
            }
            return manningPawn;
        }

        #endregion Misc

        #region MoteThrower
        public static void ThrowEmptyCasing(Vector3 loc, Map map, ThingDef casingMoteDef, float size = 1f)
        {
            if (!ModSettings.showCasings || !loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(casingMoteDef, null);
            moteThrown.Scale = Rand.Range(0.5f, 0.3f) * size;
            moteThrown.exactRotation = Rand.Range(-3f, 4f);
            moteThrown.exactPosition = loc;
            moteThrown.airTimeLeft = 60;
            moteThrown.SetVelocity((float)Rand.Range(160, 200), Rand.Range(0.7f, 0.5f));
            //     moteThrown.SetVelocityAngleSpeed((float)Rand.Range(160, 200), Rand.Range(0.020f, 0.0115f));
            GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }
        #endregion

        #region Physics

        public const float gravityConst = 9.8f;
        public const float collisionHeightFactor = 1.0f;    // Global collision height multiplier
        public const float treeCollisionHeight = 5f;        // Trees are this tall
        public const float bodyRegionBottomHeight = 0.45f;  // Hits below this percentage will impact the corresponding body region
        public const float bodyRegionMiddleHeight = 0.85f;
        /*public static float GetCollisionHeight(Thing thing)
        {
            if (thing == null)
            {
                return 0;
            }
            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                float collisionHeight = pawn.BodySize;
                if (!humanoidBodyList.Contains(pawn.def.race.body.defName)) collisionHeight *= 0.5f;
                if (pawn.GetPosture() != PawnPosture.Standing)
                {
                    collisionHeight = pawn.BodySize > 1 ? pawn.BodySize - 0.8f : 0.2f * pawn.BodySize;
                }
                return collisionHeight * collisionHeightFactor;
            }
            return thing.def.fillPercent * collisionHeightFactor;
        }*/
        /// <summary>
        /// Returns the vertical collision box of a Thing
        /// </summary>
        /// <param name="isEdifice">False by default. Set to true if thing is the edifice at the location thing.Position.</param>
        public static FloatRange GetCollisionVertical(Thing thing, bool isEdifice = false)
        {
            if (thing == null)
            {
            	return new FloatRange(0f, 0f);
            }
            if (isEdifice)
            {
                if (thing.def.category == ThingCategory.Plant && thing.def.altitudeLayer == AltitudeLayer.Building) return new FloatRange(0, treeCollisionHeight * collisionHeightFactor);    // Check for trees
            	return new FloatRange(0f, thing.def.fillPercent * collisionHeightFactor);
            }
            float collisionHeight = 0f;
            var pawn = thing as Pawn;
            if (pawn != null)
            {
                collisionHeight = pawn.BodySize * GetCollisionBodyFactors(pawn).Second;
                if (pawn.GetPosture() != PawnPosture.Standing)
                {
                    collisionHeight = pawn.BodySize > 1 ? pawn.BodySize - 0.8f : 0.2f * pawn.BodySize;
                }
            }
            else
            {
            	collisionHeight = thing.def.fillPercent;
            }
            var edificeHeight = 0f;
            if (thing.Map != null)
            {
                var edifice = thing.Position.GetEdifice(thing.Map);
                if (edifice != null && edifice.GetHashCode() != thing.GetHashCode())
                {
                    edificeHeight = GetCollisionVertical(edifice, true).max;
                }
            }
            FloatRange floatRange = new FloatRange(edificeHeight, edificeHeight + collisionHeight * collisionHeightFactor);
            return floatRange;
        }

        /// <summary>
        /// Calculates the width of an object for purposes of bullet collision. Return value is distance from center of object to its edge in cells, so a wall filling out an entire cell has a width of 0.5.
        /// Also accounts for general body type, humanoids must be specified in the humanoidBodyList and will have reduced width relative to their overall body size.
        /// </summary>
        /// <param name="thing">The Thing to measure width of</param>
        /// <returns>Distance from center of Thing to its edge in cells</returns>
        public static float GetCollisionWidth(Thing thing)
        {
            Pawn pawn = thing as Pawn;
            if (pawn == null)
            {
                return 0.5f;    //Buildings, etc. fill out half a square to each side
            }
            return pawn.BodySize * GetCollisionBodyFactors(pawn).First;
        }

        /// <summary>
        /// Calculates body scale factors based on body type
        /// </summary>
        /// <param name="pawn">Which pawn to measure for</param>
        /// <returns>Width factor as First, height factor as second</returns>
        private static Pair<float, float> GetCollisionBodyFactors(Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Error("CE calling GetCollisionBodyHeightFactor with nullPawn");
                return new Pair<float, float>(1, 1);
            }
            BodyType type = BodyType.Undefined;
            RaceProperties_CE props = pawn.def.race as RaceProperties_CE;
            if (props != null)
            {
                type = props.bodyType;
            }
#if DEBUG
            if (type == BodyType.Undefined) Log.ErrorOnce("CE returning BodyType Undefined for pawn " + pawn.ToString(),  35000198 + pawn.GetHashCode());
#endif
            switch (type)
            {
                case BodyType.Humanoid:
                    return new Pair<float, float>(0.25f, 1);
                case BodyType.Quadruped:
                    return new Pair<float, float>(0.5f, 0.5f);
                case BodyType.QuadrupedLow:
                    return new Pair<float, float>(0.5f, 0.25f);
                case BodyType.Serpentine:
                    return new Pair<float, float>(0.5f, 0.125f);
                case BodyType.Birdlike:
                    return new Pair<float, float>(0.5f, 0.75f);
                case BodyType.Monkeylike:
                    return new Pair<float, float>(0.25f, 0.5f);
                default:
                    return new Pair<float, float>(1, 1);
            }
        }

        /// <summary>
        /// Calculates the BodyPartHeight based on how high a projectile was at time of collision with a pawn.
        /// </summary>
        /// <param name="thing">The Thing to check impact height on. Returns Undefined for non-pawns.</param>
        /// <param name="projectileHeight">The height of the projectile at time of impact.</param>
        /// <returns></returns>
        public static BodyPartHeight GetCollisionBodyHeight(Thing thing, float projectileHeight)
        {
            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                FloatRange pawnHeight = GetCollisionVertical(thing);
                if (projectileHeight < pawnHeight.max * bodyRegionBottomHeight) return BodyPartHeight.Bottom;
                else if (projectileHeight < pawnHeight.max * bodyRegionMiddleHeight) return BodyPartHeight.Middle;
                return BodyPartHeight.Top;
            }
            return BodyPartHeight.Undefined;
        }

        #endregion Physics

        #region Inventory

        public static void TryUpdateInventory(Pawn pawn)
        {
            if (pawn != null)
            {
                CompInventory comp = pawn.TryGetComp<CompInventory>();
                if (comp != null)
                {
                    comp.UpdateInventory();
                }
            }
        }

        public static void TryUpdateInventory(Pawn_InventoryTracker tracker)
        {
            if (tracker != null && tracker.pawn != null)
            {
                TryUpdateInventory(tracker.pawn);
            }
        }

        #endregion Inventory
    }
}