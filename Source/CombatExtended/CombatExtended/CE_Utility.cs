using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace CombatExtended
{
    static class CE_Utility
    {

        #region Blitting
        private const int blitMaxDimensions = 64;

        /// <summary>
        /// Code from https://gamedev.stackexchange.com/questions/92285/unity3d-resize-texture-without-corruption
        /// </summary>
        /// <param name="texture">Any texture with or without read-write protection</param>
        /// <param name="blitRect">The Rect to be extracted from the <i>rtSize</i>'d render of <i>texture</i> (.x+.width, .y+.height smaller than <i>rtSize</i>)</param>
        /// <param name="rtSize">The size that <i>texture</i> is to be rendered at</param>
        /// <returns>Texture2D of size <i>blitRect</i>.width, <i>blitRect</i>.height extracted from a <i>rtSize</i>[0] width, <i>rtSize</i>[1] height render of <i>texture</i> starting at position (<i>blitRect</i>.x, <i>blitRect</i>.y).</returns>
        public static Texture2D Blit(this Texture2D texture, Rect blitRect, int[] rtSize)
        {
            var prevFilterMode = texture.filterMode;
            texture.filterMode = FilterMode.Point;

            RenderTexture rt = RenderTexture
                .GetTemporary(rtSize[0],                        //render width
                           rtSize[1],                       //render height
                           0,                               //no depth buffer
                           RenderTextureFormat.Default,     //default (=automatic) color mode
                           RenderTextureReadWrite.Default,  //default (=automatic) r/w mode
                           1);                              //no anti-aliasing (1=none,2=2x,4=4x,8=8x)

            rt.filterMode = FilterMode.Point;

            RenderTexture.active = rt;

            Graphics.Blit(texture, rt);

            Texture2D blit = new Texture2D((int)blitRect.width, (int)blitRect.height);
            blit.ReadPixels(blitRect, 0, 0);
            blit.Apply();

            RenderTexture.active = null;

            texture.filterMode = prevFilterMode;

            return blit;
        }

        /// <summary>
        /// Texture2D.GetPixels() method circumventing the read-write protection and taking into account <i>blitMaxDimensions</i>.
        /// </summary>
        /// <param name="texture">Any texture with/without read-write protection, of any size (but will be scaled to blitMaxDimensions if larger than those)</param>
        /// <param name="width">Final width of Color[]</param>
        /// <param name="height">Final height of Color[]</param>
        /// <returns>Color[] array after resizing to fit blitMaxDimensions</returns>
        public static Color[] GetColorSafe(this Texture2D texture, out int width, out int height)
        {
            width = texture.width;
            height = texture.height;
            if (texture.width > texture.height)
            {
                width = Math.Min(width, blitMaxDimensions);
                height = (int)((float)width * ((float)texture.height / (float)texture.width));
            }
            else if (texture.height > texture.width)
            {
                height = Math.Min(height, blitMaxDimensions);
                width = (int)((float)height * ((float)texture.width / (float)texture.height));
            }
            else
            {
                width = Math.Min(width, blitMaxDimensions);
                height = Math.Min(height, blitMaxDimensions);
            }

            Color[] color = null;

            var blitRect = new Rect(0, 0, width, height);
            var rtSize = new[] { width, height };

            if (width == texture.width && height == texture.height)
            {
                try
                {
                    color = texture.GetPixels();
                }
                catch
                {
                    color = texture.Blit(blitRect, rtSize).GetPixels();
                }
            }
            else
            {
                color = texture.Blit(blitRect, rtSize).GetPixels();
            }
            return color;
        }

        public static Texture2D BlitCrop(this Texture2D texture, Rect blitRect)
        {
            return texture.Blit(blitRect, new int[] { texture.width, texture.height });
        }
        #endregion

        #region Misc
        public static List<ThingDef> allWeaponDefs = new List<ThingDef>();

        public static readonly FieldInfo cachedLabelCapInfo = typeof(Def).GetField("cachedLabelCap", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void UpdateLabel(this Def def, string label)
        {
            def.label = label;
            cachedLabelCapInfo.SetValue(def, new TaggedString(""));
        }

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
            if (!pawn.pather.Moving)
            {
                return 0f;
            }
            float movePerTick = 60 / pawn.GetStatValue(StatDefOf.MoveSpeed, false);    //Movement per tick

            //pawn.pather.nextCellCostLeft
            //the orginial is (pawn.Position, false, pawn.Position) before 1.3

            movePerTick += pawn.Map.pathing.For(pawn).pathGrid.CalculatedCostAt(pawn.pather.nextCell, perceivedStatic: false, pawn.Position);
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

        public static float ClosestDistBetween(Vector2 origin, Vector2 destination, Vector2 target)
        {
            return Mathf.Abs((destination.y - origin.y) * target.x - (destination.x - origin.x) * target.y + destination.x * origin.y - destination.y * origin.x) / (destination - origin).magnitude;
        }

        /// <summary>
        /// Attempts to find a turret operator. Accepts any Thing as input and does a sanity check to make sure it is an actual turret.
        /// </summary>
        /// <param name="thing">The turret to check for an operator</param>
        /// <returns>Turret operator if one is found, null if not</returns>
        public static Pawn TryGetTurretOperator(Thing thing)
        {
            // Building_TurretGunCE DOES NOT inherit from Building_TurretGun!!!
            if (thing is Building_Turret)
            {
                CompMannable comp = thing.TryGetComp<CompMannable>();
                if (comp != null)
                {
                    return comp.ManningPawn;
                }
            }
            return null;
        }

        /// <summary>
        /// Extension method to determine whether a pawn has equipped a shield
        /// </summary>
        /// <returns>True if the pawn has a shield equipped</returns>
        public static bool HasShield(this Pawn pawn)
        {
            if ((pawn.apparel?.WornApparelCount ?? 0) == 0) return false;
            return pawn.apparel.WornApparel.Any(a => a is Apparel_Shield);
        }

        /// <summary>
        /// Extension method to determine whether a pawn has equipped two handed weapon
        /// </summary>
        /// <returns>True if the pawn has equipped a two handed weapon</returns>
        public static bool HasTwoWeapon(this Pawn pawn)
        {
            if (pawn.equipment?.Primary == null) return false;
            return !(pawn.equipment.Primary.def.weaponTags?.Contains(Apparel_Shield.OneHandedTag) ?? false);
        }

        /// <summary>
        /// Extension method to determine whether a pawn has equipped two handed weapon
        /// </summary>
        /// <returns>True if the pawn has equipped a two handed weapon</returns>
        public static bool IsTwoHandedWeapon(this Thing weapon)
        {
            return !(weapon.def.weaponTags?.Contains(Apparel_Shield.OneHandedTag) ?? false);
        }

        /// <summary>
        /// Extension method to determine whether a ranged weapon has ammo available to it
        /// </summary>
        /// <returns>True if the gun has no CompAmmoUser, doesn't use ammo or has ammo in its magazine or carrier inventory, false otherwise</returns>
        public static bool HasAmmo(this ThingWithComps gun)
        {
            CompAmmoUser comp = gun.TryGetComp<CompAmmoUser>();
            if (comp == null) return true;
            return !comp.UseAmmo || comp.CurMagCount > 0 || comp.HasAmmo;
        }

        public static bool CanBeStabilized(this Hediff diff)
        {
            HediffWithComps hediff = diff as HediffWithComps;
            if (hediff == null)
            {
                return false;
            }
            if (hediff.BleedRate == 0f || hediff.IsTended() || hediff.IsPermanent())
            {
                return false;
            }
            HediffComp_Stabilize comp = hediff.TryGetComp<HediffComp_Stabilize>();
            return comp != null && !comp.Stabilized;
        }

        /// <summary>
        /// Attempts to get the weapon from the equipper of the weapon that launched the projectile
        /// </summary>
        /// <param name="launcher">The equipper of the weapon that launched the projectile</param>
        /// <returns>Weapon if one is found, null if not</returns>
        /*
         * Fundamentally broken - will null ref if launcher pawn drops equipment in-between firing the projectile and it impacting -NIA
        public static Thing GetWeaponFromLauncher(Thing launcher)
        {
            if (launcher is Pawn pawn)
                return pawn.equipment?.Primary;
            if (launcher is Building_TurretGunCE turretCE)
                return turretCE.Gun;
            return null;
        }
        */

        #endregion Misc

        #region MoteThrower
        public static void ThrowEmptyCasing(Vector3 loc, Map map, ThingDef casingMoteDef, float size = 1f)
        {
            if (!Controller.settings.ShowCasings || !loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            //MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(casingMoteDef, null);
            //moteThrown.Scale = Rand.Range(0.5f, 0.3f) * size;
            //moteThrown.exactRotation = Rand.Range(-3f, 4f);
            //moteThrown.exactPosition = loc;
            //moteThrown.airTimeLeft = 60;
            //moteThrown.SetVelocity((float)Rand.Range(160, 200), Rand.Range(0.7f, 0.5f));
            //     moteThrown.SetVelocityAngleSpeed((float)Rand.Range(160, 200), Rand.Range(0.020f, 0.0115f));
            //GenSpawn.Spawn(moteThrown, loc.ToIntVec3(), map);
        }
        #endregion

        #region Physics
        /// <summary>
        /// Gravity constant in meters per second squared
        /// </summary>
        public const float GravityConst = 9.8f * 0.2f;

        public static Bounds GetBoundsFor(IntVec3 cell, RoofDef roof)
        {
            if (roof == null)
                return new Bounds();

            float height = CollisionVertical.WallCollisionHeight;

            if (roof.isNatural)
                height *= CollisionVertical.NaturalRoofThicknessMultiplier;

            if (roof.isThickRoof)
                height *= CollisionVertical.ThickRoofThicknessMultiplier;

            height = Mathf.Max(0.1f, height - CollisionVertical.WallCollisionHeight);

            Vector3 center = cell.ToVector3Shifted();
            center.y = CollisionVertical.WallCollisionHeight + height / 2f;

            return new Bounds(center,
                              new Vector3(1f, height, 1f));
        }

        public static Bounds GetBoundsFor(Thing thing)
        {
            if (thing == null)
            {
                return new Bounds();
            }
            var height = new CollisionVertical(thing);
            var width = GetCollisionWidth(thing);
            var thingPos = thing.DrawPos;
            thingPos.y = height.Max - height.HeightRange.Span / 2;
            Bounds bounds = new Bounds(thingPos, new Vector3(width, height.HeightRange.Span, width));
            return bounds;
        }

        /// <summary>
        /// Calculates the width of an object for purposes of bullet collision. Return value is distance from center of object to its edge in cells, so a wall filling out an entire cell has a width of 0.5.
        /// Also accounts for general body type, humanoids must be specified in the humanoidBodyList and will have reduced width relative to their overall body size.
        /// </summary>
        /// <param name="thing">The Thing to measure width of</param>
        /// <returns>Distance from center of Thing to its edge in cells</returns>
        public static float GetCollisionWidth(Thing thing)
        {
            /* Possible solution for fixing tree widths
			if (thing.IsTree())
        	{
        		return (thing as Plant).def.graphicData.shadowData.volume.x;
        	}*/

            var pawn = thing as Pawn;
            if (pawn != null)
            {
                return GetCollisionBodyFactors(pawn).x;
            }

            return 1f;    //Buildings, etc. fill out a full square
        }

        /// <summary>
        /// Calculates body scale factors based on body type
        /// </summary>
        /// <param name="pawn">Which pawn to measure for</param>
        /// <returns>Width factor as First, height factor as second</returns>
        public static Vector2 GetCollisionBodyFactors(Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Error("CE calling GetCollisionBodyHeightFactor with nullPawn");
                return new Vector2(1, 1);
            }

            var factors = BoundsInjector.ForPawn(pawn);

            if (pawn.GetPosture() != PawnPosture.Standing)
            {
                RacePropertiesExtensionCE props = pawn.def.GetModExtension<RacePropertiesExtensionCE>() ?? new RacePropertiesExtensionCE();

                var shape = props.bodyShape;

                if (shape == CE_BodyShapeDefOf.Invalid)
                {
                    Log.ErrorOnce("CE returning BodyType Undefined for pawn " + pawn.ToString(), 35000198 + pawn.GetHashCode());
                }

                factors.x *= shape.widthLaying / shape.width;
                factors.y *= shape.heightLaying / shape.height;
            }

            return factors;
        }

        /// <summary>
        /// Determines whether a pawn should be currently crouching down or not
        /// </summary>
        /// <returns>True for humanlike pawns currently doing a job during which they should be crouching down</returns>
        public static bool IsCrouching(this Pawn pawn)
        {
            return pawn.RaceProps.Humanlike && !pawn.Downed && (pawn.CurJob?.def.GetModExtension<JobDefExtensionCE>()?.isCrouchJob ?? false);
        }

        public static bool IsPlant(this Thing thing)
        {
            return thing.def.category == ThingCategory.Plant;
        }

        public static float MaxProjectileRange(float shotHeight, float shotSpeed, float shotAngle, float gravityFactor)
        {
            //Fragment at 0f height early opt-out
            if (shotHeight < 0.001f)
            {
                return (Mathf.Pow(shotSpeed, 2f) / gravityFactor) * Mathf.Sin(2f * shotAngle);
            }
            return ((shotSpeed * Mathf.Cos(shotAngle)) / gravityFactor) * (shotSpeed * Mathf.Sin(shotAngle) + Mathf.Sqrt(Mathf.Pow(shotSpeed * Mathf.Sin(shotAngle), 2f) + 2f * gravityFactor * shotHeight));
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

        public static void TryUpdateInventory(ThingOwner owner)
        {
            Pawn pawn = owner?.Owner?.ParentHolder as Pawn;
            if (pawn != null)
            {
                TryUpdateInventory(pawn);
            }
        }

        #endregion
    }
}
