using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using CombatExtended.Compatibility;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public abstract class ProjectileCE : ThingWithComps
    {
        #region ClassVariables
        /// <summary>
        /// Suppression is applied within this radius (x-y and z)
        /// </summary>
        private const int SuppressionRadius = 3;

        /// <summary>
        /// Check for collision with multi-cell pawns and apply suppression in radius of this size, centered on flight path.
        /// </summary>
        private const int collisionCheckSize = 5;

        #region Origin destination
        protected Vector2 origin;

        private IntVec3 originInt = new IntVec3(0, -1000, 0);
        public IntVec3 OriginIV3
        {
            get
            {
                if (originInt.y < 0)
                {
                    originInt = new IntVec3(origin);
                }
                return originInt;
            }
        }

        protected Vector3 destinationInt = new Vector3(0f, 0f, -1f);
        /// <summary>
        /// Calculates the destination (zero height) reached with a projectile of speed <i>shotSpeed</i> fired at <i>shotAngle</i> from height <i>shotHeight</i> starting from <i>origin</i>. Does not take into account air resistance.
        /// </summary>
        protected Vector2 Destination
        {
            get
            {
                if (destinationInt.z < 0)
                {
                    destinationInt = origin + Vector2.up.RotatedBy(shotRotation) * DistanceTraveled;
                    destinationInt.z = 0f;
                }
                // Since returning as a Vector2 yields Vector2(Vector3.x, Vector3.y)!
                return destinationInt;
            }
        }
        #endregion

        protected ThingDef equipmentDef;
        protected Thing launcher;
        public Thing intendedTarget;
        public float minCollisionSqr;
        public bool canTargetSelf;
        public bool castShadow = true;
        public bool logMisses = true;

        #region Vanilla
        protected bool landed;
        protected int ticksToImpact;
        private Sustainer ambientSustainer;
        #endregion

        private float suppressionAmount;
        public Thing mount; // GiddyUp compatibility, ignore collisions with pawns the launcher is mounting
        public float AccuracyFactor;

        #region Height
        private int lastHeightTick = -1;
        private float heightInt = 0f;
        /// <summary>
        /// If lastHeightTick is not FlightTicks, Height calculates the quadratic formula (g/2)t^2 + (-v_0y)t + (y-y0) for {g -> gravity, v_0y -> shotSpeed * Mathf.Sin(shotAngle), y0 -> shotHeight, t -> seconds} to find y rounded to the nearest 3 decimals.
        /// 
        /// If lastHeightTick equals FlightTicks, it returns a locally stored value heightInt which is the product of previous calculation.
        /// </summary>
        public float Height
        {
            get
            {
                if (lastHeightTick != FlightTicks)
                {
                    heightInt = ticksToImpact > 0 ? GetHeightAtTicks(FlightTicks) : 0f;
                    lastHeightTick = FlightTicks;
                }
                return heightInt;
            }
        }
        #endregion

        #region Ticks/Seconds
        float startingTicksToImpactInt = -1f;
        protected float StartingTicksToImpact
        {
            get
            {
                if (startingTicksToImpactInt < 0f)
                {
                    // Optimization in case shotHeight is zero (for example for fragments)
                    if (shotHeight < 0.001f)
                    {
                        // Opt-out in case the projectile is to collide instantly
                        if (shotAngle < 0f)
                        {
                            destinationInt = origin;
                            startingTicksToImpactInt = 0f;
                            ImpactSomething();
                            return 0f;
                        }
                        // Multiplied by ticksPerSecond since the calculated time is actually in seconds.
                        startingTicksToImpactInt = (float)((origin - Destination).magnitude / (Mathf.Cos(shotAngle) * shotSpeed)) * (float)GenTicks.TicksPerRealSecond;
                        return startingTicksToImpactInt;
                    }

                    startingTicksToImpactInt = GetFlightTime() * (float)GenTicks.TicksPerRealSecond;
                }
                return startingTicksToImpactInt;
            }
        }

        int intTicksToImpact = -1;
        /// <summary>
        /// An integer ceil value of StartingTicksToImpact. intTicksToImpact is equal to -1 when not initialized.
        /// </summary>
        protected int IntTicksToImpact
        {
            get
            {
                if (intTicksToImpact < 0)
                {
                    intTicksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
                }
                return intTicksToImpact;
            }
        }

        /// <summary>
        /// The amount of integer ticks this projectile has remained in the air for, ignoring impact.
        /// </summary>
        protected int FlightTicks
        {
            get
            {
                return IntTicksToImpact - ticksToImpact;
            }
        }
        /// <summary>
        /// The amount of float ticks the projectile has remained in the air for, including impact.
        /// </summary>
        protected float fTicks
        {
            get
            {
                return (ticksToImpact == 0 ? StartingTicksToImpact : (float)FlightTicks);
            }
        }
        #endregion

        #region Position
        private Vector2 Vec2Position(float ticks = -1f)
        {
            if (ticks < 0)
            {
                ticks = fTicks;
            }
            return Vector2.Lerp(origin, Destination, ticks / StartingTicksToImpact);
        }

        private Vector3 impactPosition = new Vector3();
        /// <summary>
        /// Exact x,y,z (x,height,y) position in terms of Vec2Position.x, .y (lerped origin to Destination) and Height.
        /// </summary>
        public virtual Vector3 ExactPosition
        {
            set
            {
                impactPosition = new Vector3(value.x, value.y, value.z);
                Position = impactPosition.ToIntVec3();
            }
            get
            {
                if (landed)
                {
                    return impactPosition;
                }
                var v = Vec2Position();
                return new Vector3(v.x, Height, v.y);
            }
        }

        public Vector2 DrawPosV2
        {
            get
            {
                return Vec2Position() + new Vector2(0, Height - shotHeight * ((StartingTicksToImpact - fTicks) / StartingTicksToImpact));
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                var v = DrawPosV2;
                return new Vector3(v.x, def.Altitude, v.y);
            }
        }

        private Vector3 lastExactPos = new Vector3(-1000, 0, 0);
        private Vector3 LastPos
        {
            set
            {
                lastExactPos = value;
            }
            get
            {
                if (lastExactPos.x < -999)
                {
                    var lastPos = Vec2Position(FlightTicks - 1);
                    lastExactPos = new Vector3(lastPos.x, GetHeightAtTicks(FlightTicks - 1), lastPos.y);
                }
                return lastExactPos;
            }
        }

        public Vector3 ExactMinusLastPos
        {
            get
            {
                return (ExactPosition - LastPos);
            }
        }

        private int lastShotLine = -1;
        private Ray shotLine;
        public Ray ShotLine
        {
            get
            {
                if (lastShotLine != FlightTicks)
                {
                    shotLine = new Ray(LastPos, ExactMinusLastPos);
                    lastShotLine = FlightTicks;
                }
                return shotLine;
            }
        }
        #endregion

        #region Angle
        /// <summary>
        /// Based on equations of motion
        /// </summary>
        public Quaternion DrawRotation
        {
            get
            {
                var w = (Destination - origin);

                var vx = w.x / StartingTicksToImpact;

                var vy = (w.y - shotHeight) / StartingTicksToImpact
                    + shotSpeed * Mathf.Sin(shotAngle) / GenTicks.TicksPerRealSecond
                    - (GravityFactor * fTicks) / (GenTicks.TicksPerRealSecond * GenTicks.TicksPerRealSecond);

                return Quaternion.AngleAxis(
                    Mathf.Rad2Deg * Mathf.Atan2(-vy, vx) + 90f
                    , Vector3.up);
            }
        }

        public virtual Quaternion ExactRotation
        {
            get
            {
                return Quaternion.AngleAxis(shotRotation, Vector3.down);
            }
        }
        #endregion

        /// <summary>
        /// Angle off the ground [radians].
        /// </summary>
        public float shotAngle = 0f;
        /// <summary>
        /// Angle rotation between shooter and positive y-vector [degrees]. North: 0f, East: 90f, South: 180f, West: 270f.
        /// </summary>
        public float shotRotation = 0f;
        /// <summary>
        /// Shot height in vertical cells. Humans start their shot at 0.85f [vcells].
        /// </summary>
        public float shotHeight = 0f;
        /// <summary>
        /// The assigned shot speed [cells/s] (not speed in z axis or x-y plane), in general equal to the projectile.def.speed value.
        /// </summary>
        public float shotSpeed = -1f;

        private float _gravityFactor = -1;

        /// <summary>
        /// Gravity factor in meters(cells) per second squared
        /// </summary>
        private float GravityFactor
        {
            get
            {
                if (_gravityFactor < 0)
                {
                    _gravityFactor = CE_Utility.GravityConst;
                    if (def.projectile is ProjectilePropertiesCE props) _gravityFactor = props.Gravity;
                }
                return _gravityFactor;
            }
        }

        private Material[] shadowMaterial;
        private Material ShadowMaterial
        {
            get
            {
                if (shadowMaterial == null)
                {
                    //Get fully black version of this.Graphic
		    var g = Graphic as Graphic_Collection;
		    if (g!=null)
		    {
			shadowMaterial = GetShadowMaterial(g);
		    }
		
		    else
		    {
			shadowMaterial = new Material[1];
			shadowMaterial[0] = Graphic.GetColoredVersion(ShaderDatabase.Transparent, Color.black, Color.black).MatSingle;
		    }
                }
                return shadowMaterial[Rand.Range(0, this.shadowMaterial.Length)];
            }
        }
        #endregion

        /*
         * *** End of class variables ***
        */

        #region Methods

        #region Expose
        /// <summary>
        /// Saves new variables shotAngle, shotHeight, shotSpeed.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving && launcher != null && launcher.Destroyed)
            {
                launcher = null;
            }
            Scribe_Values.Look<Vector2>(ref origin, "ori", default(Vector2), true);
            Scribe_Values.Look<int>(ref ticksToImpact, "tTI", 0, true);
            Scribe_References.Look<Thing>(ref intendedTarget, "iT");
            Scribe_References.Look<Thing>(ref launcher, "lcr");
            Scribe_Defs.Look<ThingDef>(ref equipmentDef, "ed");
            Scribe_Values.Look<bool>(ref landed, "lnd");

            //Here be new variables
            Scribe_Values.Look(ref shotAngle, "ang", 0f, true);
            Scribe_Values.Look(ref shotRotation, "sRot", 0f, true);
            Scribe_Values.Look(ref shotHeight, "hgt", 0f, true);
            Scribe_Values.Look(ref shotSpeed, "spd", 0f, true);
            Scribe_Values.Look<bool>(ref canTargetSelf, "cts");
            Scribe_Values.Look<bool>(ref logMisses, "lM", true);
            Scribe_Values.Look<bool>(ref castShadow, "cS", true);
        }
        #endregion

        #region Launch
        /// <summary>
        /// Physics-enabled Launch() method.
        /// </summary>
        /// <param name="launcher">The Thing that launched this projectile.</param>
        /// <param name="origin">The origin of the projectile (different from the launcher for e.g grenade fragments)</param>
        /// <param name="shotAngle">Angle off the ground [radians].</param>
        /// <param name="shotRotation">Rotation between shooter and destination [degrees].</param>
        /// <param name="shotHeight">The shot height, usually the max height of the non-pawn caster, a portion of the height of the pawn caster OR zero. (default: 0)</param>
        /// <param name="shotSpeed">The shot speed (default: def.projectile.speed)</param>
        /// <param name="equipment">The equipment used to fire the projectile.</param>
        public virtual void Launch(Thing launcher, Vector2 origin, float shotAngle, float shotRotation, float shotHeight = 0f, float shotSpeed = -1f, Thing equipment = null)
        {
            this.shotAngle = shotAngle;
            this.shotHeight = shotHeight;
            this.shotRotation = shotRotation;

            Launch(launcher, origin, equipment);
            if (shotSpeed > 0f)
            {
                this.shotSpeed = shotSpeed;
            }

            ticksToImpact = IntTicksToImpact;
        }

        public virtual void Launch(Thing launcher, Vector2 origin, Thing equipment = null)
        {
            shotSpeed = def.projectile.speed;
            this.launcher = launcher;
            this.origin = origin;
            //For explosives/bullets, equipmentDef is important
            equipmentDef = (equipment != null) ? equipment.def : null;

            if (!def.projectile.soundAmbient.NullOrUndefined())
            {
                var info = SoundInfo.InMap(this, MaintenanceType.PerTick);
                ambientSustainer = def.projectile.soundAmbient.TrySpawnSustainer(info);
            }
        }
        #endregion

        #region Collisions
        static readonly FieldInfo interceptAngleField = typeof(CompProjectileInterceptor).GetField("lastInterceptAngle", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo interceptTicksField = typeof(CompProjectileInterceptor).GetField("lastInterceptTicks", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo interceptDebug = typeof(CompProjectileInterceptor).GetField("debugInterceptNonHostileProjectiles", BindingFlags.NonPublic | BindingFlags.Instance);

        static readonly MethodInfo interceptBreakShield = typeof(CompProjectileInterceptor).GetMethod("BreakShield", BindingFlags.NonPublic | BindingFlags.Instance);
        private bool CheckIntercept(Thing interceptorThing, CompProjectileInterceptor interceptorComp, bool withDebug = false)
        {
            Vector3 shieldPosition = interceptorThing.Position.ToVector3ShiftedWithAltitude(0.5f);
            float radius = interceptorComp.Props.radius;
            float blockRadius = radius + def.projectile.SpeedTilesPerTick + 0.1f;

            var newExactPos = ExactPosition;

            if ((newExactPos - shieldPosition).sqrMagnitude > Mathf.Pow(blockRadius, 2))
            {
                return false;
            }
            if (!interceptorComp.Active)
            {
                return false;
            }

            if (interceptorComp.Props.interceptGroundProjectiles && def.projectile.flyOverhead)
            {
                return false;
            }

            if (interceptorComp.Props.interceptAirProjectiles && !def.projectile.flyOverhead)
            {
                return false;
            }

            if ((launcher == null || !launcher.HostileTo(interceptorThing)) && !((bool)interceptDebug.GetValue(interceptorComp)) && !interceptorComp.Props.interceptNonHostileProjectiles)
            {
                return false;
            }
            if (!interceptorComp.Props.interceptOutgoingProjectiles && (shieldPosition - lastExactPos).sqrMagnitude <= Mathf.Pow((float)radius, 2))
            {
                return false;
            }
            if (!IntersectLineSphericalOutline(shieldPosition, radius, lastExactPos, newExactPos))
            {
                return false;
            }
            interceptAngleField.SetValue(interceptorComp, lastExactPos.AngleToFlat(interceptorThing.TrueCenter()));
            interceptTicksField.SetValue(interceptorComp, Find.TickManager.TicksGame);
            var areWeLucky = Rand.Chance((def.projectile as ProjectilePropertiesCE)?.empShieldBreakChance ?? 0);
            if (areWeLucky)
            {
                var firstEMPSecondaryDamage = (def.projectile as ProjectilePropertiesCE)?.secondaryDamage?.FirstOrDefault(sd => sd.def == DamageDefOf.EMP);
                if (def.projectile.damageDef == DamageDefOf.EMP)
                {
                    interceptBreakShield.Invoke(interceptorComp, new object[] { new DamageInfo(def.projectile.damageDef, def.projectile.damageDef.defaultDamage) });
                }
                else if (firstEMPSecondaryDamage != null)
                {
                    interceptBreakShield.Invoke(interceptorComp, new object[] { new DamageInfo(firstEMPSecondaryDamage.def, firstEMPSecondaryDamage.def.defaultDamage) });
                }
            }
            Effecter eff = new Effecter(EffecterDefOf.Interceptor_BlockedProjectile);
            eff.Trigger(new TargetInfo(newExactPos.ToIntVec3(), interceptorThing.Map, false), TargetInfo.Invalid);
            eff.Cleanup();
            return true;
        }

        private static bool IntersectLineSphericalOutline(Vector3 center, float radius, Vector3 pointA, Vector3 pointB)
        {
            var pointAInShield = (center - pointA).sqrMagnitude <= Mathf.Pow(radius, 2);
            var pointBInShield = (center - pointB).sqrMagnitude <= Mathf.Pow(radius, 2);

            if (pointAInShield && pointBInShield) { return false; }
            if (!pointAInShield && !pointBInShield) { return false; }

            return true;
        }

        //Removed minimum collision distance
        private bool CheckForCollisionBetween()
        {
            var lastPosIV3 = LastPos.ToIntVec3();
            var newPosIV3 = ExactPosition.ToIntVec3();

            List<Thing> list = base.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
            for (int i = 0; i < list.Count; i++)
            {
                if (CheckIntercept(list[i], list[i].TryGetComp<CompProjectileInterceptor>()))
                {
                    this.Destroy(DestroyMode.Vanish);
                    return true;
                }
            }

            #region Sanity checks
            if (ticksToImpact == 0 || def.projectile.flyOverhead)
                return false;

            if (!lastPosIV3.InBounds(Map) || !newPosIV3.InBounds(Map))
            {
                return false;
            }

            if (Controller.settings.DebugDrawInterceptChecks)
            {
                Map.debugDrawer.FlashLine(lastPosIV3, newPosIV3);
            }
            #endregion

            // Iterate through all cells between the last and the new position
            // INCLUDING[!!!] THE LAST AND NEW POSITIONS!
            var cells = GenSight.PointsOnLineOfSight(lastPosIV3, newPosIV3).Union(new[] { lastPosIV3, newPosIV3 }).Distinct().OrderBy(x => (x.ToVector3Shifted() - LastPos).MagnitudeHorizontalSquared());

            //Order cells by distance from the last position
            foreach (var cell in cells)
            {
                if (CheckCellForCollision(cell))
                {
                    return true;
                }

                if (Controller.settings.DebugDrawInterceptChecks)
                    Map.debugDrawer.FlashCell(cell, 1, "o");
            }

            return false;
        }

        /// <summary>
        /// Checks whether a collision occurs along flight path within this cell.
        /// </summary>
        /// <param name="cell">Where to check for collisions in</param>
        /// <returns>True if collision occured, false otherwise</returns>
        private bool CheckCellForCollision(IntVec3 cell)
        {
            if (BlockerRegistry.CheckCellForCollisionCallback(this, cell, launcher))
            {
                this.ticksToImpact = 0;
                this.landed = true;

                this.Impact(null);
                return true;
            }
            var roofChecked = false;
            var justWallsRoofs = false;

            //Check for minimum PAWN collision distance
            float distFromOrigin = cell.DistanceToSquared(OriginIV3);
            bool skipCollision = !def.projectile.alwaysFreeIntercept
                && (minCollisionSqr <= 1f
                    ? distFromOrigin < 1f
                    : distFromOrigin <= Mathf.Min(144f, minCollisionSqr / 4));

            var mainThingList = new List<Thing>(Map.thingGrid.ThingsListAtFast(cell))
                .Where(t => justWallsRoofs ? t.def.Fillage == FillCategory.Full : (t is Pawn || t.def.Fillage != FillCategory.None)).ToList();

            //Find pawns in adjacent cells and append them to main list
            if (!justWallsRoofs)
            {
                var adjList = new List<IntVec3>();
                adjList.AddRange(GenAdj.CellsAdjacentCardinal(cell, Rot4.FromAngleFlat(shotRotation), new IntVec2(collisionCheckSize, 0)).ToList());

                //Iterate through adjacent cells and find all the pawns
                foreach (var curCell in adjList)
                {
                    if (curCell != cell && curCell.InBounds(Map))
                    {
                        mainThingList.AddRange(Map.thingGrid.ThingsListAtFast(curCell)
                        .Where(x => x is Pawn));

                        if (Controller.settings.DebugDrawInterceptChecks)
                        {
                            Map.debugDrawer.FlashCell(curCell, 0.7f);
                        }
                    }
                }
            }

            //If the last position is above the wallCollisionHeight, we should check for roof intersections first
            if (LastPos.y > CollisionVertical.WallCollisionHeight)
            {
                if (TryCollideWithRoof(cell))
                {
                    return true;
                }
                roofChecked = true;
            }

            foreach (var thing in mainThingList.Distinct().OrderBy(x => (x.DrawPos - LastPos).sqrMagnitude))
            {
                if ((thing == launcher || thing == mount) && !canTargetSelf) continue;

                // Check for collision
                if ((!skipCollision || thing == intendedTarget) && TryCollideWith(thing))
                    return true;

                // Apply suppression. The height here is NOT that of the bullet in CELL,
                // it is the height at the END OF THE PATH. This is because SuppressionRadius
                // is not considered an EXACT limit.
                if (!justWallsRoofs && ExactPosition.y < SuppressionRadius)
                {
                    var pawn = thing as Pawn;
                    if (pawn != null)
                    {
                        ApplySuppression(pawn);
                    }
                }
            }

            //Finally check for intersecting with a roof (again).
            if (!roofChecked && TryCollideWithRoof(cell))
            {
                return true;
            }
            return false;
        }

        private bool TryCollideWithRoof(IntVec3 cell)
        {
            if (!cell.Roofed(Map)) return false;

            var bounds = CE_Utility.GetBoundsFor(cell, cell.GetRoof(Map));

            float dist;
            if (!bounds.IntersectRay(ShotLine, out dist))
            {
                return false;
            }
            if (dist * dist > ExactMinusLastPos.sqrMagnitude)
            {
                return false;
            }

            var point = ShotLine.GetPoint(dist);
            ExactPosition = point;
            landed = true;

            if (Controller.settings.DebugDrawInterceptChecks) MoteMaker.ThrowText(cell.ToVector3Shifted(), Map, "x", Color.red);

            Impact(null);
            return true;
        }

        /// <summary>
        /// Tries to impact the thing based on whether it intersects the given flight path. Trees have RNG chance to not collide even on intersection. 
        /// </summary>
        /// <param name="thing">What to impact</param>
        /// <returns>True if impact occured, false otherwise</returns>
        private bool TryCollideWith(Thing thing)
        {
            if (thing == launcher && !canTargetSelf)
            {
                return false;
            }

            var bounds = CE_Utility.GetBoundsFor(thing);
            if (!bounds.IntersectRay(ShotLine, out var dist))
            {
                return false;
            }
            if (dist * dist > ExactMinusLastPos.sqrMagnitude)
            {
                return false;
            }

            // Trees and bushes have RNG chance to collide
            if (thing is Plant)
            {
                //Prevents trees near the shooter (e.g the shooter's cover) to be hit
                var accuracyFactor = def.projectile.alwaysFreeIntercept ? 1 : (thing.Position - OriginIV3).LengthHorizontal / 40 * AccuracyFactor;
                var chance = thing.def.fillPercent * accuracyFactor;
                if (Controller.settings.DebugShowTreeCollisionChance) MoteMaker.ThrowText(thing.Position.ToVector3Shifted(), thing.Map, chance.ToString());
                if (!Rand.Chance(chance)) return false;
            }

            var point = ShotLine.GetPoint(dist);
            if (!point.InBounds(Map))
                Log.Error("TryCollideWith out of bounds point from ShotLine: obj " + thing.ThingID + ", proj " + ThingID + ", dist " + dist + ", point " + point);

            ExactPosition = point;
            landed = true;

            if (Controller.settings.DebugDrawInterceptChecks) MoteMaker.ThrowText(thing.Position.ToVector3Shifted(), thing.Map, "x", Color.red);

            Impact(thing);
            return true;
        }
        #endregion

        private void ApplySuppression(Pawn pawn)
        {
            ShieldBelt shield = null;
            if (pawn.RaceProps.Humanlike)
            {
                // check for shield user

                var wornApparel = pawn.apparel.WornApparel;
                for (var i = 0; i < wornApparel.Count; i++)
                {
                    var personalShield = wornApparel[i] as ShieldBelt;
                    if (personalShield != null)
                    {
                        shield = personalShield;
                        break;
                    }
                }
            }
            //Add suppression
            var compSuppressable = pawn.TryGetComp<CompSuppressable>();
            if (compSuppressable != null
                && pawn.Faction != launcher?.Faction
                && (shield == null || shield.ShieldState == ShieldState.Resetting))
            {
                suppressionAmount = def.projectile.GetDamageAmount(1);
                var propsCE = def.projectile as ProjectilePropertiesCE;
                var penetrationAmount = propsCE?.armorPenetrationSharp ?? 0f;
                var armorMod = penetrationAmount <= 0 ? 0 : 1 - Mathf.Clamp(pawn.GetStatValue(CE_StatDefOf.AverageSharpArmor) * 0.5f / penetrationAmount, 0, 1);
                suppressionAmount *= armorMod;
                compSuppressable.AddSuppression(suppressionAmount, OriginIV3);
            }
        }

        #region Tick/Draw
        public override void Tick()
        {
            base.Tick();
            if (landed)
            {
                return;
            }
            LastPos = ExactPosition;
            ticksToImpact--;
            if (!ExactPosition.InBounds(Map))
            {
                Position = LastPos.ToIntVec3();
                Destroy();
                return;
            }
            if (CheckForCollisionBetween())
            {
                return;
            }
            Position = ExactPosition.ToIntVec3();
            if (ticksToImpact == 60 && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal &&
                def.projectile.soundImpactAnticipate != null)
            {
                def.projectile.soundImpactAnticipate.PlayOneShot(this);
            }
            //TODO : It appears that the final steps in the arc (past ticksToImpact == 0) don't CheckForCollisionBetween.
            if (ticksToImpact <= 0)
            {
                ImpactSomething();
                return;
            }
            if (ambientSustainer != null)
            {
                ambientSustainer.Maintain();
            }
        }

        /// <summary>
        /// Draws projectile if at least a tick away from caster (or always if no caster)
        /// </summary>
        public override void Draw()
        {
            if (FlightTicks == 0 && launcher != null && launcher is Pawn)
            {
                //TODO: Draw at the end of the barrel on the pawn
            }
            else
            {
                //Projectile
                //Graphics.DrawMesh(MeshPool.plane10, DrawPos, DrawRotation, def.DrawMatSingle, 0);
                Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), DrawPos, DrawRotation, def.DrawMatSingle, 0);

                //Shadow
                if (castShadow)
                {
                    //TODO : EXPERIMENTAL Add edifice height
                    var shadowPos = new Vector3(ExactPosition.x,
                                                def.Altitude - 0.01f,
                                                ExactPosition.z - Mathf.Lerp(shotHeight, 0f, fTicks / StartingTicksToImpact));
                    //EXPERIMENTAL: + (new CollisionVertical(ExactPosition.ToIntVec3().GetEdifice(Map))).Max);

                    //TODO : Vary ShadowMat plane
                    //Graphics.DrawMesh(MeshPool.plane08, shadowPos, ExactRotation, ShadowMaterial, 0);
                    Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), shadowPos, ExactRotation, ShadowMaterial, 0);
                }

                Comps_PostDraw();
            }
        }
        #endregion

        #region Impact
        //Modified collision with downed pawns
        private void ImpactSomething()
        {
            if (BlockerRegistry.ImpactSomethingCallback(this, launcher))
            {
                this.Destroy();
                return;
            }
            var pos = ExactPosition.ToIntVec3();

            //Not modified, just mortar code
            if (def.projectile.flyOverhead)
            {
                var roofDef = Map.roofGrid.RoofAt(pos);
                if (roofDef != null)
                {
                    if (roofDef.isThickRoof)
                    {
                        def.projectile.soundHitThickRoof.PlayOneShot(new TargetInfo(pos, Map));
                        Destroy();
                        return;
                    }
                    if (pos.GetEdifice(Map) == null || pos.GetEdifice(Map).def.Fillage != FillCategory.Full)
                    {
                        RoofCollapserImmediate.DropRoofInCells(pos, Map);
                    }
                }
            }

            // FIXME : Early opt-out
            Thing thing = pos.GetFirstPawn(Map);
            if (thing != null && TryCollideWith(thing))
            {
                return;
            }

            var list = Map.thingGrid.ThingsListAt(pos).Where(t => t is Pawn || t.def.Fillage != FillCategory.None).ToList();
            if (list.Count > 0)
            {
                foreach (var thing2 in list)
                {
                    if (TryCollideWith(thing2))
                        return;
                }
            }

            ExactPosition = ExactPosition;
            landed = true;
            Impact(null);
        }

        protected virtual void Impact(Thing hitThing)
        {
            var ignoredThings = new List<Thing>();

            //Spawn things from preExplosionSpawnThingDef != null
            if (Position.IsValid
                && def.projectile.preExplosionSpawnChance > 0
                && def.projectile.preExplosionSpawnThingDef != null
                && (Controller.settings.EnableAmmoSystem || !(def.projectile.preExplosionSpawnThingDef is AmmoDef))
                && Rand.Value < def.projectile.preExplosionSpawnChance)
            {
                var thingDef = def.projectile.preExplosionSpawnThingDef;

                if (thingDef.IsFilth && Position.Walkable(Map))
                {
                    FilthMaker.TryMakeFilth(Position, Map, thingDef);
                }
                else if (Controller.settings.ReuseNeolithicProjectiles)
                {
                    var reusableAmmo = ThingMaker.MakeThing(thingDef);
                    reusableAmmo.stackCount = 1;
                    reusableAmmo.SetForbidden(true, false);
                    GenPlace.TryPlaceThing(reusableAmmo, Position, Map, ThingPlaceMode.Near);
                    LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_ReusableNeolithicProjectiles, reusableAmmo, OpportunityType.GoodToKnow);
                    ignoredThings.Add(reusableAmmo);
                }
            }

            var explodePos = hitThing?.DrawPos ?? ExactPosition;

            if (!explodePos.ToIntVec3().IsValid)
            {
                Destroy();
                return;
            }

            var explodingComp = this.TryGetComp<CompExplosiveCE>();

            if (explodingComp == null)
                this.TryGetComp<CompFragments>()?.Throw(explodePos, Map, launcher);

            //If the comp exists, it'll already call CompFragments
            if (explodingComp != null || def.projectile.explosionRadius > 0)
            {
                //Handle anything explosive

                if (hitThing is Pawn && (hitThing as Pawn).Dead)
                    ignoredThings.Add((hitThing as Pawn).Corpse);

                var suppressThings = new List<Pawn>();
                var dir = new float?(origin.AngleTo(Vec2Position()));

                // Opt-out for things without explosionRadius
                if (def.projectile.explosionRadius > 0)
                {
                    GenExplosionCE.DoExplosion(explodePos.ToIntVec3(), Map, def.projectile.explosionRadius,
                        def.projectile.damageDef, launcher, def.projectile.GetDamageAmount(1), GenExplosionCE.GetExplosionAP(def.projectile),
                        def.projectile.soundExplode, equipmentDef,
                        def, null, def.projectile.postExplosionSpawnThingDef, def.projectile.postExplosionSpawnChance, def.projectile.postExplosionSpawnThingCount,
                        def.projectile.applyDamageToExplosionCellsNeighbors, def.projectile.preExplosionSpawnThingDef, def.projectile.preExplosionSpawnChance,
                        def.projectile.preExplosionSpawnThingCount, def.projectile.explosionChanceToStartFire, def.projectile.explosionDamageFalloff,
                        dir, ignoredThings, explodePos.y);

                    // Apply suppression around impact area
                    if (explodePos.y < SuppressionRadius)
                        suppressThings.AddRange(GenRadial.RadialDistinctThingsAround(explodePos.ToIntVec3(), Map, SuppressionRadius + def.projectile.explosionRadius, true)
                            .Where(x => x is Pawn).Select(x => x as Pawn));
                }

                if (explodingComp != null)
                {
                    explodingComp.Explode(this, explodePos, Map, 1f, dir, ignoredThings);

                    if (explodePos.y < SuppressionRadius)
                        suppressThings.AddRange(GenRadial.RadialDistinctThingsAround(explodePos.ToIntVec3(), Map, SuppressionRadius + (explodingComp.props as CompProperties_ExplosiveCE).explosiveRadius, true)
                        .Where(x => x is Pawn).Select(x => x as Pawn));
                }

                foreach (var thing in suppressThings)
                    ApplySuppression(thing as Pawn);
            }

            Destroy();
        }
        #endregion

        #region Ballistics
        /// <summary>
        /// Calculated rounding to three decimales the output of h0 + v * sin(a0) * t - g/2 * t^2 with {h0 -> shotHeight, v -> shotSpeed, a0 -> shotAngle, t -> ticks/GenTicks.TicksPerRealSecond, g -> GravityFactor}. Called roughly each tick for impact checks and for drawing.
        /// </summary>
        /// <param name="ticks">Integer ticks, since the only time value which is not an integer (accessed by StartingTicksToImpact) has height zero by definition.</param>
        /// <returns>Projectile height at time ticks in ticks.</returns>
        private float GetHeightAtTicks(int ticks)
        {
            var seconds = ((float)ticks) / GenTicks.TicksPerRealSecond;
            return (float)Math.Round(shotHeight + shotSpeed * Mathf.Sin(shotAngle) * seconds - (GravityFactor * seconds * seconds) / 2f, 3);
        }

        /// <summary>
        /// Calculates the time in seconds the arc characterized by <i>angle</i>, <i>shotHeight</i> takes to traverse at speed <i>velocity</i> - e.g until the height reaches zero. Does not take into account air resistance.
        /// </summary>
        /// <param name="velocity">Projectile velocity in cells per second.</param>
        /// <param name="angle">Shot angle in radians off the ground.</param>
        /// <param name="shotHeight">Height from which the projectile is fired in vertical cells.</param>
        /// <returns>Time in seconds that the projectile will take to traverse the given arc.</returns>
        private float GetFlightTime()
        {
            //Calculates quadratic formula (g/2)t^2 + (-v_0y)t + (y-y0) for {g -> gravity, v_0y -> vSin, y -> 0, y0 -> shotHeight} to find t in fractional ticks where height equals zero.
            return (Mathf.Sin(shotAngle) * shotSpeed + Mathf.Sqrt(Mathf.Pow(Mathf.Sin(shotAngle) * shotSpeed, 2f) + 2f * GravityFactor * shotHeight)) / GravityFactor;
        }

        /// <summary>
        /// Calculates the range reachable with a projectile of speed <i>velocity</i> fired at <i>angle</i> from height <i>shotHeight</i>. Does not take into account air resistance.
        /// </summary>
        /// <param name="velocity">Projectile velocity in cells per second.</param>
        /// <param name="angle">Shot angle in radians off the ground.</param>
        /// <param name="shotHeight">Height from which the projectile is fired in vertical cells.</param>
        /// <returns>Distance in cells that the projectile will fly at the given arc.</returns>
        private float DistanceTraveled => CE_Utility.MaxProjectileRange(shotHeight, shotSpeed, shotAngle, GravityFactor);

        /// <summary>
        /// Calculates the shot angle necessary to reach <i>range</i> with a projectile of speed <i>velocity</i> at a height difference of <i>heightDifference</i>, returning either the upper or lower arc in radians. Does not take into account air resistance.
        /// </summary>
        /// <param name="velocity">Projectile velocity in cells per second.</param>
        /// <param name="range">Cells between shooter and target.</param>
        /// <param name="heightDifference">Difference between initial shot height and target height in vertical cells.</param>
        /// <param name="flyOverhead">Whether to take the lower (False) or upper (True) arc angle.</param>
        /// <returns>Arc angle in radians off the ground.</returns>
        public static float GetShotAngle(float velocity, float range, float heightDifference, bool flyOverhead, float gravity)
        {
            float squareRootCheck = Mathf.Sqrt(Mathf.Pow(velocity, 4f) - gravity * (gravity * Mathf.Pow(range, 2f) + 2f * heightDifference * Mathf.Pow(velocity, 2f)));
            if (float.IsNaN(squareRootCheck))
            {
                //Target is too far to hit with given velocity/range/gravity params
                //set firing angle for maximum distance
                Log.Warning("[CE] Tried to fire projectile to unreachable target cell, truncating to maximum distance.");
                return 45.0f * Mathf.Deg2Rad;
            }
            return Mathf.Atan((Mathf.Pow(velocity, 2f) + (flyOverhead ? 1f : -1f) * squareRootCheck) / (gravity * range));
        }
        #endregion

        #endregion

	public static Material[] GetShadowMaterial(Graphic_Collection g) {
	    FieldInfo subGraphics = typeof(Graphic_Collection).GetField("subGraphics", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
	    Graphic[] collection = (Graphic[]) subGraphics.GetValue(g);
	    Material[] shadows = new Material[collection.Length];
	    for (int i=0; i<collection.Length; i++) {
		shadows[i] = collection[i].GetColoredVersion(ShaderDatabase.Transparent, Color.black, Color.black).MatSingle;
	    }
	    return shadows;
	}
    }
}
