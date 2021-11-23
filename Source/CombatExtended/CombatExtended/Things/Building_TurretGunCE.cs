using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using UnityEngine;
using CombatExtended.CombatExtended.LoggerUtils;
using CombatExtended.CombatExtended.Jobs.Utils;
using RimWorld.Planet;

namespace CombatExtended
{
    /* Class is cloned from Building_TurretGun with various changes made to support fire modes and ammo
     * 
     * Unmodified methods should be kept up-to-date with vanilla class so long as they don't conflict with changes made. Please mark any changes you make from vanilla.
     * -NIA
     */
    [StaticConstructorOnStartup]
    public class Building_TurretGunCE : Building_Turret
    {
        private const int minTicksBeforeAutoReload = 1800;              // This much time must pass before haulers will try to automatically reload an auto-turret
        private const int ticksBetweenAmmoChecks = 300;                 // Test nearby ammo every 5 seconds if there's many ammo changes
        private const int ticksBetweenSlowAmmoChecks = 3600;               // Test nearby ammo every minute if there's no ammo changes
        public bool isSlow = false;

        private int TicksBetweenAmmoChecks => isSlow ? ticksBetweenSlowAmmoChecks : ticksBetweenAmmoChecks;

        #region Fields

        public int burstCooldownTicksLeft;
        public int burstWarmupTicksLeft;                                // Need this public so aim mode can modify it
        public LocalTargetInfo currentTargetInt = LocalTargetInfo.Invalid;
        private bool holdFire;
        private Thing gunInt;                                           // Better to be private, because Gun is used for access, instead
        public TurretTop top;
        public CompPowerTrader powerComp;
        public CompCanBeDormant dormantComp;
        public CompInitiatable initiatableComp;
        public CompMannable mannableComp;

