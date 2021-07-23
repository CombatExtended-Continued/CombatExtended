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
using System.Runtime.CompilerServices;

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

        public static float GetLightingShift(Thing caster, float glowAtTarget)
        {
            return Mathf.Max((1.0f - glowAtTarget) * (1.0f - caster.GetStatValue(CE_StatDefOf.NightVisionEfficiency)), 0f);
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
        public static void ThrowEmptyCasing(Vector3 loc, Map map, FleckDef casingFleckDef, float size = 1f)
        {
            if (!Controller.settings.ShowCasings || !loc.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority)
            {
                return;
            }
            FleckCreationData creationData = FleckMaker.GetDataStatic(loc, map, casingFleckDef);
            creationData.airTimeLeft = 60;
            creationData.scale = Rand.Range(0.5f, 0.3f) * size;
            creationData.rotation = Rand.Range(-3f, 4f);
            creationData.spawnPosition = loc;
            creationData.velocitySpeed = (float)Rand.Range(0.7f, 0.5f);
            creationData.velocityAngle = (float)Rand.Range(160, 200);
            map.flecks.CreateFleck(creationData);
        }

        public static void MakeIconOverlay(Pawn pawn, ThingDef moteDef)
        {
            MoteThrownAttached moteThrown = (MoteThrownAttached)ThingMaker.MakeThing(moteDef);
            moteThrown.Attach(pawn);
            moteThrown.exactPosition = pawn.DrawPos;
            moteThrown.Scale = 1.0f;
            moteThrown.SetVelocity(Rand.Range(20f, 25f), 0.4f);
            GenSpawn.Spawn(moteThrown, pawn.Position, pawn.Map);
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

        /// <summary>
        /// Get all weapons a pawn has.
        /// </summary>
        /// <param name="pawn">Pawn</param>
        /// <param name="weapons">Weapons</param>
        /// <param name="rebuild">(Slow) wether to rebuild the cache</param>
        /// <returns>If this pawn has a CompInventory or not</returns>
        public static bool TryGetAllWeaponsInInventory(this Pawn pawn, out List<ThingWithComps> weapons, bool rebuildInvetory = false)
        {
            weapons = null;
            CompInventory compInventory = pawn.TryGetComp<CompInventory>();
            // check is this pawn has a CompInventory
            if (compInventory == null)
                return false;
            if (rebuildInvetory)
                compInventory.UpdateInventory();
            // Add all weapons in the inventory
            weapons = compInventory.weapons.ToList();
            return true;
        }

        /// <summary>
        /// THIS IS A VERY SLOW FUNCTION!! USE IT CAREFULY!
        /// Try to find a random weapon in inventory that has ammo and is ready combat.
        /// This will first check ranged weapons then melee weapons.
        /// </summary>
        /// <param name="pawn">Pawn</param>
        /// <param name="weapon">out The random weapons (if no ranged weapons are found and includeMelee it will return a melee weapon)</param>
        /// <param name="includeMelee">Return a random melee weapon if no ranged weapons found</param>
        /// <param name="includeAOE">Include explosive weapons</param>
        /// <returns>If a weapon is found</returns>
        public static bool TryGetRandomUsableWeapon(this Pawn pawn, out ThingWithComps weapon, bool includeMelee = true, bool includeAOE = false, bool rebuildInvetory = false)
        {
            weapon = null;
            CompInventory compInventory = pawn.TryGetComp<CompInventory>();

            if (compInventory == null)
                return false;
            if (rebuildInvetory)
                compInventory.UpdateInventory();
            List<ThingWithComps> rangedGuns = compInventory.rangedWeaponList;
            if (rangedGuns == null || rangedGuns.Count == 0)
            {
                if (!includeMelee)
                    return false;
                List<ThingWithComps> meleeWeapons = compInventory.meleeWeaponList;

                if (meleeWeapons == null || meleeWeapons.Count == 0)
                    return false;
                weapon = meleeWeapons.RandomElement();
                return true;
            }
            if (!Controller.settings.EnableAmmoSystem)
            {
                weapon = rangedGuns.RandomElement();
                return true;
            }
            foreach (ThingWithComps gun in rangedGuns.InRandomOrder())
            {
                CompAmmoUser compAmmo = gun.GetComp<CompAmmoUser>();
                if (compAmmo == null || compAmmo.IsEquippedGun)
                    continue;
                // check if this is an explosive weapon 
                if (!includeAOE && (compAmmo.Props?.ammoSet?.ammoTypes?.RandomElement().ammo?.detonateProjectile != null))
                    continue;
                // check readiness
                if (compAmmo.CanBeFiredNow || compAmmo.TryFindAmmoInInventory(out Thing _))
                {
                    weapon = gun;
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Weapons

        /// <summary>
        /// Used to get or check if this pawn has a CE (ammo enabled weapon) weapon equiped.
        /// </summary>
        /// <param name="pawn">Pawn</param>
        /// <param name="gun">out the Primary ammo enabled weapon/equipment</param>
        /// <returns>If the primary equipment is ammo enabled weapon has been found</returns>
        public static bool TryGetPrimaryCEWeapon(this Pawn pawn, out Thing gun)
        {
            gun = pawn.equipment?.Primary;
            CompAmmoUser ammoUser = gun?.TryGetComp<CompAmmoUser>() ?? null;

            if (ammoUser == null || !ammoUser.HasMagazine || ammoUser.CurMagCount > 0)
                return false; // gun isn't an ammo user that stores ammo internally or isn't out of bullets.
            return true;
        }

        #endregion

        #region Lighting

        /// <summary>
        /// Used to get the lighting penalty multiplier for a given range
        /// </summary>
        /// <returns></returns>
        public static float LightingRangeMultiplier(float range)
        {
            return lightingCurve.Evaluate(range);
        }

        private static Map[] _maps = new Map[20];
        private static LightingTracker[] _lightingTrackers = new LightingTracker[20];

        public static LightingTracker GetLightingTracker(this Map map)
        {
            int index = map?.Index ?? -1;
            if (index < 0)
                return null;
            if (index >= _maps.Length)
            {
                int expandedLength = Mathf.Max(_maps.Length * 2, index + 1);
                Map[] maps = new Map[expandedLength];
                LightingTracker[] trackers = new LightingTracker[expandedLength];
                Array.Copy(_maps, maps, _maps.Length);
                Array.Copy(_lightingTrackers, trackers, _lightingTrackers.Length);
                _maps = maps;
                _lightingTrackers = trackers;
            }
            if (_maps[index] == map)
                return _lightingTrackers[index];
            return _lightingTrackers[index] = (_maps[index] = map).GetComponent<LightingTracker>();
        }

        #endregion

        #region Initialization

        private static readonly SimpleCurve lightingCurve = new SimpleCurve();

        static CE_Utility()
        {
            lightingCurve.Add(05.00f, 0.05f);
            lightingCurve.Add(10.00f, 0.15f);
            lightingCurve.Add(22.00f, 0.475f);
            lightingCurve.Add(30.00f, 1.00f);
            lightingCurve.Add(60.00f, 1.20f);
            lightingCurve.Add(90.00f, 2.00f);
        }

        #endregion
    }
}
