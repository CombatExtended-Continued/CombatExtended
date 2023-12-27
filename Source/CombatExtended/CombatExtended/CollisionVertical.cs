using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using CombatExtended.Compatibility;

namespace CombatExtended
{
    public struct CollisionVertical
    {
        public const float ThickRoofThicknessMultiplier = 2f;
        public const float NaturalRoofThicknessMultiplier = 2f;
        public const float MeterPerCellHeight = 1.75f;
        public const float WallCollisionHeight = 2f;       // Walls are this tall
        public const float BodyRegionBottomHeight = 0.45f;  // Hits below this percentage will impact the corresponding body region
        public const float BodyRegionMiddleHeight = 0.85f;  // This also sets the altitude at which pawns hold their guns

        private FloatRange heightRange;
        public float shotHeight;

        public FloatRange HeightRange => heightRange;
        public float Min => heightRange.min;
        public float Max => heightRange.max;
        public float BottomHeight => Max * BodyRegionBottomHeight;
        public float MiddleHeight => Max * BodyRegionMiddleHeight;

        public CollisionVertical(Thing thing)
        {
            heightRange = new FloatRange(0, 0);
            shotHeight = 0f;
            CalculateHeightRange(thing);
        }

        private void CalculateHeightRange(Thing thing)
        {
            shotHeight = 0;
            heightRange.min = heightRange.max = 0f;
            if (thing == null)
            {
                return;
            }

            if (thing is Plant plant)
            {
                //Height matches up exactly with visual size
                heightRange.max = BoundsInjector.ForPlant(plant).y;
                return;
            }

            if (thing is Building)
            {
                if (thing is Building_Door door && door.Open)
                {
                    return;     //returns heightRange = (0,0) & shotHeight = 0. If not open, doors have FillCategory.Full so returns (0, WallCollisionHeight)
                }

                if (thing.def.Fillage == FillCategory.Full)
                {
                    heightRange.max = WallCollisionHeight;
                    shotHeight = WallCollisionHeight;
                    return;
                }
                float fillPercent = thing.def.fillPercent;
                heightRange.min = Mathf.Min(0f, fillPercent);
                heightRange.max = Mathf.Max(0f, fillPercent);
                shotHeight = fillPercent;
                return;
            }

            float heightAdjust = CETrenches.GetHeightAdjust(thing.Position, thing.Map);

            if (thing is Pawn pawn)
            {
                RecalculateHeight(pawn, heightAdjust);
                return;
            }
            float collisionHeight = thing.def.fillPercent;
            float fillPercent2 = collisionHeight;
            heightRange.min = Mathf.Min(0, collisionHeight) + heightAdjust;
            heightRange.max = Mathf.Max(0, collisionHeight) + heightAdjust;
            shotHeight = heightRange.max;
        }

        public void RecalculateHeight(Pawn pawn, float heightAdjust)
        {
            float collisionHeight = CE_Utility.GetCollisionBodyFactors(pawn).y;
            heightRange.min = heightAdjust;
            heightRange.max = heightAdjust + collisionHeight;
            float shotHeightOffset = shotHeight = collisionHeight * (1 - BodyRegionMiddleHeight);

            if (pawn.Downed)
            {
                collisionHeight = BodyRegionBottomHeight * BodyRegionBottomHeight * collisionHeight;
                heightRange.max = heightAdjust + collisionHeight;
                shotHeight = collisionHeight * (1 - BodyRegionMiddleHeight);
                return;
            }

            // Humanlikes in combat crouch to reduce their profile
            if (pawn.IsCrouching())
            {
                float crouchHeight = BodyRegionBottomHeight * collisionHeight;  // Minimum height we can crouch down to
                // check our stance if we are shooting over cover.
                Stance curStance = pawn.stances.curStance;
                LocalTargetInfo currentTarget = null;
                if (curStance is Stance_Warmup || curStance is Stance_Cooldown)
                {
                    currentTarget = ((Stance_Busy)curStance).focusTarg;
                }

                // Find the highest adjacent cover
                Map map = pawn.Map;

                if (currentTarget != null && currentTarget.IsValid)
                {
                    foreach (IntVec3 curCell in GenSight.PointsOnLineOfSight(pawn.Position, currentTarget.Cell))
                    {
                        Thing cover = curCell.GetCover(map);
                        if (cover != null && cover.def.Fillage == FillCategory.Partial && !cover.IsPlant())
                        {
                            var coverHeight = new CollisionVertical(cover).Max;
                            if (coverHeight > crouchHeight)
                            {
                                crouchHeight = coverHeight;
                            }
                        }
                        break;
                    }
                }
                collisionHeight = Mathf.Min(collisionHeight, crouchHeight + 0.01f + shotHeightOffset);  // We crouch down only so far that we can still shoot over our own cover and never beyond our own body size
                heightRange.min = heightAdjust;
                heightRange.max = heightAdjust + collisionHeight;
                shotHeight = Mathf.Min(heightRange.max * BodyRegionMiddleHeight, crouchHeight + 0.01f);
            }
        }

        /// <summary>
        /// Calculates the BodyPartHeight based on how high a projectile impacted in relation to overall pawn height.
        /// </summary>
        /// <param name="projectileHeight">The height of the projectile at time of impact.</param>
        /// <returns>BodyPartHeight between Bottom and Top.</returns>
        public BodyPartHeight GetCollisionBodyHeight(float projectileHeight)
        {
            if (projectileHeight < BottomHeight)
            {
                return BodyPartHeight.Bottom;
            }
            else if (projectileHeight < MiddleHeight)
            {
                return BodyPartHeight.Middle;
            }
            return BodyPartHeight.Top;
        }

        /* Alistaire: Part of code that targets Bottom too often
        public BodyPartHeight GetRandWeightedBodyHeightBelow(float threshold)
        {
            return GetCollisionBodyHeight(Rand.Range(Min, threshold));
        }*/
    }
}