        public static Material ForcedTargetLineMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.5f, 0.5f));

        // New fields
        private bool targetingWorldMap = false;
        private CompAmmoUser compAmmo = null;
        private CompFireModes compFireModes = null;
        private CompChangeableProjectile compChangeable = null;
        public bool isReloading = false;
        private int ticksUntilAutoReload = 0;
        private bool everSpawned = false;
        private GlobalTargetInfo globalTargetInfo = GlobalTargetInfo.Invalid;
        
        #endregion

        #region Properties
        // Core properties
        public bool Active => (powerComp == null || powerComp.PowerOn) && (dormantComp == null || dormantComp.Awake) && (initiatableComp == null || initiatableComp.Initiated);
        public CompEquippable GunCompEq => Gun.TryGetComp<CompEquippable>();
        public override LocalTargetInfo CurrentTarget => currentTargetInt;        
        private bool WarmingUp => burstWarmupTicksLeft > 0;
        public override Verb AttackVerb => Gun == null ? null : GunCompEq.verbTracker.PrimaryVerb;
        public bool IsMannable => mannableComp != null;
        public bool PlayerControlled => (Faction == Faction.OfPlayer || MannedByColonist) && !MannedByNonColonist;
        private bool CanSetForcedTarget => mannableComp != null && PlayerControlled;
        private bool CanToggleHoldFire => PlayerControlled;
        public bool IsMortar => def.building.IsMortar;
        public bool IsMortarOrProjectileFliesOverhead => Projectile.projectile.flyOverhead || IsMortar;
        //Not included: CanExtractShell
        private bool MannedByColonist => mannableComp != null && mannableComp.ManningPawn != null
            && mannableComp.ManningPawn.Faction == Faction.OfPlayer;
        private bool MannedByNonColonist => mannableComp != null && mannableComp.ManningPawn != null
            && mannableComp.ManningPawn.Faction != Faction.OfPlayer;

        // New properties
        public Thing Gun
        {
            get
            {
                if (this.gunInt == null && Map != null)
                {
                    // I am leaving this here because god knows what uses it before postmake gets called.
                    CELogger.Warn($"Gun {this.ToString()} was referenced before PostMake. If you're seeing this, please report this to the Combat Extended team!", showOutOfDebugMode: true);
                    MakeGun();

                    if (!everSpawned && (!Map.IsPlayerHome || Faction != Faction.OfPlayer))
                    {
                        compAmmo?.ResetAmmoCount();
                        everSpawned = true;
                    }
                }
                return this.gunInt;
            }
        }

        public ThingDef Projectile
        {
            get
            {
                if (CompAmmo != null && CompAmmo.CurrentAmmo != null)
                {
                    return CompAmmo.CurAmmoProjectile;
                }
                if (CompChangeable != null && CompChangeable.Loaded)
                {
                    return CompChangeable.Projectile;
                }
                return this.GunCompEq.PrimaryVerb.verbProps.defaultProjectile;
            }
        }

        public CompChangeableProjectile CompChangeable
        {
            get
            {
                if (compChangeable == null && Gun != null) compChangeable = Gun.TryGetComp<CompChangeableProjectile>();
                return compChangeable;
            }
        }

        public CompAmmoUser CompAmmo
        {
            get
            {
                if (compAmmo == null && Gun != null) compAmmo = Gun.TryGetComp<CompAmmoUser>();
                return compAmmo;
            }
        }

        public CompFireModes CompFireModes
        {
            get
            {
                if (compFireModes == null && Gun != null) compFireModes = Gun.TryGetComp<CompFireModes>();
                return compFireModes;
            }
        }
        private ProjectilePropertiesCE ProjectileProps => (ProjectilePropertiesCE) compAmmo?.CurAmmoProjectile?.projectile ?? null;
        public float MaxWorldRange => ProjectileProps?.shellingProps.range ?? -1f;
        public bool EmptyMagazine => CompAmmo?.EmptyMagazine ?? false;
        public bool FullMagazine => CompAmmo?.FullMagazine ?? false;
        public bool AutoReloadableMagazine => AutoReloadableNow && CompAmmo.CurMagCount <= Mathf.CeilToInt(CompAmmo.MagSize / 6);
        public bool AutoReloadableNow => (mannableComp == null || (!mannableComp.MannedNow && ticksUntilAutoReload == 0)) && Reloadable;    //suppress manned turret auto-reload for a short time after spawning
        public bool Reloadable => CompAmmo?.HasMagazine ?? false;
        public CompMannable MannableComp => mannableComp;
        #endregion

        public Building_TurretGunCE()
        {
            top = new TurretTop(this);
        }

        #region Methods

        public override void SpawnSetup(Map map, bool respawningAfterLoad)      //Add mannableComp, ticksUntilAutoReload, register to GenClosestAmmo
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Map.GetComponent<TurretTracker>().Register(this);

            dormantComp = GetComp<CompCanBeDormant>();
            initiatableComp = GetComp<CompInitiatable>();
            powerComp = GetComp<CompPowerTrader>();
            mannableComp = GetComp<CompMannable>();

            if (!everSpawned && (!Map.IsPlayerHome || Faction != Faction.OfPlayer))
            {
                compAmmo?.ResetAmmoCount();
                everSpawned = true;
            }

            if (!respawningAfterLoad)
            {
                CELogger.Message($"top is {top?.ToString() ?? "null"}");
                top.SetRotationFromOrientation();
                burstCooldownTicksLeft = def.building.turretInitialCooldownTime.SecondsToTicks();

                //Delay auto-reload for a few seconds after spawn, so player can operate the turret right after placing it, before other colonists start reserving it for reload jobs
                if (mannableComp != null)
                    ticksUntilAutoReload = minTicksBeforeAutoReload;
            }

            // if (CompAmmo == null || CompAmmo.Props == null || CompAmmo.Props.ammoSet == null || CompAmmo.Props.ammoSet.ammoTypes.NullOrEmpty())
            //     return;

            // //"Subscribe" turret to GenClosestAmmo
            // foreach (var ammo in CompAmmo.Props.ammoSet.ammoTypes.Select(x => x.ammo))
            // {
            //     if (!GenClosestAmmo.listeners.ContainsKey(ammo))
            //         GenClosestAmmo.listeners.Add(ammo, new List<Building_TurretGunCE>() { this });
            //     else
            //         GenClosestAmmo.listeners[ammo].Add(this);

            //     if (!GenClosestAmmo.latestAmmoUpdate.ContainsKey(ammo))
            //         GenClosestAmmo.latestAmmoUpdate.Add(ammo, 0);
            // }
        }

        //PostMake not added -- MakeGun-like code is run whenever Gun is called
        //No. Fuck you. ^

        public override void PostMake()
        {
            base.PostMake();
            MakeGun();
        }

        private void MakeGun()
        {
            this.gunInt = ThingMaker.MakeThing(this.def.building.turretGunDef, null);
            this.compAmmo = gunInt.TryGetComp<CompAmmoUser>();

            InitGun();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)    // Added GenClosestAmmo unsubscription
        {
            Map.GetComponent<TurretTracker>().Unregister(this);
            base.DeSpawn(mode);
            ResetCurrentTarget();
        }        

        public override void ExposeData()           // Added new variables, removed bool loaded (not used in CE)
        {
            base.ExposeData();

            // New variables                        
            Scribe_Deep.Look(ref gunInt, "gunInt");
            InitGun();
            Scribe_Values.Look(ref isReloading, "isReloading", false);
            Scribe_Values.Look(ref ticksUntilAutoReload, "ticksUntilAutoReload", 0);
            //lastSurroundingAmmoCheck should never be saved

            Scribe_Values.Look<int>(ref this.burstCooldownTicksLeft, "burstCooldownTicksLeft", 0, false);
            Scribe_Values.Look<int>(ref this.burstWarmupTicksLeft, "burstWarmupTicksLeft", 0, false);
            Scribe_TargetInfo.Look(ref this.currentTargetInt, "currentTarget");
            Scribe_Values.Look<bool>(ref this.holdFire, "holdFire", false, false);
            Scribe_Values.Look<bool>(ref this.everSpawned, "everSpawned", false, false);
            
            Scribe_TargetInfo.Look(ref globalTargetInfo, "globalSourceInfo");          
            BackCompatibility.PostExposeData(this);
        }

        public override bool ClaimableBy(Faction by)        // Core method
        {
            return base.ClaimableBy(by) && (this.mannableComp == null || this.mannableComp.ManningPawn == null) && (!this.Active || this.mannableComp != null) && (((this.dormantComp == null || this.dormantComp.Awake) && (this.initiatableComp == null || this.initiatableComp.Initiated)) || (this.powerComp != null && !this.powerComp.PowerOn));
        }

        public override void OrderAttack(LocalTargetInfo targ)      // Core method
        {            
            if (globalTargetInfo.IsValid)
            {
                this.ResetForcedTarget();                
            }
            if (!targ.IsValid)
            {
                if (this.forcedTarget.IsValid)
                {
                    this.ResetForcedTarget();
                }
                return;
            }            
            if ((targ.Cell - base.Position).LengthHorizontal < this.GunCompEq.PrimaryVerb.verbProps.minRange)
            {
                Messages.Message("MessageTargetBelowMinimumRange".Translate(), this, MessageTypeDefOf.RejectInput);
                return;
            }
            if ((targ.Cell - base.Position).LengthHorizontal > this.GunCompEq.PrimaryVerb.verbProps.range)
            {
                Messages.Message("MessageTargetBeyondMaximumRange".Translate(), this, MessageTypeDefOf.RejectInput);
                return;
            }            
            if (this.forcedTarget != targ)
            {
                this.forcedTarget = targ;
                if (this.burstCooldownTicksLeft <= 0)
                {
                    this.TryStartShootSomething(false);
                }
            }
            if (this.holdFire)
            {
                Messages.Message("MessageTurretWontFireBecauseHoldFire".Translate(this.def.label), this, MessageTypeDefOf.RejectInput, false);
            }
        }

        public override void Tick()     //Autoreload code and IsReloading check
        {
            base.Tick();
            if (ticksUntilAutoReload > 0) ticksUntilAutoReload--;   // Reduce time until we can auto-reload

            if (!isReloading && this.IsHashIntervalTick(TicksBetweenAmmoChecks) && (MannableComp?.MannedNow ?? false))
            {
                TryOrderReload();
            }

            //This code runs TryOrderReload for manning pawns or for non-humanlike intelligence such as mechs
            /*if (this.IsHashIntervalTick(TicksBetweenAmmoChecks) && !isReloading && (MannableComp?.MannedNow ?? false))
                  TryOrderReload(CompAmmo?.CurMagCount == 0);*/
            if (!CanSetForcedTarget && !isReloading && forcedTarget.IsValid && burstCooldownTicksLeft <= 0)
            {
                ResetForcedTarget();
            }
            if (!CanToggleHoldFire)
            {
                holdFire = false;
            }
            if (forcedTarget.ThingDestroyed)
            {
                ResetForcedTarget();
            }
            if (Active && (this.mannableComp == null || this.mannableComp.MannedNow) && base.Spawned && !(isReloading && WarmingUp))
            {
                this.GunCompEq.verbTracker.VerbsTick();
                if (!this.stunner.Stunned && this.GunCompEq.PrimaryVerb.state != VerbState.Bursting)
                {
                    if (this.WarmingUp)
                    {
                        this.burstWarmupTicksLeft--;
                        if (this.burstWarmupTicksLeft == 0)
                        {
                            this.BeginBurst();
                        }
                    }
                    else
                    {
                        if (this.burstCooldownTicksLeft > 0)
                        {
                            this.burstCooldownTicksLeft--;
                        }
                        if (this.burstCooldownTicksLeft <= 0)
                        {
                            this.TryStartShootSomething(true);
                        }
                    }
                    this.top.TurretTopTick();
                    return;
                }
            }
            else
            {
                this.ResetCurrentTarget();
            }
        }

        public void TryStartShootSomething(bool canBeginBurstImmediately)    // Added ammo check and use verb warmup time instead of turret's
        {
            // Check for ammo first
            if (!Spawned
                || (holdFire && CanToggleHoldFire)
                || (Projectile.projectile.flyOverhead && Map.roofGrid.Roofed(Position))
                //|| !AttackVerb.Available()  -- Check replaced by the following:
                || (CompAmmo != null && (isReloading || (mannableComp == null && CompAmmo.CurMagCount <= 0))))
            {
                ResetCurrentTarget();
                return;
            }            
            //Copied and modified from Verb_LaunchProjectileCE.Available
            if (!isReloading && (Projectile == null || (CompAmmo != null && !CompAmmo.CanBeFiredNow)))
            {
                ResetCurrentTarget();
                TryOrderReload();
                return;
            }
            bool isValid = currentTargetInt.IsValid;
            currentTargetInt = forcedTarget.IsValid ? forcedTarget : TryFindNewTarget();
            if (!isValid && currentTargetInt.IsValid)
            {
                SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(Position, Map, false));
            }
            if (!currentTargetInt.IsValid)
            {
                ResetCurrentTarget();
                return;
            }
            // Use verb warmup time instead of turret's
            if (AttackVerb.verbProps.warmupTime > 0f)
            {
                burstWarmupTicksLeft = AttackVerb.verbProps.warmupTime.SecondsToTicks();
                return;
            }
            if (targetingWorldMap && (!globalTargetInfo.IsValid || globalTargetInfo.WorldObject is DestroyedSettlement))
            {
                ResetCurrentTarget();
                return;
            }
            if (canBeginBurstImmediately)
            {
                BeginBurst();
                return;
            }
            burstWarmupTicksLeft = 1;
        }

        public LocalTargetInfo TryFindNewTarget()    // Core method
        {
            IAttackTargetSearcher attackTargetSearcher = this.TargSearcher();
            Faction faction = attackTargetSearcher.Thing.Faction;
            float range = this.AttackVerb.verbProps.range;
            Building t;
            if (Rand.Value < 0.5f && this.AttackVerb.ProjectileFliesOverhead() && faction.HostileTo(Faction.OfPlayer) && base.Map.listerBuildings.allBuildingsColonist.Where(delegate (Building x)
            {
                float num = this.AttackVerb.verbProps.EffectiveMinRange(x, this);
                float num2 = (float)x.Position.DistanceToSquared(this.Position);
                return num2 > num * num && num2 < range * range;
            }).TryRandomElement(out t))
            {
                return t;
            }
            TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat;
            if (!this.AttackVerb.ProjectileFliesOverhead())
            {
                targetScanFlags |= TargetScanFlags.NeedLOSToAll;
                targetScanFlags |= TargetScanFlags.LOSBlockableByGas;
            }
            if (this.AttackVerb.IsIncendiary())
            {
                targetScanFlags |= TargetScanFlags.NeedNonBurning;
            }
            return (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(attackTargetSearcher, targetScanFlags, new Predicate<Thing>(this.IsValidTarget), 0f, 9999f);
        }

        private IAttackTargetSearcher TargSearcher()    // Core method
        {
            if (mannableComp != null && mannableComp.MannedNow)
            {
                return mannableComp.ManningPawn;
            }
            return this;
        }

        private bool IsValidTarget(Thing t)             // Projectile flyoverhead check instead of verb
        {
            Pawn pawn = t as Pawn;
            if (pawn != null)
            {
                //if (this.GunCompEq.PrimaryVerb.verbProps.projectileDef.projectile.flyOverhead)
                if (Projectile.projectile.flyOverhead)
                {
                    RoofDef roofDef = base.Map.roofGrid.RoofAt(t.Position);
                    if (roofDef != null && roofDef.isThickRoof)
                    {
                        return false;
                    }
                }
                if (this.mannableComp == null)
                {
                    return !GenAI.MachinesLike(base.Faction, pawn);
                }
                if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer)
                {
                    return false;
                }
            }
            return true;
        }

        public void BeginBurst()                     // Added handling for ticksUntilAutoReload
        {
            ticksUntilAutoReload = minTicksBeforeAutoReload;
            if(AttackVerb is Verb_ShootMortarCE shootMortar)
            {
                if (globalTargetInfo.IsValid)
                {
                    targetingWorldMap = true;
                    GlobalTargetInfo sourceInfo = new GlobalTargetInfo();
                    sourceInfo.tileInt = Map.Tile;
                    sourceInfo.cellInt = Position;
                    sourceInfo.mapInt = Map;
                    sourceInfo.thingInt = IsMannable ? ((Thing) mannableComp.ManningPawn) : ((Thing) this);                    
                    shootMortar.TryStartShelling(sourceInfo, globalTargetInfo);
                }
                else
                {
                    shootMortar.globalTargetInfo = GlobalTargetInfo.Invalid;
                    shootMortar.TryStartCastOn(CurrentTarget, false, true);
                }
            }
            else
            {                
                AttackVerb.TryStartCastOn(CurrentTarget, false, true);
            }            
            OnAttackedTarget(CurrentTarget);
        }

        public void BurstComplete()                  // Added CompAmmo reload check
        {
            burstCooldownTicksLeft = BurstCooldownTime().SecondsToTicks();
            if (CompAmmo != null && CompAmmo.CurMagCount <= 0)
            {
                TryForceReload();
            }
        }

        public float BurstCooldownTime()             // Core method
        {
            if (def.building.turretBurstCooldownTime >= 0f)
            {
                return def.building.turretBurstCooldownTime;
            }
            return AttackVerb.verbProps.defaultCooldownTime;
        }

        public override string GetInspectString()       // Replaced vanilla loaded text with CE reloading
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = base.GetInspectString();
            if (!inspectString.NullOrEmpty())
            {
                stringBuilder.AppendLine(inspectString);
            }

            stringBuilder.AppendLine("GunInstalled".Translate() + ": " + this.Gun.LabelCap);    // New code

            if (this.GunCompEq.PrimaryVerb.verbProps.minRange > 0f)
            {
                stringBuilder.AppendLine("MinimumRange".Translate() + ": " + this.GunCompEq.PrimaryVerb.verbProps.minRange.ToString("F0"));
            }

            if (isReloading)        // New code
            {
                stringBuilder.AppendLine("CE_TurretReloading".Translate());
            }

            else if (Spawned && IsMortarOrProjectileFliesOverhead && Position.Roofed(Map))
            {
                stringBuilder.AppendLine("CannotFire".Translate() + ": " + "Roofed".Translate().CapitalizeFirst());
            }
            else if (Spawned && burstCooldownTicksLeft > 0)
            {
                stringBuilder.AppendLine("CanFireIn".Translate() + ": " + this.burstCooldownTicksLeft.ToStringSecondsFromTicks());
            }
            /*
            if (this.def.building.turretShellDef != null)
            {
                if (this.loaded)
                {
                    stringBuilder.AppendLine("ShellLoaded".Translate());
                }
                else
                {
                    stringBuilder.AppendLine("ShellNotLoaded".Translate());
                }
            }
            */
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override void Draw()
        {
            top.DrawTurret(Vector3.zero, 0f);
            base.Draw();
        }

        public override void DrawExtraSelectionOverlays()           // Draw at range less than 1.42 tiles
        {
            float range = this.GunCompEq.PrimaryVerb.verbProps.range;
            if (range < 90f)
            {
                GenDraw.DrawRadiusRing(base.Position, range);
            }
            float minRange = AttackVerb.verbProps.minRange;     // Changed to minRange instead of EffectiveMinRange
            if (minRange < 90f && minRange > 0.1f)
            {
                GenDraw.DrawRadiusRing(base.Position, minRange);
            }
            if (this.WarmingUp)
            {
                int degreesWide = (int)((float)this.burstWarmupTicksLeft * 0.5f);
                GenDraw.DrawAimPie(this, this.CurrentTarget, degreesWide, def.size.x * 0.5f);
            }
            if (this.forcedTarget.IsValid && (!this.forcedTarget.HasThing || this.forcedTarget.Thing.Spawned))
            {
                Vector3 b;
                if (this.forcedTarget.HasThing)
                {
                    b = this.forcedTarget.Thing.TrueCenter();
                }
                else
                {
                    b = this.forcedTarget.Cell.ToVector3Shifted();
                }
                Vector3 a = this.TrueCenter();
                b.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
                a.y = b.y;
                GenDraw.DrawLineBetween(a, b, Building_TurretGun.ForcedTargetLineMat);
            }
        }

        public bool TryAttackWorldTarget(GlobalTargetInfo targetInfo, LocalTargetInfo localTarget)
        {
            ResetCurrentTarget();
            ResetForcedTarget();
            int distanceToTarget = Find.WorldGrid.TraversalDistanceBetween(Map.Tile, targetInfo.Tile, true, maxDist: (int)(this.MaxWorldRange * 1.5f));
            if (distanceToTarget > MaxWorldRange)
            {                
                return false;
            }
            if (!Active)
            {
                return false;
            }
            if (localTarget.IsValid)
            {
                TryOrderAttackWorldTile(targetInfo, localTarget.Cell);
            }
            else
            {
                TryOrderAttackWorldTile(targetInfo, null);
            }            
            return true;
        }

        public virtual void TryOrderAttackWorldTile(GlobalTargetInfo targetInf, IntVec3? cell = null)
        {
            int startingTile = Map.Tile;
            int destinationTile = targetInf.Tile;
            
            Vector3 direction = (Find.WorldGrid.GetTileCenter(startingTile) - Find.WorldGrid.GetTileCenter(destinationTile)).normalized;
            Vector3 shotPos = DrawPos.Yto0();
            Vector3 mapSize = Map.Size.ToVector3();
            mapSize.y = Mathf.Max(mapSize.x, mapSize.z);

            Ray ray = new Ray(shotPos, direction);
            Bounds mapBounds = new Bounds(mapSize.Yto0() / 2f, mapSize);

            mapBounds.IntersectRay(ray, out float dist);
            Vector3 exitCell = ray.GetPoint(dist);
            exitCell.x = Mathf.Clamp(exitCell.x, 0, mapSize.x - 1);
            exitCell.z = Mathf.Clamp(exitCell.z, 0, mapSize.z - 1);
            exitCell.y = 0;            

            if (cell.HasValue)
            {
                targetInf.cellInt = cell.Value;
            }
            this.globalTargetInfo = targetInf;            
            this.forcedTarget = new LocalTargetInfo(exitCell.ToIntVec3());
            this.currentTargetInt = this.forcedTarget;
            this.TryStartShootSomething(false);            
        }

        public override IEnumerable<Gizmo> GetGizmos()              // Modified
         {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            foreach (Gizmo gizmo in GetTurretGizmos())
            {
                yield return gizmo;
            }
        }

        protected virtual IEnumerable<Gizmo> GetTurretGizmos()
        {
            // Ammo gizmos
            if (CompAmmo != null && (PlayerControlled || Prefs.DevMode))
            {
                foreach (Command com in CompAmmo.CompGetGizmosExtra())
                {
                    if (!PlayerControlled && Prefs.DevMode && com is GizmoAmmoStatus)
                        (com as GizmoAmmoStatus).prefix = "DEV: ";

                    yield return com;
                }
            }       
            // Don't show CONTROL gizmos on enemy turrets (even with dev mode enabled)
            if (PlayerControlled)
            {
                if (IsMortar && Active && (compAmmo?.UseAmmo ?? false) && ProjectileProps?.shellingProps != null)
                {
                    Command_ArtilleryTarget wt = new Command_ArtilleryTarget()
                    {
                        defaultLabel = "CE_ArtilleryTargetLabel".Translate(),
                        defaultDesc = "CE_ArtilleryTargetDesc".Translate(),
                        turret = this,
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true),
                        hotKey = KeyBindingDefOf.Misc5
                    };
                    yield return wt;
                }
                // Fire mode gizmos
                if (CompFireModes != null)
                {
                    foreach (Command com in CompFireModes.GenerateGizmos())
                    {
                        yield return com;
                    }
                }
                // Set forced target gizmo
                if (CanSetForcedTarget)
                {
                    var vt = new Command_VerbTarget
                    {
                        defaultLabel = "CommandSetForceAttackTarget".Translate(),
                        defaultDesc = "CommandSetForceAttackTargetDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true),
                        verb = GunCompEq.PrimaryVerb,
                        hotKey = KeyBindingDefOf.Misc4
                    };
                    if (Spawned && IsMortarOrProjectileFliesOverhead && Position.Roofed(Map))
                    {
                        vt.Disable("CannotFire".Translate() + ": " + "Roofed".Translate().CapitalizeFirst());
                    }
                    yield return vt;
                }
                // Stop forced attack gizmo
                if (forcedTarget.IsValid)
                {
                    Command_Action stop = new Command_Action();
                    stop.defaultLabel = "CommandStopForceAttack".Translate();
                    stop.defaultDesc = "CommandStopForceAttackDesc".Translate();
                    stop.icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", true);
                    stop.action = delegate
                    {
                        ResetForcedTarget();
                        SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                    };
                    if (!this.forcedTarget.IsValid)
                    {
                        stop.Disable("CommandStopAttackFailNotForceAttacking".Translate());
                    }
                    stop.hotKey = KeyBindingDefOf.Misc5;
                    yield return stop;
                }
                // Toggle fire gizmo
                if (CanToggleHoldFire)
                {
                    yield return new Command_Toggle
                    {
                        defaultLabel = "CommandHoldFire".Translate(),
                        defaultDesc = "CommandHoldFireDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire", true),
                        hotKey = KeyBindingDefOf.Misc6,
                        toggleAction = delegate
                        {
                            holdFire = !holdFire;
                            if (holdFire)
                            {
                                ResetForcedTarget();
                            }
                        },
                        isActive = (() => holdFire)
                    };
                }
            }
        }

        // ExtractShell not added

        private void ResetForcedTarget()                // Core method
        {
            this.targetingWorldMap = false;
            this.forcedTarget = LocalTargetInfo.Invalid;
            this.globalTargetInfo = GlobalTargetInfo.Invalid;
            this.burstWarmupTicksLeft = 0;            
            if (this.burstCooldownTicksLeft <= 0)
            {
                this.TryStartShootSomething(false);                
            }          
        }

        private void ResetCurrentTarget()               // Core method
        {            
            this.currentTargetInt = LocalTargetInfo.Invalid;            
            this.burstWarmupTicksLeft = 0;            
        }

        //MakeGun not added -- MakeGun-like code is run whenever Gun is called
        //UpdateGunVerbs not added

        // New methods
        private void InitGun()
        {
            // Callback for ammo comp
            if (CompAmmo != null)
            {
                CompAmmo.turret = this;
                //if (def.building.turretShellDef != null && def.building.turretShellDef is AmmoDef) CompAmmo.selectedAmmo = (AmmoDef)def.building.turretShellDef;
            }
            List<Verb> allVerbs = this.gunInt.TryGetComp<CompEquippable>().AllVerbs;
            for (int i = 0; i < allVerbs.Count; i++)
            {
                Verb verb = allVerbs[i];
                verb.caster = this;
                verb.castCompleteCallback = new Action(this.BurstComplete);
            }
        }

        public void TryForceReload()
        {
            TryOrderReload(true);
        }

        public Thing InventoryAmmo(CompInventory inventory)
        {
            if (inventory == null)
                return null;

            Thing ammo = inventory.container.FirstOrDefault(x => x.def == CompAmmo.SelectedAmmo);

            // NPC's switch ammo types
            if (ammo == null)
            {
                ammo = inventory.container.FirstOrDefault(x => CompAmmo.Props.ammoSet.ammoTypes.Any(a => a.ammo == x.def));
            }

            return ammo;
        }

        public void TryOrderReload(bool forced = false)
        {
            //No reload necessary at all --
            if ((CompAmmo.CurrentAmmo == CompAmmo.SelectedAmmo && (!CompAmmo.HasMagazine || CompAmmo.CurMagCount == CompAmmo.MagSize)))
                return;

            //Non-mannableComp interaction
            if (!mannableComp?.MannedNow ?? true)
            {
                return;
            }

            //Only have manningPawn reload after a long time of no firing
            if (!forced && Reloadable && (compAmmo.CurMagCount != 0 || ticksUntilAutoReload > 0))
                return;

            //Already reserved for manning
            Pawn manningPawn = mannableComp.ManningPawn;
            if (manningPawn != null)
            {
                if (!JobGiverUtils_Reload.CanReload(manningPawn, this))
                {
                    return;
                }
                var jobOnThing = JobGiverUtils_Reload.MakeReloadJob(manningPawn, this);

                if (jobOnThing != null)
                {
                    manningPawn.jobs.StartJob(jobOnThing, JobCondition.Ongoing, null, manningPawn.CurJob?.def != CE_JobDefOf.ReloadTurret);
                }
            }

        }
        #endregion
    }
}
